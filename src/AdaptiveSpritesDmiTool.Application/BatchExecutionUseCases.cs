using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;

namespace AdaptiveSpritesDmiTool.Application;

public sealed class LoadBatchManifestUseCase(IBatchManifestRepository repository)
{
    public Task<Result<BatchManifest>> ExecuteAsync(string path, CancellationToken cancellationToken) =>
        repository.LoadAsync(path, cancellationToken);
}

public sealed class SaveBatchManifestUseCase(IBatchManifestRepository repository)
{
    public Task<Result> ExecuteAsync(string path, BatchManifest manifest, CancellationToken cancellationToken) =>
        repository.SaveAsync(path, manifest, cancellationToken);
}

public sealed class ExecuteTrackedBatchJobUseCase(
    IBatchProcessingService batchProcessingService,
    IBatchArtifactsStore artifactsStore,
    IBatchFingerprintService fingerprintService,
    EditorSession session)
{
    public async Task<Result<BatchJobRunResult>> ExecuteAsync(
        string inputDirectory,
        string outputDirectory,
        OverwritePolicy overwritePolicy,
        BatchRunMode runMode,
        IProgress<BatchProgress>? progress,
        IReadOnlyList<string>? explicitFiles,
        CancellationToken cancellationToken)
    {
        if (session.CurrentConfig is null)
        {
            return Result.Failure<BatchJobRunResult>(Errors.Conflict("There is no active config to apply."));
        }

        var candidateFiles = BatchPathLayout.ResolveInputFiles(inputDirectory, outputDirectory, explicitFiles);
        var startedUtc = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid().ToString("N");
        var configPath = string.IsNullOrWhiteSpace(session.CurrentConfigPath) ? null : session.CurrentConfigPath;
        var configFingerprint = fingerprintService.ComputeConfigFingerprint(session.CurrentConfig);

        var journalResult = await artifactsStore.LoadJournalAsync(outputDirectory, cancellationToken);
        if (journalResult.IsFailure)
        {
            return Result.Failure<BatchJobRunResult>(journalResult.Error);
        }

        var journalEntries = journalResult.Value.Entries.ToDictionary(
            static entry => BatchExecutionKeys.BuildJournalKey(entry.JobId, entry.InputPath),
            static entry => entry,
            StringComparer.OrdinalIgnoreCase);

        var snapshotResult = await BatchExecutionCoordinator.ExecuteJobAsync(
            new BatchExecutionPlan(
                JobId: "manual-batch",
                Title: string.IsNullOrWhiteSpace(session.CurrentConfig.Name) ? "Manual batch" : session.CurrentConfig.Name,
                InputDirectory: inputDirectory,
                OutputDirectory: outputDirectory,
                ConfigPath: configPath,
                Config: session.CurrentConfig,
                OverwritePolicy: overwritePolicy,
                CandidateFiles: candidateFiles,
                ConfigFingerprint: configFingerprint),
            journalEntries,
            runMode,
            batchProcessingService,
            fingerprintService,
            progress,
            progressOffset: 0,
            progressTotal: candidateFiles.Count,
            runId,
            cancellationToken);
        if (snapshotResult.IsFailure)
        {
            return Result.Failure<BatchJobRunResult>(snapshotResult.Error);
        }

        var completedUtc = DateTimeOffset.UtcNow;
        var report = new BatchRunReport(
            runId,
            Path.GetFullPath(outputDirectory),
            ManifestPath: null,
            runMode,
            startedUtc,
            completedUtc,
            [snapshotResult.Value.ReportJob]);

        var saveJournalResult = await artifactsStore.SaveJournalAsync(
            outputDirectory,
            new BatchProcessedJournal(journalEntries.Values.OrderBy(static entry => entry.JobId, StringComparer.Ordinal)
                .ThenBy(static entry => entry.InputPath, StringComparer.Ordinal)
                .ToArray()),
            cancellationToken);
        if (saveJournalResult.IsFailure)
        {
            return Result.Failure<BatchJobRunResult>(saveJournalResult.Error);
        }

        var artifactsResult = await artifactsStore.WriteRunArtifactsAsync(outputDirectory, report, cancellationToken);
        if (artifactsResult.IsFailure)
        {
            return Result.Failure<BatchJobRunResult>(artifactsResult.Error);
        }

        return Result.Success(new BatchJobRunResult(snapshotResult.Value.Job, artifactsResult.Value));
    }
}

public sealed class RunBatchManifestUseCase(
    IBatchManifestRepository manifestRepository,
    IConfigRepository configRepository,
    IBatchProcessingService batchProcessingService,
    IBatchArtifactsStore artifactsStore,
    IBatchFingerprintService fingerprintService)
{
    public async Task<Result<BatchManifestRunResult>> ExecuteAsync(
        string manifestPath,
        BatchRunMode runMode,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken)
    {
        var manifestResult = await manifestRepository.LoadAsync(manifestPath, cancellationToken);
        if (manifestResult.IsFailure)
        {
            return Result.Failure<BatchManifestRunResult>(manifestResult.Error);
        }

        var validationResult = ValidateManifest(manifestResult.Value);
        if (validationResult.IsFailure)
        {
            return Result.Failure<BatchManifestRunResult>(validationResult.Error);
        }

        var enabledJobs = manifestResult.Value.Jobs
            .Where(static job => job.Enabled)
            .ToArray();

        var plannedJobs = new List<ManifestJobPlan>(enabledJobs.Length);
        foreach (var job in enabledJobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var configResult = await configRepository.LoadAsync(job.ConfigPath, cancellationToken);
            if (configResult.IsFailure)
            {
                return Result.Failure<BatchManifestRunResult>(configResult.Error);
            }

            var outputDirectory = ResolveManifestJobOutputDirectory(manifestResult.Value.OutputRoot, job.OutputSubdirectory);
            var candidateFiles = BatchPathLayout.ResolveInputFiles(job.InputDirectory, outputDirectory, job.ExplicitFiles);
            plannedJobs.Add(
                new ManifestJobPlan(
                    job,
                    outputDirectory,
                    configResult.Value,
                    BatchExecutionCoordinator.ComputeConfigFingerprint(fingerprintService, configResult.Value),
                    candidateFiles));
        }

        var runId = Guid.NewGuid().ToString("N");
        var startedUtc = DateTimeOffset.UtcNow;
        var totalFiles = plannedJobs.Sum(static job => job.CandidateFiles.Count);
        var journalResult = await artifactsStore.LoadJournalAsync(manifestResult.Value.OutputRoot, cancellationToken);
        if (journalResult.IsFailure)
        {
            return Result.Failure<BatchManifestRunResult>(journalResult.Error);
        }

        var journalEntries = journalResult.Value.Entries.ToDictionary(
            static entry => BatchExecutionKeys.BuildJournalKey(entry.JobId, entry.InputPath),
            static entry => entry,
            StringComparer.OrdinalIgnoreCase);

        var jobResults = new List<BatchJobExecutionResult>(plannedJobs.Count);
        var reportJobs = new List<BatchRunReportJob>(plannedJobs.Count);
        var completedFiles = 0;

        foreach (var job in plannedJobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshotResult = await BatchExecutionCoordinator.ExecuteJobAsync(
                new BatchExecutionPlan(
                    job.Job.JobId,
                    string.IsNullOrWhiteSpace(job.Job.Title) ? job.Job.JobId : job.Job.Title,
                    job.Job.InputDirectory,
                    job.OutputDirectory,
                    job.Job.ConfigPath,
                    job.Config,
                    job.Job.OverwritePolicy,
                    job.CandidateFiles,
                    job.ConfigFingerprint),
                journalEntries,
                runMode,
                batchProcessingService,
                fingerprintService,
                progress,
                completedFiles,
                totalFiles,
                runId,
                cancellationToken);
            if (snapshotResult.IsFailure)
            {
                return Result.Failure<BatchManifestRunResult>(snapshotResult.Error);
            }

            completedFiles += job.CandidateFiles.Count;
            jobResults.Add(snapshotResult.Value.Job);
            reportJobs.Add(snapshotResult.Value.ReportJob);
        }

        var report = new BatchRunReport(
            runId,
            manifestResult.Value.OutputRoot,
            Path.GetFullPath(manifestPath),
            runMode,
            startedUtc,
            DateTimeOffset.UtcNow,
            reportJobs);

        var saveJournalResult = await artifactsStore.SaveJournalAsync(
            manifestResult.Value.OutputRoot,
            new BatchProcessedJournal(journalEntries.Values.OrderBy(static entry => entry.JobId, StringComparer.Ordinal)
                .ThenBy(static entry => entry.InputPath, StringComparer.Ordinal)
                .ToArray()),
            cancellationToken);
        if (saveJournalResult.IsFailure)
        {
            return Result.Failure<BatchManifestRunResult>(saveJournalResult.Error);
        }

        var artifactsResult = await artifactsStore.WriteRunArtifactsAsync(manifestResult.Value.OutputRoot, report, cancellationToken);
        if (artifactsResult.IsFailure)
        {
            return Result.Failure<BatchManifestRunResult>(artifactsResult.Error);
        }

        return Result.Success(new BatchManifestRunResult(jobResults, artifactsResult.Value));
    }

    private static Result ValidateManifest(BatchManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.OutputRoot))
        {
            return Result.Failure(Errors.Validation("Manifest output root is required."));
        }

        var enabledJobs = manifest.Jobs.Where(static job => job.Enabled).ToArray();
        if (enabledJobs.Length == 0)
        {
            return Result.Failure(Errors.Validation("Manifest does not contain any enabled jobs."));
        }

        var jobIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var job in manifest.Jobs)
        {
            if (string.IsNullOrWhiteSpace(job.JobId))
            {
                return Result.Failure(Errors.Validation("Each manifest job must define a non-empty jobId."));
            }

            if (!jobIds.Add(job.JobId.Trim()))
            {
                return Result.Failure(Errors.Validation($"Manifest contains duplicate jobId '{job.JobId}'."));
            }
        }

        var outputSubdirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var job in enabledJobs)
        {
            if (string.IsNullOrWhiteSpace(job.InputDirectory) || !Directory.Exists(job.InputDirectory))
            {
                return Result.Failure(Errors.Validation($"Input directory '{job.InputDirectory}' was not found for job '{job.JobId}'."));
            }

            if (string.IsNullOrWhiteSpace(job.ConfigPath) || !File.Exists(job.ConfigPath))
            {
                return Result.Failure(Errors.Validation($"Config path '{job.ConfigPath}' was not found for job '{job.JobId}'."));
            }

            var normalizedSubdirectory = NormalizeOutputSubdirectory(job.OutputSubdirectory);

            if (!IsSafeOutputSubdirectory(normalizedSubdirectory))
            {
                return Result.Failure(Errors.Validation($"Output subdirectory '{job.OutputSubdirectory}' is invalid for job '{job.JobId}'."));
            }

            var outputDirectoryKey = NormalizeOutputDirectoryKey(
                manifest.OutputRoot,
                normalizedSubdirectory);

            if (!outputSubdirectories.Add(outputDirectoryKey))
            {
                return Result.Failure(Errors.Validation($"Manifest contains duplicate outputSubdirectory '{job.OutputSubdirectory}' among enabled jobs."));
            }

            if (job.ExplicitFiles is not { Count: > 0 })
            {
                continue;
            }

            foreach (var file in job.ExplicitFiles)
            {
                if (!BatchPathLayout.IsPathUnderDirectory(file, job.InputDirectory))
                {
                    return Result.Failure(Errors.Validation($"Explicit file '{file}' must stay inside input directory '{job.InputDirectory}'."));
                }
            }
        }

        return Result.Success();
    }

    private static bool IsSafeOutputSubdirectory(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return BatchPathLayout.IsRelativePathInsideRoot(value.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
    }

    private static string ResolveManifestJobOutputDirectory(string outputRoot, string outputSubdirectory) =>
        string.IsNullOrWhiteSpace(outputSubdirectory)
            ? Path.GetFullPath(outputRoot)
            : Path.GetFullPath(Path.Combine(outputRoot, outputSubdirectory));

    private static string NormalizeOutputDirectoryKey(
        string outputRoot,
        string outputSubdirectory)
    {
        var outputDirectory = ResolveManifestJobOutputDirectory(
            outputRoot,
            outputSubdirectory);

        return Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(outputDirectory));
    }

    private static string NormalizeOutputSubdirectory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Trim()
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (normalized == ".")
        {
            return string.Empty;
        }

        return Path.TrimEndingDirectorySeparator(normalized);
    }
}

internal static class BatchExecutionCoordinator
{
    public static string ComputeConfigFingerprint(IBatchFingerprintService fingerprintService, SpriteConfig config) =>
        fingerprintService.ComputeConfigFingerprint(config);

    public static async Task<Result<BatchExecutionSnapshot>> ExecuteJobAsync(
        BatchExecutionPlan plan,
        Dictionary<string, BatchJournalEntry> journalEntries,
        BatchRunMode runMode,
        IBatchProcessingService batchProcessingService,
        IBatchFingerprintService fingerprintService,
        IProgress<BatchProgress>? progress,
        int progressOffset,
        int progressTotal,
        string runId,
        CancellationToken cancellationToken)
    {
        var journalSkipResults = new Dictionary<string, BatchFileResult>(StringComparer.OrdinalIgnoreCase);
        var pendingFiles = new List<string>(plan.CandidateFiles.Count);
        var completedFiles = 0;

        foreach (var inputPath in plan.CandidateFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outputPath = BatchPathLayout.BuildOutputPath(plan.InputDirectory, plan.OutputDirectory, inputPath);
            if (runMode != BatchRunMode.Incremental)
            {
                pendingFiles.Add(inputPath);
                continue;
            }

            if (!journalEntries.TryGetValue(BatchExecutionKeys.BuildJournalKey(plan.JobId, inputPath), out var entry) ||
                !File.Exists(outputPath) ||
                !string.Equals(Path.GetFullPath(outputPath), Path.GetFullPath(entry.OutputPath), BatchPathLayout.PathComparison) ||
                !string.Equals(entry.ConfigFingerprint, plan.ConfigFingerprint, StringComparison.Ordinal))
            {
                pendingFiles.Add(inputPath);
                continue;
            }

            var inputFingerprintResult = await fingerprintService.ComputeInputFingerprintAsync(inputPath, cancellationToken);
            if (inputFingerprintResult.IsFailure)
            {
                return Result.Failure<BatchExecutionSnapshot>(inputFingerprintResult.Error);
            }

            if (!string.Equals(entry.InputFingerprint, inputFingerprintResult.Value, StringComparison.Ordinal))
            {
                pendingFiles.Add(inputPath);
                continue;
            }

            journalSkipResults[inputPath] = new BatchFileResult(
                inputPath,
                outputPath,
                BatchFileStatus.Skipped,
                "Skipped because journal indicates current output is up to date.");
            completedFiles++;
            progress?.Report(new BatchProgress(progressOffset + completedFiles, progressTotal, inputPath));
        }

        BatchJobResult processedResult;
        if (pendingFiles.Count == 0)
        {
            processedResult = new BatchJobResult([]);
            progress?.Report(new BatchProgress(progressOffset + completedFiles, progressTotal, null));
        }
        else
        {
            var nestedProgress = progress is null
                ? null
                : new Progress<BatchProgress>(value =>
                    progress.Report(new BatchProgress(progressOffset + completedFiles + value.ProcessedFiles, progressTotal, value.CurrentFile)));

            var batchResult = await batchProcessingService.RunAsync(
                new BatchJobRequest(
                    plan.InputDirectory,
                    plan.OutputDirectory,
                    plan.Config,
                    plan.OverwritePolicy,
                    pendingFiles),
                nestedProgress,
                cancellationToken);
            if (batchResult.IsFailure)
            {
                return Result.Failure<BatchExecutionSnapshot>(batchResult.Error);
            }

            processedResult = batchResult.Value;
        }

        var pendingEnumerator = processedResult.Files.GetEnumerator();
        var orderedResults = new List<BatchFileResult>(plan.CandidateFiles.Count);
        foreach (var inputPath in plan.CandidateFiles)
        {
            if (journalSkipResults.TryGetValue(inputPath, out var skippedResult))
            {
                orderedResults.Add(skippedResult);
                continue;
            }

            if (!pendingEnumerator.MoveNext())
            {
                return Result.Failure<BatchExecutionSnapshot>(Errors.Unexpected("Batch processing returned fewer results than expected."));
            }

            orderedResults.Add(pendingEnumerator.Current);
        }

        if (pendingEnumerator.MoveNext())
        {
            return Result.Failure<BatchExecutionSnapshot>(Errors.Unexpected("Batch processing returned more results than expected."));
        }

        var processedUtc = DateTimeOffset.UtcNow;
        foreach (var file in orderedResults)
        {
            if (file.Status != BatchFileStatus.Processed || string.IsNullOrWhiteSpace(file.OutputPath))
            {
                continue;
            }

            var inputFingerprintResult = await fingerprintService.ComputeInputFingerprintAsync(file.InputPath, cancellationToken);
            if (inputFingerprintResult.IsFailure)
            {
                return Result.Failure<BatchExecutionSnapshot>(inputFingerprintResult.Error);
            }

            journalEntries[BatchExecutionKeys.BuildJournalKey(plan.JobId, file.InputPath)] = new BatchJournalEntry(
                plan.JobId,
                Path.GetFullPath(file.InputPath),
                Path.GetFullPath(file.OutputPath),
                inputFingerprintResult.Value,
                plan.ConfigFingerprint,
                processedUtc,
                runId);
        }

        var reportFiles = orderedResults.Select(file =>
            new BatchRunReportFile(
                Path.GetFullPath(file.InputPath),
                string.IsNullOrWhiteSpace(file.OutputPath) ? null : Path.GetFullPath(file.OutputPath),
                file.Status,
                file.Message,
                journalSkipResults.ContainsKey(file.InputPath)))
            .ToArray();

        return Result.Success(
            new BatchExecutionSnapshot(
                new BatchJobExecutionResult(plan.JobId, plan.Title, new BatchJobResult(orderedResults)),
                new BatchRunReportJob(
                    plan.JobId,
                    plan.Title,
                    Path.GetFullPath(plan.InputDirectory),
                    Path.GetFullPath(plan.OutputDirectory),
                    plan.ConfigPath,
                    plan.OverwritePolicy,
                    reportFiles)));
    }
}

internal sealed record BatchExecutionPlan(
    string JobId,
    string Title,
    string InputDirectory,
    string OutputDirectory,
    string? ConfigPath,
    SpriteConfig Config,
    OverwritePolicy OverwritePolicy,
    IReadOnlyList<string> CandidateFiles,
    string ConfigFingerprint);

internal sealed record BatchExecutionSnapshot(
    BatchJobExecutionResult Job,
    BatchRunReportJob ReportJob);

internal sealed record ManifestJobPlan(
    BatchManifestJob Job,
    string OutputDirectory,
    SpriteConfig Config,
    string ConfigFingerprint,
    IReadOnlyList<string> CandidateFiles);

internal static class BatchExecutionKeys
{
    public static string BuildJournalKey(string jobId, string inputPath) =>
        $"{jobId}|{Path.GetFullPath(inputPath)}";
}
