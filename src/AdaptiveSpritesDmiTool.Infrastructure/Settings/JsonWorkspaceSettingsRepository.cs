using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using Newtonsoft.Json;
using System.Globalization;

namespace AdaptiveSpritesDmiTool.Infrastructure.Settings;

public sealed class JsonWorkspaceSettingsRepository(string filePath) : ISettingsRepository
{
    private const int CurrentVersion = 4;

    public async Task<Result<WorkspaceSettings>> LoadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Failure<WorkspaceSettings>(Errors.Validation("Workspace settings path is required."));
        }

        if (!File.Exists(filePath))
        {
            return Result.Failure<WorkspaceSettings>(Errors.NotFound($"Workspace settings file '{filePath}' was not found."));
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            var document = JsonConvert.DeserializeObject<WorkspaceSettingsDocument>(json);
            if (document is null)
            {
                return Result.Failure<WorkspaceSettings>(Errors.Validation("Workspace settings file is empty or malformed."));
            }

            if (document.Version is < 1 or > CurrentVersion)
            {
                return Result.Failure<WorkspaceSettings>(Errors.Validation($"Unsupported workspace settings version '{document.Version}'."));
            }

            return Result.Success(ToDomain(document));
        }
        catch (JsonException exception)
        {
            return Result.Failure<WorkspaceSettings>(Errors.Validation($"Workspace settings JSON is invalid: {exception.Message}"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result.Failure<WorkspaceSettings>(Errors.Unexpected($"Failed to load workspace settings: {exception.Message}"));
        }
    }

    public async Task<Result> SaveAsync(WorkspaceSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Failure(Errors.Validation("Workspace settings path is required."));
        }

        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(FromDomain(settings), Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result.Failure(Errors.Unexpected($"Failed to save workspace settings: {exception.Message}"));
        }
    }

    private static WorkspaceSettings ToDomain(WorkspaceSettingsDocument document) =>
        new(
            Normalize(document.LastOpenedDmiPath),
            Normalize(document.LastOpenedConfigPath),
            Normalize(document.LastImportedLegacyCsvPath),
            Normalize(document.LastInputDirectory),
            Normalize(document.LastOutputDirectory),
            Normalize(document.LastDraftConfigName),
            Normalize(document.LastBaseState),
            Normalize(document.LastLandmarkState),
            Normalize(document.LastOverlayState),
            ParseDirection(document.LastSelectedDirection),
            ParseOverwritePolicy(document.LastOverwritePolicy),
            Normalize(document.LastThemeMode),
            Normalize(document.LastEditorViewportMode),
            Normalize(document.LastBottomWorkspaceTab),
            document.IsPreviewInspectorExpanded,
            document.IsBottomWorkspaceExpanded ?? true,
            Normalize(document.LastUiLanguage));

    private static WorkspaceSettingsDocument FromDomain(WorkspaceSettings settings) =>
        new()
        {
            Version = CurrentVersion,
            LastOpenedDmiPath = settings.LastOpenedDmiPath,
            LastOpenedConfigPath = settings.LastOpenedConfigPath,
            LastImportedLegacyCsvPath = settings.LastImportedLegacyCsvPath,
            LastInputDirectory = settings.LastInputDirectory,
            LastOutputDirectory = settings.LastOutputDirectory,
            LastDraftConfigName = settings.LastDraftConfigName,
            LastBaseState = settings.LastBaseState,
            LastLandmarkState = settings.LastLandmarkState,
            LastOverlayState = settings.LastOverlayState,
            LastSelectedDirection = settings.LastSelectedDirection?.ToString(),
            LastOverwritePolicy = settings.LastOverwritePolicy.ToString(),
            LastThemeMode = settings.LastThemeMode,
            LastEditorViewportMode = settings.LastEditorViewportMode,
            LastBottomWorkspaceTab = settings.LastBottomWorkspaceTab,
            IsPreviewInspectorExpanded = settings.IsPreviewInspectorExpanded,
            IsBottomWorkspaceExpanded = settings.IsBottomWorkspaceExpanded,
            LastUiLanguage = settings.LastUiLanguage
        };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static OverwritePolicy ParseOverwritePolicy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OverwritePolicy.SkipExisting;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !Enum.TryParse<OverwritePolicy>(value, true, out var policy) ||
            !Enum.IsDefined(policy))
        {
            throw new JsonException($"Unsupported overwrite policy '{value}'.");
        }

        return policy;
    }

    private static SpriteDirection? ParseDirection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !Enum.TryParse<SpriteDirection>(value, true, out var direction) ||
            !Enum.IsDefined(direction))
        {
            throw new JsonException($"Unsupported sprite direction '{value}'.");
        }

        return direction;
    }

    private sealed class WorkspaceSettingsDocument
    {
        public int Version { get; set; } = CurrentVersion;

        public string? LastOpenedDmiPath { get; set; }

        public string? LastOpenedConfigPath { get; set; }

        public string? LastImportedLegacyCsvPath { get; set; }

        public string? LastInputDirectory { get; set; }

        public string? LastOutputDirectory { get; set; }

        public string? LastDraftConfigName { get; set; }

        public string? LastBaseState { get; set; }

        public string? LastLandmarkState { get; set; }

        public string? LastOverlayState { get; set; }

        public string? LastSelectedDirection { get; set; }

        public string LastOverwritePolicy { get; set; } = OverwritePolicy.SkipExisting.ToString();

        public string? LastThemeMode { get; set; }

        public string? LastEditorViewportMode { get; set; }

        public string? LastBottomWorkspaceTab { get; set; }

        public string? LastUiLanguage { get; set; }

        public bool IsPreviewInspectorExpanded { get; set; } = true;

        public bool? IsBottomWorkspaceExpanded { get; set; }
    }
}
