using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;

namespace AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;

public sealed class DeterministicBatchProcessingService(IDmiWriter dmiWriter)
    : IBatchProcessingService
{
    public async Task<Result<BatchJobResult>> RunAsync(
        BatchJobRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SystemFileSystem.DirectoryExists(request.InputDirectory))
        {
            return Result.Failure<BatchJobResult>(Errors.NotFound($"Input directory '{request.InputDirectory}' was not found."));
        }

        SystemFileSystem.CreateDirectory(request.OutputDirectory);

        var inputFiles = BatchPathLayout.ResolveInputFiles(request.InputDirectory, request.OutputDirectory, request.ExplicitFiles).ToArray();
        var results = new List<BatchFileResult>(inputFiles.Length);

        for (var index = 0; index < inputFiles.Length; index++)
        {
            var inputPath = inputFiles[index];
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(new BatchFileResult(inputPath, null, BatchFileStatus.Cancelled, "Batch processing was cancelled."));
                ReportProgress(progress, results.Count, inputFiles.Length, inputPath);
                continue;
            }

            var outputPath = BatchPathLayout.BuildOutputPath(request.InputDirectory, request.OutputDirectory, inputPath);
            var overwriteDecision = EvaluateOverwritePolicy(request.OverwritePolicy, inputPath, outputPath);
            if (overwriteDecision is not null)
            {
                results.Add(overwriteDecision);
                ReportProgress(progress, results.Count, inputFiles.Length, inputPath);
                continue;
            }

            var applyRequest = new ApplyConfigToFileRequest(inputPath, outputPath, request.Config, request.OverwritePolicy);
            var applyResult = await dmiWriter.ApplyAsync(applyRequest, cancellationToken);

            results.Add(
                applyResult.IsSuccess
                    ? applyResult.Value
                    : new BatchFileResult(
                        inputPath,
                        outputPath,
                        applyResult.Error.Code == "cancelled" ? BatchFileStatus.Cancelled : BatchFileStatus.Failed,
                        applyResult.Error.Message));
            ReportProgress(progress, results.Count, inputFiles.Length, inputPath);
        }

        ReportProgress(progress, results.Count, inputFiles.Length, null);
        return Result.Success(new BatchJobResult(results));
    }

    private static BatchFileResult? EvaluateOverwritePolicy(OverwritePolicy overwritePolicy, string inputPath, string outputPath) =>
        overwritePolicy switch
        {
            OverwritePolicy.OverwriteExisting => null,
            OverwritePolicy.SkipExisting when SystemFileSystem.FileExists(outputPath) =>
                new BatchFileResult(inputPath, outputPath, BatchFileStatus.Skipped, "Skipped because output file already exists."),
            OverwritePolicy.FailIfExists when SystemFileSystem.FileExists(outputPath) =>
                new BatchFileResult(inputPath, outputPath, BatchFileStatus.Failed, "Output file already exists."),
            _ => null
        };

    private static void ReportProgress(IProgress<BatchProgress>? progress, int processedFiles, int totalFiles, string? currentFile) =>
        progress?.Report(new BatchProgress(processedFiles, totalFiles, currentFile));

    private static class SystemFileSystem
    {
        public static bool DirectoryExists(string path) => Directory.Exists(path);

        public static bool FileExists(string path) => File.Exists(path);

        public static void CreateDirectory(string path) => Directory.CreateDirectory(path);

    }
}
