using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Workspaces;

namespace AdaptiveSpritesDmiTool.Application;

public sealed record DmiStateInfo(string Name, int FrameCount);

public sealed record DmiAssetInfo(
    string DisplayName,
    string? SourcePath,
    SpriteResolution Resolution,
    SupportedDirectionSet SupportedDirections,
    IReadOnlyList<DmiStateInfo> States);

public sealed record SpriteImage(int Width, int Height, byte[] RgbaBytes);

public sealed record PreviewSelection(string BaseState, string? LandmarkState, string? OverlayState);

public sealed record PreviewBuildRequest(
    DmiAssetInfo Asset,
    SpriteConfig Config,
    PreviewSelection Selection,
    SpriteDirection Direction);

public sealed record PreviewBuildResult(
    SpriteImage? BaseImage,
    SpriteImage? LandmarkImage,
    SpriteImage? OverlayImage,
    SpriteImage? CompositeImage);

public enum OverwritePolicy
{
    SkipExisting = 0,
    OverwriteExisting = 1,
    FailIfExists = 2
}

public sealed record ApplyConfigToFileRequest(
    string InputPath,
    string OutputPath,
    SpriteConfig Config,
    OverwritePolicy OverwritePolicy);

public enum BatchFileStatus
{
    Skipped = 0,
    Processed = 1,
    Failed = 2,
    Cancelled = 3
}

public sealed record BatchFileResult(
    string InputPath,
    string? OutputPath,
    BatchFileStatus Status,
    string Message);

public sealed record BatchProgress(int ProcessedFiles, int TotalFiles, string? CurrentFile);

public sealed record BatchJobRequest(
    string InputDirectory,
    string OutputDirectory,
    SpriteConfig Config,
    OverwritePolicy OverwritePolicy,
    IReadOnlyList<string>? ExplicitFiles = null);

public sealed record BatchJobResult(IReadOnlyList<BatchFileResult> Files)
{
    public bool WasCancelled => Files.Any(static file => file.Status == BatchFileStatus.Cancelled);
}

public sealed record WorkspaceSettings(
    string? LastOpenedDmiPath,
    string? LastOpenedConfigPath,
    string? LastImportedLegacyCsvPath,
    string? LastInputDirectory,
    string? LastOutputDirectory,
    string? LastDraftConfigName,
    string? LastBaseState,
    string? LastLandmarkState,
    string? LastOverlayState,
    SpriteDirection? LastSelectedDirection,
    OverwritePolicy LastOverwritePolicy,
    string? LastThemeMode,
    string? LastEditorViewportMode,
    string? LastBottomWorkspaceTab,
    bool IsPreviewInspectorExpanded)
{
    public static WorkspaceSettings Empty { get; } = new(
        LastOpenedDmiPath: null,
        LastOpenedConfigPath: null,
        LastImportedLegacyCsvPath: null,
        LastInputDirectory: null,
        LastOutputDirectory: null,
        LastDraftConfigName: null,
        LastBaseState: null,
        LastLandmarkState: null,
        LastOverlayState: null,
        LastSelectedDirection: null,
        LastOverwritePolicy: OverwritePolicy.OverwriteExisting,
        LastThemeMode: null,
        LastEditorViewportMode: null,
        LastBottomWorkspaceTab: null,
        IsPreviewInspectorExpanded: true);
}

public interface IConfigRepository
{
    Task<Result<SpriteConfig>> LoadAsync(string path, CancellationToken cancellationToken);

    Task<Result> SaveAsync(string path, SpriteConfig config, CancellationToken cancellationToken);
}

public interface ILegacyCsvConfigImporter
{
    Task<Result<SpriteConfig>> ImportAsync(string path, CancellationToken cancellationToken);
}

public interface IDmiReader
{
    Task<Result<DmiAssetInfo>> LoadAsync(string path, CancellationToken cancellationToken);
}

public interface IStateFrameReader
{
    Task<Result<SpriteImage>> ReadFrameAsync(string dmiPath, string stateName, SpriteDirection direction, CancellationToken cancellationToken);
}

public interface IDmiWriter
{
    Task<Result<BatchFileResult>> ApplyAsync(ApplyConfigToFileRequest request, CancellationToken cancellationToken);
}

public interface IPreviewBuilder
{
    Task<Result<PreviewBuildResult>> BuildAsync(PreviewBuildRequest request, CancellationToken cancellationToken);
}

public interface IBatchProcessingService
{
    Task<Result<BatchJobResult>> RunAsync(
        BatchJobRequest request,
        IProgress<BatchProgress>? progress,
        CancellationToken cancellationToken);
}

public interface ISettingsRepository
{
    Task<Result<WorkspaceSettings>> LoadAsync(CancellationToken cancellationToken);

    Task<Result> SaveAsync(WorkspaceSettings settings, CancellationToken cancellationToken);
}

public interface IWorkspaceService
{
    WorkspaceState Current { get; }

    Result<WorkspaceState> StartEmpty();

    Result<WorkspaceState> Load(DmiAssetInfo asset);
}
