using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;

namespace AdaptiveSpritesDmiTool.Application;

public enum BatchRunMode
{
    Incremental = 0,
    RebuildAll = 1
}

public sealed record BatchArtifactsInfo(
    string RootDirectory,
    string? ManifestPath,
    string JournalPath,
    string RunReportPath,
    string RunSummaryPath);

public sealed record BatchManifest(
    string OutputRoot,
    BatchRunMode DefaultRunMode,
    IReadOnlyList<BatchManifestJob> Jobs);

public sealed record BatchManifestJob(
    string JobId,
    string Title,
    bool Enabled,
    string InputDirectory,
    string OutputSubdirectory,
    string ConfigPath,
    OverwritePolicy OverwritePolicy,
    IReadOnlyList<string>? ExplicitFiles = null);

public sealed record BatchJobExecutionResult(
    string JobId,
    string Title,
    BatchJobResult Result);

public sealed record BatchJobRunResult(
    BatchJobExecutionResult Job,
    BatchArtifactsInfo Artifacts);

public sealed record BatchManifestRunResult(
    IReadOnlyList<BatchJobExecutionResult> Jobs,
    BatchArtifactsInfo Artifacts)
{
    public IReadOnlyList<BatchFileResult> Files { get; } =
        Jobs.SelectMany(static job => job.Result.Files).ToArray();

    public bool WasCancelled => Jobs.Any(static job => job.Result.WasCancelled);
}

public sealed record BatchJournalEntry(
    string JobId,
    string InputPath,
    string OutputPath,
    string InputFingerprint,
    string ConfigFingerprint,
    DateTimeOffset ProcessedUtc,
    string RunId);

public sealed record BatchProcessedJournal(IReadOnlyList<BatchJournalEntry> Entries);

public sealed record BatchRunReport(
    string RunId,
    string OutputRoot,
    string? ManifestPath,
    BatchRunMode RunMode,
    DateTimeOffset StartedUtc,
    DateTimeOffset CompletedUtc,
    IReadOnlyList<BatchRunReportJob> Jobs)
{
    public int ProcessedCount => Jobs.Sum(static job => job.ProcessedCount);

    public int SkippedCount => Jobs.Sum(static job => job.SkippedCount);

    public int FailedCount => Jobs.Sum(static job => job.FailedCount);

    public int CancelledCount => Jobs.Sum(static job => job.CancelledCount);
}

public sealed record BatchRunReportJob(
    string JobId,
    string Title,
    string InputDirectory,
    string OutputDirectory,
    string? ConfigPath,
    OverwritePolicy OverwritePolicy,
    IReadOnlyList<BatchRunReportFile> Files)
{
    public int ProcessedCount => Files.Count(static file => file.Status == BatchFileStatus.Processed);

    public int SkippedCount => Files.Count(static file => file.Status == BatchFileStatus.Skipped);

    public int FailedCount => Files.Count(static file => file.Status == BatchFileStatus.Failed);

    public int CancelledCount => Files.Count(static file => file.Status == BatchFileStatus.Cancelled);
}

public sealed record BatchRunReportFile(
    string InputPath,
    string? OutputPath,
    BatchFileStatus Status,
    string Message,
    bool SkippedByJournal);

public interface IBatchManifestRepository
{
    Task<Result<BatchManifest>> LoadAsync(string path, CancellationToken cancellationToken);

    Task<Result> SaveAsync(string path, BatchManifest manifest, CancellationToken cancellationToken);
}

public interface IBatchArtifactsStore
{
    Task<Result<BatchProcessedJournal>> LoadJournalAsync(string outputRoot, CancellationToken cancellationToken);

    Task<Result> SaveJournalAsync(string outputRoot, BatchProcessedJournal journal, CancellationToken cancellationToken);

    Task<Result<BatchArtifactsInfo>> WriteRunArtifactsAsync(string outputRoot, BatchRunReport report, CancellationToken cancellationToken);
}

public interface IBatchFingerprintService
{
    Task<Result<string>> ComputeInputFingerprintAsync(string inputPath, CancellationToken cancellationToken);

    string ComputeConfigFingerprint(SpriteConfig config);
}
