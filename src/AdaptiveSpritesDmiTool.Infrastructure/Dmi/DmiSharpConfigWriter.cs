using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using DMISharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Infrastructure.Dmi;

public sealed partial class DmiSharpConfigWriter : IDmiWriter
{
    [LoggerMessage(
        EventId = 2301,
        Level = LogLevel.Information,
        Message = "dmi_write operation_id={OperationId} tool={Tool} state={State} direction={Direction} frame={Frame} " +
                  "scope={Scope} mappings={Mappings} applied={Applied} skipped={Skipped} input={InputPath} output={OutputPath} result=processed")]
    private static partial void LogProcessed(
        ILogger logger,
        string operationId,
        string tool,
        string state,
        string direction,
        string frame,
        string scope,
        int mappings,
        int applied,
        int skipped,
        string inputPath,
        string outputPath);

    [LoggerMessage(
        EventId = 2302,
        Level = LogLevel.Warning,
        Message = "dmi_write operation_id={OperationId} tool={Tool} state={State} direction={Direction} frame={Frame} " +
                  "scope={Scope} mappings={Mappings} applied={Applied} skipped={Skipped} input={InputPath} output={OutputPath} " +
                  "result=cancelled message={Message}")]
    private static partial void LogCancelled(
        ILogger logger,
        string operationId,
        string tool,
        string state,
        string direction,
        string frame,
        string scope,
        int mappings,
        int applied,
        int skipped,
        string inputPath,
        string outputPath,
        string message);

    [LoggerMessage(
        EventId = 2303,
        Level = LogLevel.Warning,
        Message = "dmi_write operation_id={OperationId} tool={Tool} state={State} direction={Direction} frame={Frame} " +
                  "scope={Scope} mappings={Mappings} applied={Applied} skipped={Skipped} input={InputPath} output={OutputPath} " +
                  "result=validation_failed message={Message}")]
    private static partial void LogValidationFailed(
        ILogger logger,
        string operationId,
        string tool,
        string state,
        string direction,
        string frame,
        string scope,
        int mappings,
        int applied,
        int skipped,
        string inputPath,
        string outputPath,
        string message,
        Exception? exception);

    [LoggerMessage(
        EventId = 2304,
        Level = LogLevel.Error,
        Message = "dmi_write operation_id={OperationId} tool={Tool} state={State} direction={Direction} frame={Frame} " +
                  "scope={Scope} mappings={Mappings} applied={Applied} skipped={Skipped} input={InputPath} output={OutputPath} " +
                  "result=failed message={Message}")]
    private static partial void LogFailed(
        ILogger logger,
        string operationId,
        string tool,
        string state,
        string direction,
        string frame,
        string scope,
        int mappings,
        int applied,
        int skipped,
        string inputPath,
        string outputPath,
        string message,
        Exception? exception);

    private static void LogDmiProcessed(
        ILogger logger,
        string operationId,
        string inputPath,
        string outputPath,
        int mappings,
        int applied) =>
        LogProcessed(logger, operationId, "DmiWriter", "all", "all", "all", "Batch", mappings, applied, 0, inputPath, outputPath);

    private static void LogDmiCancelled(
        ILogger logger,
        string operationId,
        string inputPath,
        string outputPath,
        int mappings,
        int applied,
        string message) =>
        LogCancelled(logger, operationId, "DmiWriter", "all", "all", "all", "Batch", mappings, applied, 0, inputPath, outputPath, message);

    private static void LogDmiValidationFailed(
        ILogger logger,
        string operationId,
        string inputPath,
        string outputPath,
        int mappings,
        int applied,
        string message,
        Exception? exception) =>
        LogValidationFailed(
            logger,
            operationId,
            "DmiWriter",
            "all",
            "all",
            "all",
            "Batch",
            mappings,
            applied,
            0,
            inputPath,
            outputPath,
            message,
            exception);

    private static void LogDmiFailed(
        ILogger logger,
        string operationId,
        string inputPath,
        string outputPath,
        int mappings,
        int applied,
        string message,
        Exception? exception) =>
        LogFailed(
            logger,
            operationId,
            "DmiWriter",
            "all",
            "all",
            "all",
            "Batch",
            mappings,
            applied,
            0,
            inputPath,
            outputPath,
            message,
            exception);

    private readonly IDmiArtifactValidator _artifactValidator;
    private readonly IDmiAtomicCommitter _atomicCommitter;
    private readonly ILogger<DmiSharpConfigWriter> _logger;

    public DmiSharpConfigWriter(ILogger<DmiSharpConfigWriter>? logger = null)
        : this(new DmiArtifactValidator(), new DmiAtomicCommitter(), logger ?? NullLogger<DmiSharpConfigWriter>.Instance)
    {
    }

    internal DmiSharpConfigWriter(
        IDmiArtifactValidator artifactValidator,
        IDmiAtomicCommitter atomicCommitter,
        ILogger<DmiSharpConfigWriter>? logger = null)
    {
        _artifactValidator = artifactValidator ?? throw new ArgumentNullException(nameof(artifactValidator));
        _atomicCommitter = atomicCommitter ?? throw new ArgumentNullException(nameof(atomicCommitter));
        _logger = logger ?? NullLogger<DmiSharpConfigWriter>.Instance;
    }

    public async Task<Result<BatchFileResult>> ApplyAsync(ApplyConfigToFileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var operationId = Guid.NewGuid().ToString("N");
        var rawInputPath = request.InputPath ?? string.Empty;
        var rawOutputPath = request.OutputPath ?? string.Empty;
        var mappingCount = request.Config?.Directions.Sum(direction => request.Config.GetMappings(direction).Count) ?? 0;

        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            LogDmiValidationFailed(_logger, operationId, rawInputPath, rawOutputPath, mappingCount, 0, "Input DMI path is required.", null);
            return Result.Failure<BatchFileResult>(Errors.Validation("Input DMI path is required."));
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            LogDmiValidationFailed(_logger, operationId, rawInputPath, rawOutputPath, mappingCount, 0, "Output DMI path is required.", null);
            return Result.Failure<BatchFileResult>(Errors.Validation("Output DMI path is required."));
        }

        string inputPath;
        string outputPath;
        try
        {
            inputPath = Path.GetFullPath(request.InputPath);
            outputPath = Path.GetFullPath(request.OutputPath);
        }
        catch (Exception exception)
        {
            LogDmiValidationFailed(_logger, operationId, rawInputPath, rawOutputPath, mappingCount, 0, exception.Message, exception);
            return Result.Failure<BatchFileResult>(Errors.Validation($"DMI path is invalid: {exception.Message}"));
        }

        if (!File.Exists(inputPath))
        {
            LogDmiValidationFailed(
                _logger,
                operationId,
                inputPath,
                outputPath,
                mappingCount,
                0,
                $"DMI file '{inputPath}' was not found.",
                null);
            return Result.Failure<BatchFileResult>(Errors.NotFound($"DMI file '{inputPath}' was not found."));
        }

        if (File.Exists(outputPath))
        {
            if (request.OverwritePolicy == OverwritePolicy.SkipExisting)
            {
                return Result.Success(
                    new BatchFileResult(inputPath, outputPath, BatchFileStatus.Skipped, "Skipped because output file already exists."));
            }

            if (request.OverwritePolicy == OverwritePolicy.FailIfExists)
            {
                LogDmiValidationFailed(
                    _logger,
                    operationId,
                    inputPath,
                    outputPath,
                    mappingCount,
                    0,
                    "Output file already exists.",
                    null);
                return Result.Success(
                    new BatchFileResult(inputPath, outputPath, BatchFileStatus.Failed, "Output file already exists."));
            }
        }

        return await ProcessAsync(inputPath, outputPath, request.Config, operationId, mappingCount, cancellationToken);
    }

    private async Task<Result<BatchFileResult>> ProcessAsync(
        string inputPath,
        string outputPath,
        SpriteConfig? config,
        string operationId,
        int mappingCount,
        CancellationToken cancellationToken)
    {
        if (config is null)
        {
            const string message = "A sprite config is required.";
            LogDmiValidationFailed(_logger, operationId, inputPath, outputPath, mappingCount, 0, message, null);
            return Result.Failure<BatchFileResult>(Errors.Validation(message));
        }

        var configValidation = config.Validate();
        if (!configValidation.IsValid)
        {
            LogDmiValidationFailed(
                _logger,
                operationId,
                inputPath,
                outputPath,
                mappingCount,
                0,
                configValidation.Errors[0].Message,
                null);
            return Result.Failure<BatchFileResult>(Errors.Validation(configValidation.Errors[0].Message));
        }

        string? tempPath = null;
        try
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var workingTempPath = Path.Combine(
                string.IsNullOrWhiteSpace(directory) ? Path.GetDirectoryName(inputPath) ?? Path.GetTempPath() : directory,
                $"{Path.GetFileNameWithoutExtension(outputPath)}.{Guid.NewGuid():N}.tmp.dmi");
            tempPath = workingTempPath;

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (new FileInfo(inputPath).Length == 0)
                {
                    return ValidationFailure(Errors.Validation("DMI file is empty."));
                }

                DmiArtifactFingerprint expectedFingerprint;
                int appliedPixelCount;
                using (var dmiFile = new DMIFile(inputPath))
                {
                    if (dmiFile.States.Count == 0)
                    {
                        return ValidationFailure(Errors.Validation("DMI file does not contain any states."));
                    }

                    var compatibility = config.ValidateCompatibility(
                        DmiSharpConversions.InferResolution(dmiFile),
                        DmiSharpConversions.InferSupportedDirections(dmiFile.States));
                    if (!compatibility.IsValid)
                    {
                        return ValidationFailure(Errors.Validation(compatibility.Errors[0].Message));
                    }

                    appliedPixelCount = TransformStates(dmiFile, config, cancellationToken);
                    expectedFingerprint = DmiArtifactFingerprintFactory.Create(dmiFile, cancellationToken);

                    dmiFile.Save(workingTempPath);
                }

                if (!File.Exists(workingTempPath))
                {
                    const string message = "Failed to save transformed DMI file.";
                    LogDmiFailed(_logger, operationId, inputPath, outputPath, mappingCount, 0, message, null);
                    return Result.Failure<BatchFileResult>(Errors.Unexpected(message));
                }

                cancellationToken.ThrowIfCancellationRequested();
                var verification = _artifactValidator.Validate(workingTempPath, expectedFingerprint, cancellationToken);
                if (verification.IsFailure)
                {
                    return ValidationFailure(verification.Error);
                }

                cancellationToken.ThrowIfCancellationRequested();
                _atomicCommitter.Commit(workingTempPath, outputPath);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    LogDmiProcessed(_logger, operationId, inputPath, outputPath, mappingCount, appliedPixelCount);
                }

                return Result.Success(
                    new BatchFileResult(inputPath, outputPath, BatchFileStatus.Processed, "Config applied successfully."));

                Result<BatchFileResult> ValidationFailure(Error error)
                {
                    LogDmiValidationFailed(_logger, operationId, inputPath, outputPath, mappingCount, 0, error.Message, null);
                    return Result.Failure<BatchFileResult>(error);
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogDmiCancelled(
                _logger,
                operationId,
                inputPath,
                outputPath,
                mappingCount,
                0,
                "DMI processing was cancelled.");
            return Result.Failure<BatchFileResult>(Errors.Cancelled("DMI processing was cancelled."));
        }
        catch (ArgumentException exception)
        {
            LogDmiValidationFailed(_logger, operationId, inputPath, outputPath, mappingCount, 0, exception.Message, exception);
            return Result.Failure<BatchFileResult>(Errors.Validation($"Failed to process DMI file: {exception.Message}"));
        }
        catch (Exception exception)
        {
            LogDmiFailed(_logger, operationId, inputPath, outputPath, mappingCount, 0, exception.Message, exception);
            return Result.Failure<BatchFileResult>(Errors.Unexpected($"Failed to process DMI file: {exception.Message}"));
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempPath) && File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception exception)
                {
                    LogDmiFailed(
                        _logger,
                        operationId,
                        inputPath,
                        outputPath,
                        mappingCount,
                        0,
                        $"Failed to delete temporary file '{tempPath}': {exception.Message}",
                        exception);
                }
            }
        }
    }

    private static int TransformStates(DMIFile dmiFile, SpriteConfig config, CancellationToken cancellationToken)
    {
        var appliedPixelCount = 0;
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
                    appliedPixelCount += config.GetMappings(DmiSharpConversions.ToDomainDirection(direction)).Count;
                    state.SetFrame(transformedFrame, direction, frameIndex);
                }
            }
        }

        return appliedPixelCount;
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

}
