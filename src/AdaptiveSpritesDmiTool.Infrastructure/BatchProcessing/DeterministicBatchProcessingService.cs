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

        var inputFiles = ResolveInputFiles(request).ToArray();
        var results = new List<BatchFileResult>(inputFiles.Length);

        for (var index = 0; index < inputFiles.Length; index++)
        {
            var inputPath = inputFiles[index];
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(new BatchFileResult(inputPath, null, BatchFileStatus.Cancelled, "Batch processing was cancelled."));
                continue;
            }

            progress?.Report(new BatchProgress(index + 1, inputFiles.Length, inputPath));

            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(new BatchFileResult(inputPath, null, BatchFileStatus.Cancelled, "Batch processing was cancelled."));
                continue;
            }

            var outputPath = BuildOutputPath(request, inputPath);
            var overwriteDecision = EvaluateOverwritePolicy(request.OverwritePolicy, inputPath, outputPath);
            if (overwriteDecision is not null)
            {
                results.Add(overwriteDecision);
                continue;
            }

            var applyRequest = new ApplyConfigToFileRequest(inputPath, outputPath, request.Config, request.OverwritePolicy);
            var applyResult = await dmiWriter.ApplyAsync(applyRequest, cancellationToken);

            results.Add(
                applyResult.IsSuccess
                    ? applyResult.Value
                    : new BatchFileResult(inputPath, outputPath, BatchFileStatus.Failed, applyResult.Error.Message));
        }

        progress?.Report(new BatchProgress(results.Count, inputFiles.Length, null));
        return Result.Success(new BatchJobResult(results));
    }

    private static IEnumerable<string> ResolveInputFiles(BatchJobRequest request)
    {
        if (request.ExplicitFiles is { Count: > 0 })
        {
            return request.ExplicitFiles
                .Where(static file => !string.IsNullOrWhiteSpace(file))
                .Select(static file => Path.GetFullPath(file))
                .OrderBy(static file => file, StringComparer.Ordinal);
        }

        return SystemFileSystem
            .EnumerateFiles(request.InputDirectory, "*.dmi", SearchOption.AllDirectories)
            .Select(static file => Path.GetFullPath(file))
            .OrderBy(static file => file, StringComparer.Ordinal);
    }

    private static string BuildOutputPath(BatchJobRequest request, string inputPath)
    {
        var fileName = Path.GetFileName(inputPath);
        return Path.Combine(request.OutputDirectory, fileName);
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

    private static class SystemFileSystem
    {
        public static bool DirectoryExists(string path) => Directory.Exists(path);

        public static bool FileExists(string path) => File.Exists(path);

        public static void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
            Directory.EnumerateFiles(path, searchPattern, searchOption);
    }
}
