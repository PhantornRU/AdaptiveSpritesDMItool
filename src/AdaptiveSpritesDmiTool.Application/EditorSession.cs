using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Workspaces;

namespace AdaptiveSpritesDmiTool.Application;

public sealed class EditorSession
{
    private readonly Stack<SpriteConfig> _undoStack = new();
    private readonly Stack<SpriteConfig> _redoStack = new();

    public WorkspaceState Workspace { get; private set; } = WorkspaceState.Empty;

    public DmiAssetInfo? LoadedAsset { get; private set; }

    public SpriteConfig? CurrentConfig { get; private set; }

    public string? CurrentConfigPath { get; private set; }

    public PreviewSelection PreviewSelection { get; private set; } = new(string.Empty, null, null);

    public SpriteDirection SelectedDirection { get; private set; } = SpriteDirection.South;

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public void Reset()
    {
        Workspace = WorkspaceState.Empty;
        LoadedAsset = null;
        CurrentConfig = null;
        CurrentConfigPath = null;
        PreviewSelection = new PreviewSelection(string.Empty, null, null);
        SelectedDirection = SpriteDirection.South;
        _undoStack.Clear();
        _redoStack.Clear();
    }

    public Result LoadAsset(DmiAssetInfo asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        LoadedAsset = asset;
        Workspace = asset.SourcePath is { Length: > 0 } sourcePath
            ? WorkspaceState.Loaded(asset.DisplayName, asset.Resolution, asset.SupportedDirections, sourcePath)
            : WorkspaceState.ForSprite(asset.DisplayName, asset.Resolution, asset.SupportedDirections);

        if (!asset.SupportedDirections.Supports(SelectedDirection))
        {
            SelectedDirection = asset.SupportedDirections.GetDirections().First();
        }

        return Result.Success();
    }

    public Result CreateConfig(string name, ConfigMetadata metadata)
    {
        if (LoadedAsset is null)
        {
            return Result.Failure(Errors.Conflict("A DMI asset must be loaded before creating a config."));
        }

        CurrentConfig = SpriteConfig.CreateEmpty(name, LoadedAsset.Resolution, LoadedAsset.SupportedDirections, metadata);
        CurrentConfigPath = null;
        _undoStack.Clear();
        _redoStack.Clear();
        return Result.Success();
    }

    public Result SetCurrentConfig(SpriteConfig config, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (LoadedAsset is not null)
        {
            var compatibility = config.ValidateCompatibility(LoadedAsset.Resolution, LoadedAsset.SupportedDirections);
            if (!compatibility.IsValid)
            {
                return Result.Failure(Errors.Validation(compatibility.Errors[0].Message));
            }
        }

        CurrentConfig = config;
        CurrentConfigPath = path;
        _undoStack.Clear();
        _redoStack.Clear();
        return Result.Success();
    }

    public Result SetPreviewSelection(PreviewSelection selection)
    {
        PreviewSelection = selection ?? throw new ArgumentNullException(nameof(selection));
        return Result.Success();
    }

    public Result SetSelectedDirection(SpriteDirection direction)
    {
        if (LoadedAsset is not null && !LoadedAsset.SupportedDirections.Supports(direction))
        {
            return Result.Failure(Errors.Validation($"Direction '{direction}' is not supported by the loaded asset."));
        }

        SelectedDirection = direction;
        return Result.Success();
    }

    public Result<SpriteConfig> UpsertMapping(SpriteDirection direction, PixelCoordinate source, PixelCoordinate? target)
    {
        if (CurrentConfig is null)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict("There is no active config to edit."));
        }

        _undoStack.Push(CurrentConfig.Clone());
        _redoStack.Clear();
        CurrentConfig = CurrentConfig.SetMapping(direction, source, target);
        return Result.Success(CurrentConfig);
    }

    public Result<SpriteConfig> RemoveMapping(SpriteDirection direction, PixelCoordinate source)
    {
        if (CurrentConfig is null)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict("There is no active config to edit."));
        }

        _undoStack.Push(CurrentConfig.Clone());
        _redoStack.Clear();
        CurrentConfig = CurrentConfig.RemoveMapping(direction, source);
        return Result.Success(CurrentConfig);
    }

    public Result<SpriteConfig> ApplyTransform(Func<SpriteConfig, SpriteConfig> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        if (CurrentConfig is null)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict("There is no active config to edit."));
        }

        var snapshot = CurrentConfig.Clone();
        var updatedConfig = transform(CurrentConfig.Clone());
        ArgumentNullException.ThrowIfNull(updatedConfig);

        _undoStack.Push(snapshot);
        _redoStack.Clear();
        CurrentConfig = updatedConfig;
        return Result.Success(CurrentConfig);
    }

    public Result<SpriteConfig> Undo()
    {
        if (CurrentConfig is null || _undoStack.Count == 0)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict("There is no change to undo."));
        }

        _redoStack.Push(CurrentConfig.Clone());
        CurrentConfig = _undoStack.Pop();
        return Result.Success(CurrentConfig);
    }

    public Result<SpriteConfig> Redo()
    {
        if (CurrentConfig is null || _redoStack.Count == 0)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict("There is no change to redo."));
        }

        _undoStack.Push(CurrentConfig.Clone());
        CurrentConfig = _redoStack.Pop();
        return Result.Success(CurrentConfig);
    }
}

public sealed class EditorWorkspaceService : IWorkspaceService
{
    private WorkspaceState _current = WorkspaceState.Empty;

    public WorkspaceState Current => _current;

    public Result<WorkspaceState> StartEmpty()
    {
        _current = WorkspaceState.Empty;
        return Result.Success(_current);
    }

    public Result<WorkspaceState> Load(DmiAssetInfo asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        _current = asset.SourcePath is { Length: > 0 } sourcePath
            ? WorkspaceState.Loaded(asset.DisplayName, asset.Resolution, asset.SupportedDirections, sourcePath)
            : WorkspaceState.ForSprite(asset.DisplayName, asset.Resolution, asset.SupportedDirections);

        return Result.Success(_current);
    }
}
