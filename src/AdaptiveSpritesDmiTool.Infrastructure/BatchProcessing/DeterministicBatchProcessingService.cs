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
                    : new BatchFileResult(
                        inputPath,
                        outputPath,
                        applyResult.Error.Code == "cancelled" ? BatchFileStatus.Cancelled : BatchFileStatus.Failed,
                        applyResult.Error.Message));
        }

        progress?.Report(new BatchProgress(results.Count, inputFiles.Length, null));
        return Result.Success(new BatchJobResult(results));
    }

    private static IEnumerable<string> ResolveInputFiles(BatchJobRequest request)
    {
        var inputDirectory = Path.GetFullPath(request.InputDirectory);
        var outputDirectory = Path.GetFullPath(request.OutputDirectory);
        var excludeOutputDirectory = IsStrictSubdirectory(inputDirectory, outputDirectory);

        if (request.ExplicitFiles is { Count: > 0 })
        {
            return request.ExplicitFiles
                .Where(static file => !string.IsNullOrWhiteSpace(file))
                .Select(static file => Path.GetFullPath(file))
                .Where(file => !excludeOutputDirectory || !IsPathUnderDirectory(file, outputDirectory))
                .OrderBy(static file => file, StringComparer.Ordinal);
        }

        return SystemFileSystem
            .EnumerateFiles(inputDirectory, "*.dmi", SearchOption.AllDirectories)
            .Select(static file => Path.GetFullPath(file))
            .Where(file => !excludeOutputDirectory || !IsPathUnderDirectory(file, outputDirectory))
            .OrderBy(static file => file, StringComparer.Ordinal);
    }

    private static string BuildOutputPath(BatchJobRequest request, string inputPath)
    {
        var inputDirectory = Path.GetFullPath(request.InputDirectory);
        var fullInputPath = Path.GetFullPath(inputPath);
        var relativePath = Path.GetRelativePath(inputDirectory, fullInputPath);

        return IsRelativePathInsideRoot(relativePath)
            ? Path.Combine(request.OutputDirectory, relativePath)
            : Path.Combine(request.OutputDirectory, Path.GetFileName(fullInputPath));
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

    private static bool IsRelativePathInsideRoot(string relativePath) =>
        !string.IsNullOrWhiteSpace(relativePath) &&
        !Path.IsPathRooted(relativePath) &&
        !relativePath.Equals(".", StringComparison.Ordinal) &&
        !relativePath.StartsWith("..", StringComparison.Ordinal) &&
        !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
        !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

    private static bool IsStrictSubdirectory(string parentDirectory, string candidateDirectory) =>
        !string.Equals(parentDirectory, candidateDirectory, PathComparison) &&
        IsPathUnderDirectory(candidateDirectory, parentDirectory);

    private static bool IsPathUnderDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = EnsureTrailingSeparator(Path.GetFullPath(directory));
        return fullPath.StartsWith(fullDirectory, PathComparison);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
