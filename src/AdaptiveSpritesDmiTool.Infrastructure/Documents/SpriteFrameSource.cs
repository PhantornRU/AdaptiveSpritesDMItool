using System.Runtime.InteropServices;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Documents;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using DMISharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AdaptiveSpritesDmiTool.Infrastructure.Documents;

public sealed class SpriteFrameSource(IAssetProbeService probeService) : ISpriteFrameSource
{
    public async Task<Result<SpriteImage>> ReadAsync(
        SpriteFrameReadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var state = request.Document.States.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, request.StateName, StringComparison.Ordinal));
            if (state is null)
            {
                return Result.Failure<SpriteImage>(Errors.NotFound($"State '{request.StateName}' was not found."));
            }

            var frame = state.Frames.FirstOrDefault(candidate =>
                candidate.Direction == request.Direction && candidate.FrameIndex == request.FrameIndex);
            if (frame is null)
            {
                return Result.Failure<SpriteImage>(Errors.NotFound(
                    $"Frame '{request.StateName}/{request.Direction}/{request.FrameIndex}' was not found."));
            }

            var source = request.Document.Sources.First(candidate => candidate.Id == frame.Source.SourceId);
            var probeResult = await probeService
                .ProbeAsync(source.AbsolutePathFallback, AssetImportLimits.Default, cancellationToken)
                .ConfigureAwait(false);
            if (probeResult.IsFailure)
            {
                return Result.Failure<SpriteImage>(probeResult.Error);
            }

            var probe = probeResult.Value;
            if (probe.DetectedFormat != source.Format ||
                probe.Width != source.Width ||
                probe.Height != source.Height ||
                probe.EncodedLength != source.EncodedLength ||
                !string.Equals(probe.Sha256, source.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<SpriteImage>(Errors.Conflict(
                    $"Source '{source.RelativePath}' changed after import. Relink it, accept the new fingerprint, or cancel."));
            }

            using var decoded = source.Format switch
            {
                SpriteSourceFormat.Dmi => await ReadDmiFrameAsync(source.AbsolutePathFallback, state.Name, request.Direction, frame.Source, cancellationToken).ConfigureAwait(false),
                SpriteSourceFormat.Png => await ReadPngFrameAsync(source.AbsolutePathFallback, frame.Source.SourceFrameIndex, cancellationToken).ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Unsupported source format '{source.Format}'.")
            };
            using var transformed = TransformFrame(decoded, frame.Source);
            using var canvas = PlaceOnCanvas(transformed, request.Document.Resolution.Width, request.Document.Resolution.Height, frame.Source.Transform);
            return Result.Success(ToSpriteImage(canvas));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SpriteImage>(Errors.Cancelled("Sprite frame reading was cancelled."));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or InvalidDataException)
        {
            return Result.Failure<SpriteImage>(Errors.Validation($"Sprite frame is invalid: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<SpriteImage>(Errors.Unexpected($"Failed to read sprite frame: {exception.Message}"));
        }
    }

    private static Task<Image<Rgba32>> ReadDmiFrameAsync(
        string path,
        string documentStateName,
        Domain.Configurations.SpriteDirection documentDirection,
        SpriteFrameReference reference,
        CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var dmiFile = new DMIFile(path);
            var stateName = reference.SourceStateName ?? documentStateName;
            var state = dmiFile.States.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, stateName, StringComparison.Ordinal));
            if (state is null)
            {
                throw new InvalidDataException($"DMI state '{stateName}' was not found.");
            }

            var direction = DmiSharpConversions.ToDmiDirection(reference.SourceDirection ?? documentDirection);
            var image = state.GetFrame(direction, reference.SourceFrameIndex);
            return image?.Clone()
                ?? throw new InvalidDataException($"DMI frame '{stateName}/{direction}/{reference.SourceFrameIndex}' was not found.");
        }, cancellationToken);

    private static async Task<Image<Rgba32>> ReadPngFrameAsync(
        string path,
        int frameIndex,
        CancellationToken cancellationToken)
    {
        if (frameIndex != 0)
        {
            throw new InvalidDataException("Animated PNG frames are not supported in V2.4.");
        }

        var decoderOptions = new DecoderOptions { MaxFrames = 1, SkipMetadata = true };
        return await Image.LoadAsync<Rgba32>(decoderOptions, path, cancellationToken).ConfigureAwait(false);
    }

    private static Image<Rgba32> TransformFrame(Image<Rgba32> source, SpriteFrameReference reference)
    {
        var rectangle = reference.SourceRectangle;
        if (!rectangle.FitsInside(source.Width, source.Height))
        {
            throw new InvalidDataException("Frame crop is outside decoded source bounds.");
        }

        var result = source.Clone(context => context.Crop(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height)));
        var transform = reference.Transform;
        result.Mutate(context =>
        {
            if (transform.FlipHorizontal)
            {
                context.Flip(FlipMode.Horizontal);
            }

            if (transform.FlipVertical)
            {
                context.Flip(FlipMode.Vertical);
            }

            context.Rotate(transform.ClockwiseQuarterTurns switch
            {
                0 => RotateMode.None,
                1 => RotateMode.Rotate90,
                2 => RotateMode.Rotate180,
                3 => RotateMode.Rotate270,
                _ => throw new InvalidOperationException("Invalid frame rotation.")
            });

            if (transform.OutputWidth.HasValue && transform.OutputHeight.HasValue)
            {
                context.Resize(new ResizeOptions
                {
                    Size = new Size(transform.OutputWidth.Value, transform.OutputHeight.Value),
                    Mode = ResizeMode.Stretch,
                    Sampler = KnownResamplers.NearestNeighbor
                });
            }
        });
        return result;
    }

    private static Image<Rgba32> PlaceOnCanvas(
        Image<Rgba32> frame,
        int canvasWidth,
        int canvasHeight,
        SpriteFrameTransform transform)
    {
        var canvas = new Image<Rgba32>(canvasWidth, canvasHeight, Color.Transparent);
        for (var sourceY = 0; sourceY < frame.Height; sourceY++)
        {
            var targetY = sourceY + transform.OffsetY;
            if (targetY < 0 || targetY >= canvasHeight)
            {
                continue;
            }

            for (var sourceX = 0; sourceX < frame.Width; sourceX++)
            {
                var targetX = sourceX + transform.OffsetX;
                if (targetX >= 0 && targetX < canvasWidth)
                {
                    canvas[targetX, targetY] = frame[sourceX, sourceY];
                }
            }
        }

        return canvas;
    }

    private static SpriteImage ToSpriteImage(Image<Rgba32> image)
    {
        var pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        return new SpriteImage(image.Width, image.Height, MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray());
    }
}
