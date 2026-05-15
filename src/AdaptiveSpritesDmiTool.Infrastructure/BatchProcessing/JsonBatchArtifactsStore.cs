using System.Globalization;
using System.Text;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using Newtonsoft.Json;

namespace AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;

public sealed class JsonBatchArtifactsStore : IBatchArtifactsStore
{
    private const int CurrentVersion = 1;

    public async Task<Result<BatchProcessedJournal>> LoadJournalAsync(string outputRoot, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            return Result.Failure<BatchProcessedJournal>(Errors.Validation("Batch output root is required."));
        }

        var journalPath = GetJournalPath(outputRoot);
        if (!File.Exists(journalPath))
        {
            return Result.Success(new BatchProcessedJournal([]));
        }

        try
        {
            var json = await File.ReadAllTextAsync(journalPath, cancellationToken).ConfigureAwait(false);
            var document = JsonConvert.DeserializeObject<BatchJournalDocument>(json);
            if (document is null)
            {
                return Result.Failure<BatchProcessedJournal>(Errors.Validation("Batch journal file is empty or malformed."));
            }

            if (document.Version != CurrentVersion)
            {
                return Result.Failure<BatchProcessedJournal>(Errors.Validation($"Unsupported batch journal version '{document.Version}'."));
            }

            return Result.Success(
                new BatchProcessedJournal(
                    document.Entries.Select(entry =>
                            new BatchJournalEntry(
                                entry.JobId,
                                entry.InputPath,
                                entry.OutputPath,
                                entry.InputFingerprint,
                                entry.ConfigFingerprint,
                                entry.ProcessedUtc,
                                entry.RunId))
                        .ToArray()));
        }
        catch (JsonException exception)
        {
            return Result.Failure<BatchProcessedJournal>(Errors.Validation($"Batch journal JSON is invalid: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<BatchProcessedJournal>(Errors.Unexpected($"Failed to load batch journal: {exception.Message}"));
        }
    }

    public async Task<Result> SaveJournalAsync(string outputRoot, BatchProcessedJournal journal, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            return Result.Failure(Errors.Validation("Batch output root is required."));
        }

        ArgumentNullException.ThrowIfNull(journal);

        try
        {
            EnsureArtifactsDirectories(outputRoot);
            var document = new BatchJournalDocument
            {
                Version = CurrentVersion,
                Entries = journal.Entries.Select(entry => new BatchJournalEntryDocument
                {
                    JobId = entry.JobId,
                    InputPath = entry.InputPath,
                    OutputPath = entry.OutputPath,
                    InputFingerprint = entry.InputFingerprint,
                    ConfigFingerprint = entry.ConfigFingerprint,
                    ProcessedUtc = entry.ProcessedUtc,
                    RunId = entry.RunId
                }).ToArray()
            };

            var json = JsonConvert.SerializeObject(document, Formatting.Indented);
            await File.WriteAllTextAsync(GetJournalPath(outputRoot), json, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure(Errors.Unexpected($"Failed to save batch journal: {exception.Message}"));
        }
    }

    public async Task<Result<BatchArtifactsInfo>> WriteRunArtifactsAsync(string outputRoot, BatchRunReport report, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            return Result.Failure<BatchArtifactsInfo>(Errors.Validation("Batch output root is required."));
        }

        ArgumentNullException.ThrowIfNull(report);

        try
        {
            EnsureArtifactsDirectories(outputRoot);

            var rootDirectory = GetArtifactsRoot(outputRoot);
            var runReportPath = Path.Combine(rootDirectory, "runs", $"{report.RunId}.json");
            var runSummaryPath = Path.Combine(rootDirectory, "runs", $"{report.RunId}.summary.txt");

            var reportJson = JsonConvert.SerializeObject(report, Formatting.Indented);
            await File.WriteAllTextAsync(runReportPath, reportJson, cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(runSummaryPath, BuildSummary(report), cancellationToken).ConfigureAwait(false);

            return Result.Success(
                new BatchArtifactsInfo(
                    rootDirectory,
                    report.ManifestPath,
                    GetJournalPath(outputRoot),
                    runReportPath,
                    runSummaryPath));
        }
        catch (Exception exception)
        {
            return Result.Failure<BatchArtifactsInfo>(Errors.Unexpected($"Failed to write batch run artifacts: {exception.Message}"));
        }
    }

    private static string BuildSummary(BatchRunReport report)
    {
        var builder = new StringBuilder();
        AppendInvariantLine(builder, $"Run ID: {report.RunId}");
        AppendInvariantLine(builder, $"Run Mode: {report.RunMode}");
        AppendInvariantLine(builder, $"Output Root: {report.OutputRoot}");
        AppendInvariantLine(builder, $"Manifest: {report.ManifestPath ?? "manual batch"}");
        AppendInvariantLine(builder, $"Started: {report.StartedUtc:O}");
        AppendInvariantLine(builder, $"Completed: {report.CompletedUtc:O}");
        AppendInvariantLine(builder, $"Processed: {report.ProcessedCount}");
        AppendInvariantLine(builder, $"Skipped: {report.SkippedCount}");
        AppendInvariantLine(builder, $"Failed: {report.FailedCount}");
        AppendInvariantLine(builder, $"Cancelled: {report.CancelledCount}");

        foreach (var job in report.Jobs)
        {
            builder.AppendLine();
            AppendInvariantLine(builder, $"[{job.JobId}] {job.Title}");
            AppendInvariantLine(builder, $"Input: {job.InputDirectory}");
            AppendInvariantLine(builder, $"Output: {job.OutputDirectory}");
            AppendInvariantLine(builder, $"Config: {job.ConfigPath ?? "unsaved session config"}");
            AppendInvariantLine(builder, $"Overwrite Policy: {job.OverwritePolicy}");
            AppendInvariantLine(builder, $"Processed: {job.ProcessedCount}, Skipped: {job.SkippedCount}, Failed: {job.FailedCount}, Cancelled: {job.CancelledCount}");

            foreach (var file in job.Files)
            {
                builder.Append("- [")
                    .Append(file.Status)
                    .Append("] ")
                    .Append(file.InputPath);

                if (!string.IsNullOrWhiteSpace(file.OutputPath))
                {
                    builder.Append(" -> ").Append(file.OutputPath);
                }

                if (file.SkippedByJournal)
                {
                    builder.Append(" (journal)");
                }

                builder.Append(": ").AppendLine(file.Message);
            }
        }

        return builder.ToString();
    }

    private static void AppendInvariantLine(StringBuilder builder, FormattableString value) =>
        builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));

    private static void EnsureArtifactsDirectories(string outputRoot)
    {
        var rootDirectory = GetArtifactsRoot(outputRoot);
        Directory.CreateDirectory(rootDirectory);
        Directory.CreateDirectory(Path.Combine(rootDirectory, "runs"));
    }

    private static string GetArtifactsRoot(string outputRoot) =>
        Path.Combine(Path.GetFullPath(outputRoot), ".adaptive-sprites");

    private static string GetJournalPath(string outputRoot) =>
        Path.Combine(GetArtifactsRoot(outputRoot), "processed-journal.json");

    private sealed class BatchJournalDocument
    {
        public int Version { get; set; } = CurrentVersion;

        public BatchJournalEntryDocument[] Entries { get; set; } = [];
    }

    private sealed class BatchJournalEntryDocument
    {
        public string JobId { get; set; } = string.Empty;

        public string InputPath { get; set; } = string.Empty;

        public string OutputPath { get; set; } = string.Empty;

        public string InputFingerprint { get; set; } = string.Empty;

        public string ConfigFingerprint { get; set; } = string.Empty;

        public DateTimeOffset ProcessedUtc { get; set; }

        public string RunId { get; set; } = string.Empty;
    }
}
