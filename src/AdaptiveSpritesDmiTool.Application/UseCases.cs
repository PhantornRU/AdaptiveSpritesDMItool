using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;

namespace AdaptiveSpritesDmiTool.Application;

public sealed class StartEmptyWorkspaceUseCase(IWorkspaceService workspaceService, EditorSession session)
{
    public Result<Domain.Workspaces.WorkspaceState> Execute()
    {
        session.Reset();
        return workspaceService.StartEmpty();
    }
}

public sealed class CreateConfigUseCase(EditorSession session)
{
    public Result<SpriteConfig> Execute(string name, ConfigMetadata metadata)
    {
        var result = session.CreateConfig(name, metadata);
        return result.IsFailure || session.CurrentConfig is null
            ? Result.Failure<SpriteConfig>(result.Error)
            : Result.Success(session.CurrentConfig);
    }
}

public sealed class LoadConfigUseCase(IConfigRepository repository, EditorSession session)
{
    public async Task<Result<SpriteConfig>> ExecuteAsync(string path, CancellationToken cancellationToken)
    {
        var loadResult = await repository.LoadAsync(path, cancellationToken);
        if (loadResult.IsFailure)
        {
            return loadResult;
        }

        var sessionResult = session.SetCurrentConfig(loadResult.Value, path);
        return sessionResult.IsFailure
            ? Result.Failure<SpriteConfig>(sessionResult.Error)
            : Result.Success(loadResult.Value);
    }
}

public sealed class SaveConfigUseCase(IConfigRepository repository, EditorSession session)
{
    public async Task<Result> ExecuteAsync(string path, CancellationToken cancellationToken)
    {
        if (session.CurrentConfig is null)
        {
            return Result.Failure(Errors.Conflict("There is no active config to save."));
        }

        var saveResult = await repository.SaveAsync(path, session.CurrentConfig, cancellationToken);
        if (saveResult.IsSuccess)
        {
            var sessionResult = session.SetCurrentConfigPath(path);
            if (sessionResult.IsFailure)
            {
                return sessionResult;
            }
        }

        return saveResult;
    }
}

public sealed class ImportLegacyCsvConfigUseCase(ILegacyCsvConfigImporter importer, EditorSession session)
{
    public async Task<Result<SpriteConfig>> ExecuteAsync(string path, CancellationToken cancellationToken)
    {
        var importResult = await importer.ImportAsync(path, cancellationToken);
        if (importResult.IsFailure)
        {
            return importResult;
        }

        var sessionResult = session.SetCurrentConfig(importResult.Value);
        return sessionResult.IsFailure
            ? Result.Failure<SpriteConfig>(sessionResult.Error)
            : Result.Success(importResult.Value);
    }
}

public sealed class LoadDmiFileUseCase(IDmiReader reader, EditorSession session, IWorkspaceService workspaceService)
{
    public async Task<Result<DmiAssetInfo>> ExecuteAsync(string path, CancellationToken cancellationToken)
    {
        var loadResult = await reader.LoadAsync(path, cancellationToken);
        if (loadResult.IsFailure)
        {
            return loadResult;
        }

        session.LoadAsset(loadResult.Value);
        workspaceService.Load(loadResult.Value);
        return loadResult;
    }
}

public sealed class InspectDmiFileUseCase(IDmiReader reader)
{
    public Task<Result<DmiAssetInfo>> ExecuteAsync(string path, CancellationToken cancellationToken) =>
        reader.LoadAsync(path, cancellationToken);
}

public sealed class ReadStateFrameUseCase(IStateFrameReader reader)
{
    public Task<Result<SpriteImage>> ExecuteAsync(string dmiPath, string stateName, SpriteDirection direction, CancellationToken cancellationToken) =>
        reader.ReadFrameAsync(dmiPath, stateName, direction, cancellationToken);
}

public sealed class BuildPreviewUseCase(IPreviewBuilder previewBuilder, EditorSession session)
{
    public async Task<Result<PreviewBuildResult>> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (session.LoadedAsset is null || session.CurrentConfig is null)
        {
            return Result.Failure<PreviewBuildResult>(Errors.Conflict("A sprite asset and config are required to build preview."));
        }

        var request = new PreviewBuildRequest(
            session.LoadedAsset,
            session.CurrentConfig,
            session.PreviewSelection,
            session.SelectedDirection);

        return await previewBuilder.BuildAsync(request, cancellationToken);
    }
}

public sealed class SetPreviewSelectionUseCase(EditorSession session)
{
    public Result Execute(string baseState, string? landmarkState, string? overlayState)
    {
        if (string.IsNullOrWhiteSpace(baseState))
        {
            return Result.Failure(Errors.Validation("A base state name is required."));
        }

        return session.SetPreviewSelection(
            new PreviewSelection(
                baseState.Trim(),
                NormalizeOptionalState(landmarkState),
                NormalizeOptionalState(overlayState)));
    }

    private static string? NormalizeOptionalState(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SetSelectedDirectionUseCase(EditorSession session)
{
    public Result Execute(SpriteDirection direction) => session.SetSelectedDirection(direction);
}

public sealed class UpsertPixelMappingUseCase(EditorSession session)
{
    public Result<SpriteConfig> Execute(SpriteDirection direction, PixelCoordinate source, PixelCoordinate? target) =>
        session.UpsertMapping(direction, source, target);
}

public sealed class RemovePixelMappingUseCase(EditorSession session)
{
    public Result<SpriteConfig> Execute(SpriteDirection direction, PixelCoordinate source) =>
        session.RemoveMapping(direction, source);
}

public sealed class ApplyConfigToDmiBatchUseCase(IBatchProcessingService batchProcessingService, EditorSession session)
{
    public async Task<Result<BatchJobResult>> ExecuteAsync(
        string inputDirectory,
        string outputDirectory,
        OverwritePolicy overwritePolicy,
        IProgress<BatchProgress>? progress,
        IReadOnlyList<string>? explicitFiles,
        CancellationToken cancellationToken)
    {
        if (session.CurrentConfig is null)
        {
            return Result.Failure<BatchJobResult>(Errors.Conflict("There is no active config to apply."));
        }

        var request = new BatchJobRequest(inputDirectory, outputDirectory, session.CurrentConfig, overwritePolicy, explicitFiles);
        return await batchProcessingService.RunAsync(request, progress, cancellationToken);
    }
}

public sealed class ApplyConfigTransformUseCase(EditorSession session)
{
    public Result<SpriteConfig> Execute(Func<SpriteConfig, SpriteConfig> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return session.ApplyTransform(transform);
    }
}

public sealed class UndoChangeUseCase(EditorSession session)
{
    public Result<SpriteConfig> Execute() => session.Undo();
}

public sealed class RedoChangeUseCase(EditorSession session)
{
    public Result<SpriteConfig> Execute() => session.Redo();
}

public sealed class LoadWorkspaceSettingsUseCase(ISettingsRepository repository)
{
    private static readonly WorkspaceSettings Empty = WorkspaceSettings.Empty;

    public async Task<Result<WorkspaceSettings>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var result = await repository.LoadAsync(cancellationToken);
        return result.IsSuccess
            ? result
            : result.Error.Code == "not-found"
                ? Result.Success(Empty)
                : result;
    }
}

public sealed class SaveWorkspaceSettingsUseCase(ISettingsRepository repository)
{
    public Task<Result> ExecuteAsync(WorkspaceSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return repository.SaveAsync(settings, cancellationToken);
    }
}
