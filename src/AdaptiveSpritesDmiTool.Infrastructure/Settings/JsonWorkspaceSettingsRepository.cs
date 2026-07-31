using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using Newtonsoft.Json;
using System.Globalization;

namespace AdaptiveSpritesDmiTool.Infrastructure.Settings;

public sealed class JsonWorkspaceSettingsRepository(string filePath) : ISettingsRepository
{
    private const int CurrentVersion = 8;

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

    private static WorkspaceSettings ToDomain(WorkspaceSettingsDocument document)
    {
        var importedStates = ParseImportedStates(document.ImportedStates);
        return new WorkspaceSettings(
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
            Normalize(document.LastUiLanguage),
            document.HideInactiveSourceCanvases ?? true,
            document.FitMultipleDirectionCanvasesToViewport ?? true,
            importedStates,
            document.MirrorAxisOffsetPixels ?? 0,
            document.ShowMirrorAxisGuide ?? false,
            document.MirrorAcrossDirections ?? true,
            Normalize(document.LastOpenedDocumentPath),
            ParseAuxiliaryLayers(document.AuxiliaryLayers, importedStates),
            ParseBatchOutputFormats(document.SelectedBatchOutputFormats),
            ParseRasterExportSettings(document.RasterExportSettings));
    }

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
            LastUiLanguage = settings.LastUiLanguage,
            HideInactiveSourceCanvases = settings.HideInactiveSourceCanvases,
            FitMultipleDirectionCanvasesToViewport = settings.FitMultipleDirectionCanvasesToViewport,
            MirrorAxisOffsetPixels = settings.MirrorAxisOffsetPixels,
            ShowMirrorAxisGuide = settings.ShowMirrorAxisGuide,
            MirrorAcrossDirections = settings.MirrorAcrossDirections,
            LastOpenedDocumentPath = settings.LastOpenedDocumentPath,
            SelectedBatchOutputFormats = (settings.SelectedBatchOutputFormats ??
                    [WorkspaceBatchOutputFormat.Dmi, WorkspaceBatchOutputFormat.Png])
                .Select(static format => format.ToString())
                .ToList(),
            RasterExportSettings = ToRasterExportDocument(settings.RasterExportSettings),
            ImportedStates = (settings.ImportedStates ?? Array.Empty<WorkspaceImportedStateSettings>())
                .Select(static item => new ImportedStateDocument
                {
                    StateName = item.StateName,
                    SourcePath = item.SourcePath,
                    SourceFileLabel = item.SourceFileLabel,
                    IsSourceAssigned = item.IsSourceAssigned,
                    IsEditableAssigned = item.IsEditableAssigned,
                    PlacementMode = item.PlacementMode,
                    Order = item.Order,
                    OpacityPercent = item.OpacityPercent
                })
                .ToList(),
            AuxiliaryLayers = (settings.AuxiliaryLayers ?? Array.Empty<WorkspaceAuxiliaryLayerSettings>())
                .Select(static item => new AuxiliaryLayerDocument
                {
                    SourceId = item.SourceId,
                    StateName = item.StateName,
                    SourcePath = item.SourcePath,
                    SourceFileLabel = item.SourceFileLabel,
                    Format = item.Format.ToString(),
                    FrameIndex = item.FrameIndex,
                    IsSourceAssigned = item.IsSourceAssigned,
                    IsEditableAssigned = item.IsEditableAssigned,
                    PlacementMode = item.PlacementMode,
                    Order = item.Order,
                    OpacityPercent = item.OpacityPercent
                })
                .ToList()
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

    private static IReadOnlyList<WorkspaceImportedStateSettings> ParseImportedStates(IEnumerable<ImportedStateDocument>? documents)
    {
        if (documents is null)
        {
            return Array.Empty<WorkspaceImportedStateSettings>();
        }

        var importedStates = new List<WorkspaceImportedStateSettings>();
        foreach (var document in documents)
        {
            var stateName = Normalize(document.StateName);
            var sourcePath = Normalize(document.SourcePath);
            if (stateName is null || sourcePath is null)
            {
                throw new JsonException("Imported state entries must include stateName and sourcePath.");
            }

            var placementMode = ParseImportedStatePlacementMode(document.PlacementMode);
            var opacityPercent = ParseImportedStateOpacity(document.OpacityPercent);
            importedStates.Add(
                new WorkspaceImportedStateSettings(
                    stateName,
                    sourcePath,
                    Normalize(document.SourceFileLabel),
                    document.IsSourceAssigned,
                    document.IsEditableAssigned,
                    placementMode,
                    Math.Max(0, document.Order),
                    opacityPercent));
        }

        return importedStates;
    }

    private static IReadOnlyList<WorkspaceAuxiliaryLayerSettings> ParseAuxiliaryLayers(
        IEnumerable<AuxiliaryLayerDocument>? documents,
        IReadOnlyList<WorkspaceImportedStateSettings> importedStates)
    {
        if (documents is null)
        {
            return importedStates
                .Select(static item => new WorkspaceAuxiliaryLayerSettings(
                    SourceId: null,
                    item.StateName,
                    item.SourcePath,
                    item.SourceFileLabel,
                    SpriteSourceFormat.Dmi,
                    FrameIndex: 0,
                    item.IsSourceAssigned,
                    item.IsEditableAssigned,
                    item.PlacementMode,
                    item.Order,
                    item.OpacityPercent))
                .ToArray();
        }

        var layers = new List<WorkspaceAuxiliaryLayerSettings>();
        foreach (var document in documents)
        {
            var stateName = Normalize(document.StateName);
            var sourcePath = Normalize(document.SourcePath);
            if (stateName is null || sourcePath is null)
            {
                throw new JsonException("Auxiliary layer entries must include stateName and sourcePath.");
            }

            if (document.FrameIndex < 0)
            {
                throw new JsonException($"Auxiliary layer frame index must be non-negative, but was '{document.FrameIndex}'.");
            }

            layers.Add(new WorkspaceAuxiliaryLayerSettings(
                document.SourceId,
                stateName,
                sourcePath,
                Normalize(document.SourceFileLabel),
                ParseNamedEnum<SpriteSourceFormat>(document.Format, "auxiliary layer format"),
                document.FrameIndex,
                document.IsSourceAssigned,
                document.IsEditableAssigned,
                ParseImportedStatePlacementMode(document.PlacementMode),
                Math.Max(0, document.Order),
                ParseImportedStateOpacity(document.OpacityPercent)));
        }

        return layers;
    }

    private static WorkspaceBatchOutputFormat[] ParseBatchOutputFormats(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [WorkspaceBatchOutputFormat.Dmi, WorkspaceBatchOutputFormat.Png];
        }

        var formats = values
            .Select(value => ParseNamedEnum<WorkspaceBatchOutputFormat>(value, "batch output format"))
            .Distinct()
            .ToArray();
        if (formats.Length == 0)
        {
            throw new JsonException("At least one batch output format must be selected.");
        }

        return formats;
    }

    private static WorkspaceRasterExportSettings ParseRasterExportSettings(RasterExportSettingsDocument? document)
    {
        if (document is null)
        {
            return WorkspaceRasterExportSettings.Default;
        }

        var depth = ParseNamedEnum<SpriteDirectionDepth>(document.DirectionDepth, "raster direction depth");
        var layout = ParseNamedEnum<SpriteDocumentExportFormat>(document.Layout, "raster export layout");
        if (layout is not SpriteDocumentExportFormat.PngSheet and not SpriteDocumentExportFormat.PngSequence)
        {
            throw new JsonException($"Unsupported raster export layout '{document.Layout}'.");
        }

        return new WorkspaceRasterExportSettings(depth, layout);
    }

    private static RasterExportSettingsDocument ToRasterExportDocument(WorkspaceRasterExportSettings? settings)
    {
        var value = settings ?? WorkspaceRasterExportSettings.Default;
        return new RasterExportSettingsDocument
        {
            DirectionDepth = value.DirectionDepth.ToString(),
            Layout = value.Layout.ToString()
        };
    }

    private static TEnum ParseNamedEnum<TEnum>(string? value, string description)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value) ||
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !Enum.TryParse<TEnum>(value, true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new JsonException($"Unsupported {description} '{value}'.");
        }

        return parsed;
    }

    private static int ParseImportedStateOpacity(int? value)
    {
        if (value is null)
        {
            return 100;
        }

        if (value is < 0 or > 100)
        {
            throw new JsonException($"Imported state opacity must be between 0 and 100, but was '{value}'.");
        }

        return value.Value;
    }

    private static string ParseImportedStatePlacementMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Overlay";
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !Enum.TryParse<ImportedStatePlacementSetting>(value, true, out var placementMode) ||
            !Enum.IsDefined(placementMode))
        {
            throw new JsonException($"Unsupported imported state placement mode '{value}'.");
        }

        return placementMode.ToString();
    }

    private enum ImportedStatePlacementSetting
    {
        Background = 0,
        Overlay = 1
    }

    private sealed class WorkspaceSettingsDocument
    {
        public int Version { get; set; } = CurrentVersion;

        public string? LastOpenedDmiPath { get; set; }

        public string? LastOpenedDocumentPath { get; set; }

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

        public bool? HideInactiveSourceCanvases { get; set; }

        public bool? FitMultipleDirectionCanvasesToViewport { get; set; }

        public int? MirrorAxisOffsetPixels { get; set; }

        public bool? ShowMirrorAxisGuide { get; set; }

        public bool? MirrorAcrossDirections { get; set; }

        public List<ImportedStateDocument>? ImportedStates { get; set; }

        public List<AuxiliaryLayerDocument>? AuxiliaryLayers { get; set; }

        public List<string>? SelectedBatchOutputFormats { get; set; }

        public RasterExportSettingsDocument? RasterExportSettings { get; set; }
    }

    private sealed class ImportedStateDocument
    {
        public string? StateName { get; set; }

        public string? SourcePath { get; set; }

        public string? SourceFileLabel { get; set; }

        public bool IsSourceAssigned { get; set; }

        public bool IsEditableAssigned { get; set; }

        public string? PlacementMode { get; set; } = ImportedStatePlacementSetting.Overlay.ToString();

        public int Order { get; set; }

        public int? OpacityPercent { get; set; }
    }

    private sealed class AuxiliaryLayerDocument
    {
        public Guid? SourceId { get; set; }

        public string? StateName { get; set; }

        public string? SourcePath { get; set; }

        public string? SourceFileLabel { get; set; }

        public string? Format { get; set; }

        public int FrameIndex { get; set; }

        public bool IsSourceAssigned { get; set; }

        public bool IsEditableAssigned { get; set; }

        public string? PlacementMode { get; set; } = ImportedStatePlacementSetting.Overlay.ToString();

        public int Order { get; set; }

        public int? OpacityPercent { get; set; }
    }

    private sealed class RasterExportSettingsDocument
    {
        public string? DirectionDepth { get; set; } = SpriteDirectionDepth.Four.ToString();

        public string? Layout { get; set; } = SpriteDocumentExportFormat.PngSheet.ToString();
    }
}
