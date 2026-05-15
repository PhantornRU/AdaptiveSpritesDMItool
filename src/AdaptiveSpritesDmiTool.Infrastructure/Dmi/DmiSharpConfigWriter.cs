using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using DMISharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Infrastructure.Dmi;

public sealed class DmiSharpConfigWriter : IDmiWriter
{
    public async Task<Result<BatchFileResult>> ApplyAsync(ApplyConfigToFileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            return Result.Failure<BatchFileResult>(Errors.Validation("Input DMI path is required."));
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            return Result.Failure<BatchFileResult>(Errors.Validation("Output DMI path is required."));
        }

        var inputPath = Path.GetFullPath(request.InputPath);
        var outputPath = Path.GetFullPath(request.OutputPath);

        if (!File.Exists(inputPath))
        {
            return Result.Failure<BatchFileResult>(Errors.NotFound($"DMI file '{inputPath}' was not found."));
        }

        if (File.Exists(outputPath))
        {
            return request.OverwritePolicy switch
            {
                OverwritePolicy.SkipExisting => Result.Success(
                    new BatchFileResult(inputPath, outputPath, BatchFileStatus.Skipped, "Skipped because output file already exists.")),
                OverwritePolicy.FailIfExists => Result.Success(
                    new BatchFileResult(inputPath, outputPath, BatchFileStatus.Failed, "Output file already exists.")),
                _ => await ProcessAsync(inputPath, outputPath, request.Config, cancellationToken)
            };
        }

        return await ProcessAsync(inputPath, outputPath, request.Config, cancellationToken);
    }

    private static async Task<Result<BatchFileResult>> ProcessAsync(
        string inputPath,
        string outputPath,
        SpriteConfig config,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        var configValidation = config.Validate();
        if (!configValidation.IsValid)
        {
            return Result.Failure<BatchFileResult>(Errors.Validation(configValidation.Errors[0].Message));
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(
            string.IsNullOrWhiteSpace(directory) ? Path.GetDirectoryName(inputPath) ?? Path.GetTempPath() : directory,
            $"{Path.GetFileNameWithoutExtension(outputPath)}.{Guid.NewGuid():N}.tmp.dmi");

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (new FileInfo(inputPath).Length == 0)
                {
                    return Result.Failure<BatchFileResult>(Errors.Validation("DMI file is empty."));
                }

                using var dmiFile = new DMIFile(inputPath);
                if (dmiFile.States.Count == 0)
                {
                    return Result.Failure<BatchFileResult>(Errors.Validation("DMI file does not contain any states."));
                }

                var compatibility = config.ValidateCompatibility(
                    DmiSharpConversions.InferResolution(dmiFile),
                    DmiSharpConversions.InferSupportedDirections(dmiFile.States));
                if (!compatibility.IsValid)
                {
                    return Result.Failure<BatchFileResult>(Errors.Validation(compatibility.Errors[0].Message));
                }

                dmiFile.SortStates(StateComparer);
                TransformStates(dmiFile, config, cancellationToken);

                dmiFile.Save(tempPath);
                if (!File.Exists(tempPath))
                {
                    return Result.Failure<BatchFileResult>(Errors.Unexpected("Failed to save transformed DMI file."));
                }

                File.Move(tempPath, outputPath, true);
                return Result.Success(
                    new BatchFileResult(inputPath, outputPath, BatchFileStatus.Processed, "Config applied successfully."));
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<BatchFileResult>(Errors.Cancelled("DMI processing was cancelled."));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<BatchFileResult>(Errors.Validation($"Failed to process DMI file: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<BatchFileResult>(Errors.Unexpected($"Failed to process DMI file: {exception.Message}"));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static void TransformStates(DMIFile dmiFile, SpriteConfig config, CancellationToken cancellationToken)
    {
        foreach (var state in dmiFile.States)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directions = DmiSharpConversions.GetDirections(state.DirectionDepth);
            var frameCount = GetFrameCount(state, directions.Count);

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var direction in directions)
                {
                    var sourceFrame = state.GetFrame(direction, frameIndex);
                    if (sourceFrame is null)
                    {
                        throw new ArgumentException(
                            $"State '{state.Name}' is missing frame {frameIndex} for direction '{direction}'.");
                    }

                    var transformedFrame = sourceFrame.Clone();
                    ApplyMappings(transformedFrame, sourceFrame, config, DmiSharpConversions.ToDomainDirection(direction));
                    state.SetFrame(transformedFrame, direction, frameIndex);
                }
            }
        }
    }

    private static int GetFrameCount(DMIState state, int directionCount)
    {
        if (state.TotalFrames <= 0 || state.TotalFrames % directionCount != 0)
        {
            throw new ArgumentException(
                $"State '{state.Name}' has an unsupported frame layout for direction depth '{state.DirectionDepth}'.");
        }

        return state.TotalFrames / directionCount;
    }

    private static void ApplyMappings(
        Image<Rgba32> targetFrame,
        Image<Rgba32> sourceFrame,
        SpriteConfig config,
        SpriteDirection direction)
    {
        var mappings = config.GetMappings(direction).ToDictionary(static mapping => mapping.Source, static mapping => mapping.Target);

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
                        continue;
                    }

                    row[x] = mappedCoordinate is null
                        ? default
                        : sourceFrame[mappedCoordinate.Value.X, mappedCoordinate.Value.Y];
                }
            }
        });
    }

    private static IComparer<DMIState> StateComparer { get; } =
        Comparer<DMIState>.Create((left, right) =>
        {
            var result = StringComparer.Ordinal.Compare(left.Name, right.Name);
            if (result != 0)
            {
                return result;
            }

            result = left.DirectionDepth.CompareTo(right.DirectionDepth);
            if (result != 0)
            {
                return result;
            }

            result = left.TotalFrames.CompareTo(right.TotalFrames);
            if (result != 0)
            {
                return result;
            }

            result = left.Width.CompareTo(right.Width);
            if (result != 0)
            {
                return result;
            }

            return left.Height.CompareTo(right.Height);
        });
}
