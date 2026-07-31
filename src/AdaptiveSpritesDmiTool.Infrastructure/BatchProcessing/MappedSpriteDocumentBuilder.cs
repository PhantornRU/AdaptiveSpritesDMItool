using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;

internal sealed class MappedSpriteDocumentBuilder(
    ISpriteFrameSource frameSource,
    IAssetProbeService probeService)
{
    public async Task<Result<SpriteDocument>> BuildAsync(
        SpriteDocument sourceDocument,
        SpriteConfig config,
        SpriteDirectionDepth? targetDepth,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceDocument);
        ArgumentNullException.ThrowIfNull(config);
        if (sourceDocument.Resolution != config.Resolution)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(
                $"Source resolution {sourceDocument.Resolution} does not match config {config.Resolution}."));
        }

        var configValidation = config.Validate();
        if (!configValidation.IsValid)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation(configValidation.Errors[0].Message));
        }

        try
        {
            Directory.CreateDirectory(workingDirectory);
            var sources = new List<SpriteSourceReference>();
            var states = new List<SpriteDocumentState>(sourceDocument.States.Count);
            var totalFrames = 0;

            for (var stateIndex = 0; stateIndex < sourceDocument.States.Count; stateIndex++)
            {
                var sourceState = sourceDocument.States[stateIndex];
                var outputDepth = targetDepth ?? sourceState.DirectionDepth;
                var compatibilityError = ValidateDirectionCompatibility(config, outputDepth);
                if (compatibilityError is not null)
                {
                    return Result.Failure<SpriteDocument>(Errors.Validation(compatibilityError));
                }

                totalFrames = checked(totalFrames + checked((int)outputDepth * sourceState.FramesPerDirection));
                if (totalFrames > AssetImportLimits.Default.MaximumFramesOrCells)
                {
                    return Result.Failure<SpriteDocument>(Errors.Validation(
                        $"Mapped document exceeds the {AssetImportLimits.Default.MaximumFramesOrCells} frame limit."));
                }

                var stateDirectoryName = $"{stateIndex:D4}-{SanitizeName(sourceState.Name)}";
                var stateDirectory = Path.Combine(workingDirectory, stateDirectoryName);
                Directory.CreateDirectory(stateDirectory);
                var frames = new List<SpriteDocumentFrame>((int)outputDepth * sourceState.FramesPerDirection);
                foreach (var outputDirection in outputDepth.GetDirections())
                {
                    var sourceDirectionResult = ResolveSourceDirection(sourceState, outputDirection);
                    if (sourceDirectionResult.IsFailure)
                    {
                        return Result.Failure<SpriteDocument>(sourceDirectionResult.Error);
                    }

                    for (var frameIndex = 0; frameIndex < sourceState.FramesPerDirection; frameIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var frameResult = await frameSource.ReadAsync(
                            new SpriteFrameReadRequest(
                                sourceDocument,
                                sourceState.Name,
                                sourceDirectionResult.Value,
                                frameIndex),
                            cancellationToken).ConfigureAwait(false);
                        if (frameResult.IsFailure)
                        {
                            return Result.Failure<SpriteDocument>(frameResult.Error);
                        }

                        var mapped = ApplyMappings(frameResult.Value, config, outputDirection);
                        var relativePath = Path.Combine(
                            stateDirectoryName,
                            outputDirection.ToString().ToLowerInvariant(),
                            $"frame-{frameIndex:D4}.png");
                        var outputPath = Path.Combine(workingDirectory, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                        using (var image = Image.LoadPixelData<Rgba32>(mapped.RgbaBytes, mapped.Width, mapped.Height))
                        {
                            await image.SaveAsPngAsync(outputPath, cancellationToken).ConfigureAwait(false);
                        }

                        var probeResult = await probeService
                            .ProbeAsync(outputPath, AssetImportLimits.Default, cancellationToken)
                            .ConfigureAwait(false);
                        if (probeResult.IsFailure)
                        {
                            return Result.Failure<SpriteDocument>(probeResult.Error);
                        }

                        var sourceId = Guid.NewGuid();
                        var probe = probeResult.Value;
                        sources.Add(new SpriteSourceReference(
                            sourceId,
                            relativePath,
                            outputPath,
                            SpriteSourceFormat.Png,
                            probe.Width,
                            probe.Height,
                            probe.EncodedLength,
                            probe.Sha256));
                        frames.Add(new SpriteDocumentFrame(
                            outputDirection,
                            frameIndex,
                            new SpriteFrameReference(
                                sourceId,
                                new SpriteSourceRectangle(0, 0, mapped.Width, mapped.Height))));
                    }
                }

                states.Add(new SpriteDocumentState(
                    sourceState.Name,
                    outputDepth,
                    sourceState.FramesPerDirection,
                    sourceState.Animation,
                    frames));
            }

            return Result.Success(new SpriteDocument(
                sourceDocument.Id,
                sourceDocument.Name,
                sourceDocument.Resolution,
                sources,
                states));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SpriteDocument>(Errors.Cancelled("Mapped document generation was cancelled."));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return Result.Failure<SpriteDocument>(Errors.Validation($"Mapped document is invalid: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<SpriteDocument>(Errors.Unexpected($"Failed to generate mapped document: {exception.Message}"));
        }
    }

    private static string? ValidateDirectionCompatibility(SpriteConfig config, SpriteDirectionDepth targetDepth)
    {
        if (targetDepth == SpriteDirectionDepth.Eight && config.SupportedDirections != SupportedDirectionSet.Eight)
        {
            return "An 8-direction raster profile requires an 8-direction mapping config.";
        }

        return targetDepth is SpriteDirectionDepth.One or SpriteDirectionDepth.Four or SpriteDirectionDepth.Eight
            ? null
            : "Raster profile direction depth must be 1, 4, or 8.";
    }

    private static Result<SpriteDirection> ResolveSourceDirection(
        SpriteDocumentState state,
        SpriteDirection requestedDirection)
    {
        var available = state.DirectionDepth.GetDirections();
        if (available.Contains(requestedDirection))
        {
            return Result.Success(requestedDirection);
        }

        if (state.DirectionDepth == SpriteDirectionDepth.One)
        {
            return Result.Success(SpriteDirection.South);
        }

        return Result.Failure<SpriteDirection>(Errors.Validation(
            $"State '{state.Name}' does not contain direction '{requestedDirection}'."));
    }

    private static SpriteImage ApplyMappings(
        SpriteImage source,
        SpriteConfig config,
        SpriteDirection direction)
    {
        var output = source.RgbaBytes[..];
        foreach (var mapping in config.GetMappings(direction))
        {
            var outputOffset = ((mapping.Source.Y * source.Width) + mapping.Source.X) * 4;
            if (mapping.Target is null)
            {
                output.AsSpan(outputOffset, 4).Clear();
                continue;
            }

            var sourceOffset = ((mapping.Target.Value.Y * source.Width) + mapping.Target.Value.X) * 4;
            source.RgbaBytes.AsSpan(sourceOffset, 4).CopyTo(output.AsSpan(outputOffset, 4));
        }

        return new SpriteImage(source.Width, source.Height, output);
    }

    private static string SanitizeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var result = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(result) ? "state" : result;
    }
}
