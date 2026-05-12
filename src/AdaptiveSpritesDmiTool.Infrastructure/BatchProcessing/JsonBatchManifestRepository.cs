using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using Newtonsoft.Json;
using System.Globalization;

namespace AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;

public sealed class JsonBatchManifestRepository : IBatchManifestRepository
{
    private const int CurrentVersion = 1;

    public async Task<Result<BatchManifest>> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure<BatchManifest>(Errors.Validation("Manifest path is required."));
        }

        var normalizedPath = Path.GetFullPath(path);
        if (!File.Exists(normalizedPath))
        {
            return Result.Failure<BatchManifest>(Errors.NotFound($"Manifest file '{normalizedPath}' was not found."));
        }

        try
        {
            var json = await File.ReadAllTextAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
            var document = JsonConvert.DeserializeObject<BatchManifestDocument>(json);
            if (document is null)
            {
                return Result.Failure<BatchManifest>(Errors.Validation("Manifest file is empty or malformed."));
            }

            if (document.Version != CurrentVersion)
            {
                return Result.Failure<BatchManifest>(Errors.Validation($"Unsupported manifest version '{document.Version}'."));
            }

            var manifestDirectory = Path.GetDirectoryName(normalizedPath) ?? Path.GetDirectoryName(normalizedPath)!;
            return Result.Success(
                new BatchManifest(
                    ResolvePath(manifestDirectory, document.OutputRoot),
                    ParseRunMode(document.DefaultRunMode),
                    document.Jobs.Select(job =>
                            new BatchManifestJob(
                                NormalizeRequired(job.JobId),
                                NormalizeOptional(job.Title) ?? NormalizeRequired(job.JobId),
                                job.Enabled,
                                ResolvePath(manifestDirectory, job.InputDirectory),
                                NormalizeOptional(job.OutputSubdirectory) ?? string.Empty,
                                ResolvePath(manifestDirectory, job.ConfigPath),
                                ParseOverwritePolicy(job.OverwritePolicy),
                                job.ExplicitFiles?.Select(file => ResolvePath(manifestDirectory, file)).ToArray()))
                        .ToArray()));
        }
        catch (JsonException exception)
        {
            return Result.Failure<BatchManifest>(Errors.Validation($"Manifest JSON is invalid: {exception.Message}"));
        }
        catch (Exception exception)
        {
            return Result.Failure<BatchManifest>(Errors.Unexpected($"Failed to load manifest: {exception.Message}"));
        }
    }

    public async Task<Result> SaveAsync(string path, BatchManifest manifest, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure(Errors.Validation("Manifest path is required."));
        }

        ArgumentNullException.ThrowIfNull(manifest);

        try
        {
            var normalizedPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var manifestDirectory = directory ?? Path.GetDirectoryName(normalizedPath)!;
            var document = new BatchManifestDocument
            {
                Version = CurrentVersion,
                OutputRoot = ToStoredPath(manifestDirectory, manifest.OutputRoot),
                DefaultRunMode = manifest.DefaultRunMode.ToString(),
                Jobs = manifest.Jobs.Select(job => new BatchManifestJobDocument
                {
                    JobId = job.JobId,
                    Title = job.Title,
                    Enabled = job.Enabled,
                    InputDirectory = ToStoredPath(manifestDirectory, job.InputDirectory),
                    OutputSubdirectory = job.OutputSubdirectory,
                    ConfigPath = ToStoredPath(manifestDirectory, job.ConfigPath),
                    OverwritePolicy = job.OverwritePolicy.ToString(),
                    ExplicitFiles = job.ExplicitFiles?.Select(file => ToStoredPath(manifestDirectory, file)).ToArray()
                }).ToArray()
            };

            var json = JsonConvert.SerializeObject(document, Formatting.Indented);
            await File.WriteAllTextAsync(normalizedPath, json, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure(Errors.Unexpected($"Failed to save manifest: {exception.Message}"));
        }
    }

    private static string ResolvePath(string manifestDirectory, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(value)
            ? Path.GetFullPath(value)
            : Path.GetFullPath(Path.Combine(manifestDirectory, value));
    }

    private static string ToStoredPath(string manifestDirectory, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Path.GetRelativePath(manifestDirectory, Path.GetFullPath(value));
    }

    private static string NormalizeRequired(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Manifest contains an empty required field.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BatchRunMode ParseRunMode(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !Enum.TryParse<BatchRunMode>(value, true, out var mode) ||
            !Enum.IsDefined(mode))
        {
            throw new JsonException($"Unsupported batch run mode '{value}'.");
        }

        return mode;
    }

    private static OverwritePolicy ParseOverwritePolicy(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !Enum.TryParse<OverwritePolicy>(value, true, out var policy) ||
            !Enum.IsDefined(policy))
        {
            throw new JsonException($"Unsupported overwrite policy '{value}'.");
        }

        return policy;
    }

    private sealed class BatchManifestDocument
    {
        public int Version { get; set; } = CurrentVersion;

        public string OutputRoot { get; set; } = string.Empty;

        public string DefaultRunMode { get; set; } = BatchRunMode.Incremental.ToString();

        public BatchManifestJobDocument[] Jobs { get; set; } = [];
    }

    private sealed class BatchManifestJobDocument
    {
        public string JobId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        public string InputDirectory { get; set; } = string.Empty;

        public string OutputSubdirectory { get; set; } = string.Empty;

        public string ConfigPath { get; set; } = string.Empty;

        public string OverwritePolicy { get; set; } =
            nameof(AdaptiveSpritesDmiTool.Application.OverwritePolicy.SkipExisting);

        public string[]? ExplicitFiles { get; set; }
    }
}
