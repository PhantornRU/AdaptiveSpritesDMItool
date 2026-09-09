using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Documents;

namespace AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;

public sealed class DeterministicBatchProcessingService : IBatchProcessingService
{
    private readonly IDmiWriter _dmiWriter;
    private readonly IAssetProbeService? _probeService;
    private readonly ISpriteDocumentImporter? _documentImporter;
    private readonly ISpriteFrameSource? _frameSource;
    private readonly ISpriteDocumentExporter? _documentExporter;

    public DeterministicBatchProcessingService(IDmiWriter dmiWriter)
        : this(dmiWriter, null, null, null, null)
    {
    }

    public DeterministicBatchProcessingService(
        IDmiWriter dmiWriter,
        IAssetProbeService? probeService,
        ISpriteDocumentImporter? documentImporter,
        ISpriteFrameSource? frameSource,
        ISpriteDocumentExporter? documentExporter)
    {
        _dmiWriter = dmiWriter;
        _probeService = probeService;
        _documentImporter = documentImporter;
        _frameSource = frameSource;
        _documentExporter = documentExporter;
    }

    public async Task<Result<BatchJobResult>> RunAsync(
        BatchJobRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Directory.Exists(request.InputDirectory))
        {
            return Result.Failure<BatchJobResult>(Errors.NotFound($"Input directory '{request.InputDirectory}' was not found."));
        }

        Directory.CreateDirectory(request.OutputDirectory);
        return request.OutputFormats is null
            ? await RunLegacyDmiBatchAsync(request, progress, cancellationToken).ConfigureAwait(false)
            : await RunNativeAssetBatchAsync(request, progress, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<BatchJobResult>> RunLegacyDmiBatchAsync(
        BatchJobRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken)
    {
        var inputFiles = BatchPathLayout
            .ResolveInputFiles(request.InputDirectory, request.OutputDirectory, request.ExplicitFiles)
            .Where(static path => Path.GetExtension(path).Equals(".dmi", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var results = new List<BatchFileResult>(inputFiles.Length);

        for (var index = 0; index < inputFiles.Length; index++)
        {
            var inputPath = inputFiles[index];
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(CreateCancelledResult(inputPath));
                ReportProgress(progress, results.Count, inputFiles.Length, inputPath);
                continue;
            }

            var outputPath = BatchPathLayout.BuildOutputPath(request.InputDirectory, request.OutputDirectory, inputPath);
            var overwriteDecision = EvaluateFileOverwritePolicy(request.OverwritePolicy, inputPath, outputPath);
            if (overwriteDecision is not null)
            {
                results.Add(overwriteDecision);
                ReportProgress(progress, results.Count, inputFiles.Length, inputPath);
                continue;
            }

            var applyRequest = new ApplyConfigToFileRequest(inputPath, outputPath, request.Config, request.OverwritePolicy);
            var applyResult = await _dmiWriter.ApplyAsync(applyRequest, cancellationToken).ConfigureAwait(false);

            results.Add(
                applyResult.IsSuccess
                    ? applyResult.Value
                    : CreateFailureResult(inputPath, outputPath, applyResult.Error));
            ReportProgress(progress, results.Count, inputFiles.Length, inputPath);
        }

        ReportProgress(progress, results.Count, inputFiles.Length, null);
        return Result.Success(new BatchJobResult(results));
    }

    private async Task<Result<BatchJobResult>> RunNativeAssetBatchAsync(
        BatchJobRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken)
    {
        var formatsResult = ValidateOutputFormats(request.OutputFormats!);
        if (formatsResult.IsFailure)
        {
            return Result.Failure<BatchJobResult>(formatsResult.Error);
        }

        if (_probeService is null || _documentImporter is null || _frameSource is null || _documentExporter is null)
        {
            return Result.Failure<BatchJobResult>(Errors.Conflict(
                "Native DMI/PNG batch services are not configured."));
        }

        var formats = formatsResult.Value;
        var rasterSettings = request.RasterExportSettings ?? WorkspaceRasterExportSettings.Default;
        if (rasterSettings.Layout is not (SpriteDocumentExportFormat.PngSheet or SpriteDocumentExportFormat.PngSequence))
        {
            return Result.Failure<BatchJobResult>(Errors.Validation(
                "Raster batch layout must be PNG sheet or PNG sequence."));
        }

        var inputFiles = BatchPathLayout
            .ResolveInputFiles(request.InputDirectory, request.OutputDirectory, request.ExplicitFiles)
            .ToArray();
        var outputCollisions = FindOutputCollisions(request, inputFiles, formats);
        var results = new List<BatchFileResult>(checked(inputFiles.Length * formats.Count));

        for (var index = 0; index < inputFiles.Length; index++)
        {
            var inputPath = inputFiles[index];
            if (cancellationToken.IsCancellationRequested)
            {
                foreach (var format in formats)
                {
                    results.Add(CreateCancelledResult(inputPath, BuildOutputPath(request, inputPath, format)));
                }

                ReportProgress(progress, index + 1, inputFiles.Length, inputPath);
                continue;
            }

            var hasNonCollidingOutput = formats.Any(format =>
                !outputCollisions.ContainsKey(BuildOutputKey(inputPath, format)));
            Result<SpriteDocument>? importResult = null;
            if (hasNonCollidingOutput)
            {
                importResult = await ImportSourceAsync(inputPath, request, cancellationToken).ConfigureAwait(false);
            }

            foreach (var format in formats)
            {
                var outputPath = BuildOutputPath(request, inputPath, format);
                if (outputCollisions.TryGetValue(BuildOutputKey(inputPath, format), out var collisionMessage))
                {
                    results.Add(new BatchFileResult(
                        inputPath,
                        outputPath,
                        BatchFileStatus.Failed,
                        collisionMessage));
                    continue;
                }

                if (importResult!.IsFailure)
                {
                    results.Add(CreateFailureResult(inputPath, outputPath, importResult.Error));
                    continue;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    results.Add(CreateCancelledResult(inputPath, outputPath));
                    continue;
                }

                results.Add(await ProcessOutputAsync(
                    inputPath,
                    importResult.Value,
                    format,
                    rasterSettings,
                    request,
                    cancellationToken).ConfigureAwait(false));
            }

            ReportProgress(progress, index + 1, inputFiles.Length, inputPath);
        }

        ReportProgress(progress, inputFiles.Length, inputFiles.Length, null);
        return Result.Success(new BatchJobResult(results));
    }

    private async Task<Result<SpriteDocument>> ImportSourceAsync(
        string inputPath,
        BatchJobRequest request,
        CancellationToken cancellationToken)
    {
        var probeResult = await _probeService!
            .ProbeAsync(inputPath, AssetImportLimits.Default, cancellationToken)
            .ConfigureAwait(false);
        if (probeResult.IsFailure)
        {
            return Result.Failure<SpriteDocument>(probeResult.Error);
        }

        var kind = probeResult.Value.DetectedFormat == SpriteSourceFormat.Dmi
            ? SpriteDocumentImportKind.NativeDmi
            : SpriteDocumentImportKind.SingleRaster;
        return await _documentImporter!
            .ImportAsync(
                new SpriteDocumentImportRequest(
                    [inputPath],
                    Path.GetFileNameWithoutExtension(inputPath),
                    kind,
                    StateName: Path.GetFileNameWithoutExtension(inputPath),
                    Limits: AssetImportLimits.Default),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<BatchFileResult> ProcessOutputAsync(
        string inputPath,
        SpriteDocument sourceDocument,
        WorkspaceBatchOutputFormat outputFormat,
        WorkspaceRasterExportSettings rasterSettings,
        BatchJobRequest request,
        CancellationToken cancellationToken)
    {
        var outputPath = BuildOutputPath(request, inputPath, outputFormat);
        var workingDirectory = Path.Combine(
            Path.GetTempPath(),
            "AdaptiveSpritesDmiTool.Batch",
            Guid.NewGuid().ToString("N"));

        try
        {
            var targetDepth = outputFormat == WorkspaceBatchOutputFormat.Png
                ? rasterSettings.DirectionDepth
                : (SpriteDirectionDepth?)null;
            var builder = new MappedSpriteDocumentBuilder(_frameSource!, _probeService!);
            var documentResult = await builder
                .BuildAsync(sourceDocument, request.Config, targetDepth, workingDirectory, cancellationToken)
                .ConfigureAwait(false);
            if (documentResult.IsFailure)
            {
                return CreateFailureResult(inputPath, outputPath, documentResult.Error);
            }

            var exportFormat = outputFormat == WorkspaceBatchOutputFormat.Dmi
                ? SpriteDocumentExportFormat.Dmi
                : rasterSettings.Layout;
            var exportResult = await _documentExporter!
                .ExportAsync(
                    new SpriteDocumentExportRequest(
                        documentResult.Value,
                        outputPath,
                        exportFormat,
                        request.OverwritePolicy),
                    cancellationToken)
                .ConfigureAwait(false);
            if (exportResult.IsFailure)
            {
                return CreateFailureResult(inputPath, outputPath, exportResult.Error);
            }

            return exportResult.Value.WrittenFiles.Count == 0
                ? new BatchFileResult(
                    inputPath,
                    outputPath,
                    BatchFileStatus.Skipped,
                    $"Skipped {outputFormat} output because it already exists.")
                : new BatchFileResult(
                    inputPath,
                    outputPath,
                    BatchFileStatus.Processed,
                    $"Processed {outputFormat} output.");
        }
        catch (OperationCanceledException)
        {
            return CreateCancelledResult(inputPath, outputPath);
        }
        catch (Exception exception)
        {
            return new BatchFileResult(
                inputPath,
                outputPath,
                BatchFileStatus.Failed,
                $"Unexpected {outputFormat} batch failure: {exception.Message}");
        }
        finally
        {
            TryDeleteWorkingDirectory(workingDirectory);
        }
    }

    private static Result<IReadOnlyList<WorkspaceBatchOutputFormat>> ValidateOutputFormats(
        IReadOnlyList<WorkspaceBatchOutputFormat> requestedFormats)
    {
        if (requestedFormats.Count == 0)
        {
            return Result.Failure<IReadOnlyList<WorkspaceBatchOutputFormat>>(
                Errors.Validation("At least one batch output format must be selected."));
        }

        var formats = requestedFormats.Distinct().ToArray();
        if (formats.Any(static format => format is not (WorkspaceBatchOutputFormat.Dmi or WorkspaceBatchOutputFormat.Png)))
        {
            return Result.Failure<IReadOnlyList<WorkspaceBatchOutputFormat>>(
                Errors.Validation("Unsupported batch output format."));
        }

        return Result.Success<IReadOnlyList<WorkspaceBatchOutputFormat>>(formats);
    }

    private static Dictionary<string, string> FindOutputCollisions(
        BatchJobRequest request,
        IReadOnlyList<string> inputFiles,
        IReadOnlyList<WorkspaceBatchOutputFormat> formats)
    {
        var jobs = inputFiles.SelectMany(inputPath => formats.Select(format => new
        {
            InputPath = inputPath,
            Format = format,
            OutputPath = BuildOutputPath(request, inputPath, format)
        }));
        var collisions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in jobs.GroupBy(static job => job.OutputPath, StringComparer.OrdinalIgnoreCase))
        {
            var collidingJobs = group.ToArray();
            if (collidingJobs.Length < 2)
            {
                continue;
            }

            var message = $"Output path collision: multiple inputs resolve to '{group.Key}'.";
            foreach (var job in collidingJobs)
            {
                collisions[BuildOutputKey(job.InputPath, job.Format)] = message;
            }
        }

        return collisions;
    }

    private static string BuildOutputKey(string inputPath, WorkspaceBatchOutputFormat format) =>
        $"{Path.GetFullPath(inputPath)}\0{(int)format}";

    private static string BuildOutputPath(
        BatchJobRequest request,
        string inputPath,
        WorkspaceBatchOutputFormat outputFormat)
    {
        var layoutPath = BatchPathLayout.BuildOutputPath(
            request.InputDirectory,
            request.OutputDirectory,
            inputPath);
        return outputFormat switch
        {
            WorkspaceBatchOutputFormat.Dmi => Path.GetFullPath(Path.ChangeExtension(layoutPath, ".dmi")),
            WorkspaceBatchOutputFormat.Png => Path.GetFullPath(Path.ChangeExtension(layoutPath, null) + ".png-export"),
            _ => Path.GetFullPath(layoutPath)
        };
    }

    private static BatchFileResult? EvaluateFileOverwritePolicy(
        OverwritePolicy overwritePolicy,
        string inputPath,
        string outputPath) =>
        overwritePolicy switch
        {
            OverwritePolicy.OverwriteExisting => null,
            OverwritePolicy.SkipExisting when File.Exists(outputPath) =>
                new BatchFileResult(inputPath, outputPath, BatchFileStatus.Skipped, "Skipped because output file already exists."),
            OverwritePolicy.FailIfExists when File.Exists(outputPath) =>
                new BatchFileResult(inputPath, outputPath, BatchFileStatus.Failed, "Output file already exists."),
            _ => null
        };

    private static BatchFileResult CreateCancelledResult(string inputPath, string? outputPath = null) =>
        new(inputPath, outputPath, BatchFileStatus.Cancelled, "Batch processing was cancelled.");

    private static BatchFileResult CreateFailureResult(string inputPath, string? outputPath, Error error) =>
        new(
            inputPath,
            outputPath,
            error.Code == "cancelled" ? BatchFileStatus.Cancelled : BatchFileStatus.Failed,
            error.Message);

    private static void ReportProgress(
        IProgress<BatchProgress>? progress,
        int processedFiles,
        int totalFiles,
        string? currentFile) =>
        progress?.Report(new BatchProgress(processedFiles, totalFiles, currentFile));

    private static void TryDeleteWorkingDirectory(string path)
    {
        try
        {
            var temporaryRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.Batch"));
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath) && BatchPathLayout.IsPathUnderDirectory(fullPath, temporaryRoot))
            {
                Directory.Delete(fullPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
