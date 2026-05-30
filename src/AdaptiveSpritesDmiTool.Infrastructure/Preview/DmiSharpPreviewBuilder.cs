using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using DMISharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Infrastructure.Preview;

public sealed class DmiSharpPreviewBuilder : IPreviewBuilder, IDisposable
{
    /// <summary>
    /// ConcurrentDictionary is used to cache DMIFiles safely across asynchronous preview builds and concurrent UI requests.
    /// IDisposable is implemented to ensure manual release of unmanaged resources held by DMIFile objects.
    /// </summary>
    private readonly ConcurrentDictionary<string, DmiCacheEntry> _dmiCache = new();

    private readonly record struct DmiCacheEntry(DMIFile File, DateTime LastWriteTimeUtc, long Length);

    public async Task<Result<PreviewBuildResult>> BuildAsync(PreviewBuildRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Asset.SourcePath))
        {
            return Result.Failure<PreviewBuildResult>(Errors.Validation("Preview requires a source DMI path."));
        }

        if (string.IsNullOrWhiteSpace(request.Selection.BaseState))
        {
            return Result.Failure<PreviewBuildResult>(Errors.Validation("Preview requires a base state name."));
        }

        var sourcePath = Path.GetFullPath(request.Asset.SourcePath);
        if (!File.Exists(sourcePath))
        {
            return Result.Failure<PreviewBuildResult>(Errors.NotFound($"Preview source DMI '{sourcePath}' was not found."));
        }

        var compatibility = request.Config.ValidateCompatibility(request.Asset.Resolution, request.Asset.SupportedDirections);
        if (!compatibility.IsValid)
        {
            return Result.Failure<PreviewBuildResult>(Errors.Validation(compatibility.Errors[0].Message));
        }

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dmiFile = GetOrOpenFile(sourcePath);

                var baseState = FindState(dmiFile, request.Selection.BaseState);
                if (baseState is null)
                {
                    return Result.Failure<PreviewBuildResult>(Errors.NotFound($"Base state '{request.Selection.BaseState}' was not found."));
                }

                using var baseFrame = ReadFrame(baseState, request.Direction);
                if (baseFrame is null)
                {
                    return Result.Failure<PreviewBuildResult>(Errors.Validation($"Base state '{baseState.Name}' does not have a readable frame."));
                }

                using var originalBase = baseFrame.Clone();
                using var transformedBase = baseFrame.Clone();
                var editableBackingOrigins = ApplyConfigToFrame(
                    transformedBase,
                    baseFrame,
                    request.Config,
                    ResolveDirection(baseState, request.Direction));

                using var landmarkFrame = ReadOptionalFrame(dmiFile, request.Selection.LandmarkState, request.Direction, transformedBase.Width, transformedBase.Height);
                using var overlayFrame = ReadOptionalFrame(dmiFile, request.Selection.OverlayState, request.Direction, transformedBase.Width, transformedBase.Height);
                using var composite = ComposeLayers(landmarkFrame, transformedBase, overlayFrame);

                var result = new PreviewBuildResult(
                    ToSpriteImage(originalBase),
                    landmarkFrame is null ? null : ToSpriteImage(landmarkFrame),
                    overlayFrame is null ? null : ToSpriteImage(overlayFrame),
                    ToSpriteImage(composite),
                    editableBackingOrigins);

                return Result.Success(result);
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<PreviewBuildResult>(Errors.Cancelled("Preview generation was cancelled."));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<PreviewBuildResult>(Errors.Validation($"Failed to build preview: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<PreviewBuildResult>(Errors.Unexpected($"Failed to build preview: {exception.Message}"));
        }
    }

    private static DMIState? FindState(DMIFile dmiFile, string stateName) =>
        dmiFile.States.FirstOrDefault(state => string.Equals(state.Name, stateName, StringComparison.OrdinalIgnoreCase));

    private static Image<Rgba32>? ReadOptionalFrame(
        DMIFile dmiFile,
        string? stateName,
        SpriteDirection requestedDirection,
        int expectedWidth,
        int expectedHeight)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return null;
        }

        var state = FindState(dmiFile, stateName);
        if (state is null)
        {
            return null;
        }

        var frame = ReadFrame(state, requestedDirection);
        if (frame is null)
        {
            return null;
        }

        if (frame.Width != expectedWidth || frame.Height != expectedHeight)
        {
            frame.Dispose();
            return null;
        }

        return frame;
    }

    private static Image<Rgba32>? ReadFrame(DMIState state, SpriteDirection requestedDirection)
    {
        var resolvedDirection = ResolveDirection(state, requestedDirection);
        return state.GetFrame(resolvedDirection, 0)?.Clone();
    }

    private static StateDirection ResolveDirection(DMIState state, SpriteDirection requestedDirection)
    {
        var requested = DmiSharpConversions.ToDmiDirection(requestedDirection);
        var availableDirections = DmiSharpConversions.GetDirections(state.DirectionDepth);
        return availableDirections.Contains(requested)
            ? requested
            : availableDirections[0];
    }

    private static Dictionary<PixelCoordinate, PixelCoordinate?> ApplyConfigToFrame(
        Image<Rgba32> targetFrame,
        Image<Rgba32> sourceFrame,
        SpriteConfig config,
        StateDirection direction)
    {
        var mappings = config.GetMappings(DmiSharpConversions.ToDomainDirection(direction))
            .ToDictionary(static mapping => mapping.Source, static mapping => mapping.Target);
        var editableBackingOrigins = new Dictionary<PixelCoordinate, PixelCoordinate?>(targetFrame.Width * targetFrame.Height);

        targetFrame.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var coordinate = new PixelCoordinate(x, y);
                    if (!mappings.TryGetValue(coordinate, out var mappedCoordinate))
                    {
                        editableBackingOrigins[coordinate] = coordinate;
                        continue;
                    }

                    editableBackingOrigins[coordinate] = mappedCoordinate;

                    row[x] = mappedCoordinate is null
                        ? default
                        : sourceFrame[mappedCoordinate.Value.X, mappedCoordinate.Value.Y];
                }
            }
        });

        return editableBackingOrigins;
    }

    private static Image<Rgba32> ComposeLayers(
        Image<Rgba32>? landmarkFrame,
        Image<Rgba32> baseFrame,
        Image<Rgba32>? overlayFrame)
    {
        var composite = new Image<Rgba32>(baseFrame.Width, baseFrame.Height);

        if (landmarkFrame is not null)
        {
            BlendOver(composite, landmarkFrame);
        }

        BlendOver(composite, baseFrame);

        if (overlayFrame is not null)
        {
            BlendOver(composite, overlayFrame);
        }

        return composite;
    }

    private static void BlendOver(Image<Rgba32> canvas, Image<Rgba32> layer)
    {
        canvas.ProcessPixelRows(layer, (canvasAccessor, layerAccessor) =>
        {
            for (var y = 0; y < canvasAccessor.Height; y++)
            {
                var canvasRow = canvasAccessor.GetRowSpan(y);
                var layerRow = layerAccessor.GetRowSpan(y);

                for (var x = 0; x < canvasRow.Length; x++)
                {
                    canvasRow[x] = AlphaComposite(canvasRow[x], layerRow[x]);
                }
            }
        });
    }

    private static Rgba32 AlphaComposite(Rgba32 background, Rgba32 foreground)
    {
        var fgAlpha = foreground.A / 255f;
        if (fgAlpha <= 0f)
        {
            return background;
        }

        var bgAlpha = background.A / 255f;
        var outAlpha = fgAlpha + (bgAlpha * (1f - fgAlpha));
        if (outAlpha <= 0f)
        {
            return default;
        }

        static byte Channel(byte bg, byte fg, float bgAlpha, float fgAlpha, float outAlpha)
        {
            var value = ((fg * fgAlpha) + (bg * bgAlpha * (1f - fgAlpha))) / outAlpha;
            return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
        }

        return new Rgba32(
            Channel(background.R, foreground.R, bgAlpha, fgAlpha, outAlpha),
            Channel(background.G, foreground.G, bgAlpha, fgAlpha, outAlpha),
            Channel(background.B, foreground.B, bgAlpha, fgAlpha, outAlpha),
            (byte)Math.Clamp((int)Math.Round(outAlpha * 255f), 0, 255));
    }

    private static SpriteImage ToSpriteImage(Image<Rgba32> image)
    {
        var pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        var bytes = MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
        return new SpriteImage(image.Width, image.Height, bytes);
    }

    private DMIFile GetOrOpenFile(string path)
    {
        var fileInfo = new FileInfo(path);
        var currentTimestamp = fileInfo.LastWriteTimeUtc;
        var currentLength = fileInfo.Length;

        // Fast path: cache hit with matching file metadata
        if (_dmiCache.TryGetValue(path, out var entry) &&
            entry.LastWriteTimeUtc == currentTimestamp &&
            entry.Length == currentLength)
        {
            return entry.File;
        }

        // Slow path: stale or missing entry — load fresh and atomically replace
        var newFile = new DMIFile(path);
        var newEntry = new DmiCacheEntry(newFile, currentTimestamp, currentLength);

        while (true)
        {
            if (_dmiCache.TryGetValue(path, out var existingEntry))
            {
                if (_dmiCache.TryUpdate(path, newEntry, existingEntry))
                {
                    existingEntry.File.Dispose();
                    return newEntry.File;
                }
                // Another thread changed the entry concurrently; retry
            }
            else
            {
                if (_dmiCache.TryAdd(path, newEntry))
                {
                    return newEntry.File;
                }
                // Another thread added an entry concurrently; retry to verify freshness
            }
        }
    }

    public void Dispose()
    {
        foreach (var entry in _dmiCache.Values)
        {
            entry.File.Dispose();
        }
        _dmiCache.Clear();
    }
}
