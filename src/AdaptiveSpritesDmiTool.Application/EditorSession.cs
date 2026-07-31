using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Workspaces;

namespace AdaptiveSpritesDmiTool.Application;

public sealed class EditorConfigSessionSnapshot
{
    internal EditorConfigSessionSnapshot(
        SpriteConfig config,
        string? configPath,
        SpriteConfig? savedConfigBaseline)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        ConfigPath = string.IsNullOrWhiteSpace(configPath) ? null : configPath;
        SavedConfigBaseline = savedConfigBaseline;
    }

    internal SpriteConfig Config { get; }

    public string? ConfigPath { get; }

    internal SpriteConfig? SavedConfigBaseline { get; }
}

public sealed class EditorSession
{
    private readonly Stack<SpriteConfig> _undoStack = new();
    private readonly Stack<SpriteConfig> _redoStack = new();
    private SpriteConfig? _savedConfig;

    public WorkspaceState Workspace { get; private set; } = WorkspaceState.Empty;

    public DmiAssetInfo? LoadedAsset { get; private set; }

    public SpriteConfig? CurrentConfig { get; private set; }

    public string? CurrentConfigPath { get; private set; }

    public bool IsDirty { get; private set; }

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
        _savedConfig = null;
        IsDirty = false;
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

    public Result CreateConfig(
        string name,
        ConfigMetadata metadata,
        SpriteEditorSettings? editorSettings = null)
    {
        if (LoadedAsset is null)
        {
            return Result.Failure(Errors.Conflict("A DMI asset must be loaded before creating a config."));
        }

        CurrentConfig = SpriteConfig.CreateEmpty(
            name,
            LoadedAsset.Resolution,
            LoadedAsset.SupportedDirections,
            metadata,
            editorSettings);
        CurrentConfigPath = null;
        _savedConfig = null;
        IsDirty = true;
        _undoStack.Clear();
        _redoStack.Clear();
        return Result.Success();
    }

    public Result SetCurrentConfig(SpriteConfig config, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        var compatibility = ValidateConfigCompatibility(config);
        if (compatibility.IsFailure)
        {
            return compatibility;
        }

        CurrentConfig = config;
        CurrentConfigPath = path;
        _savedConfig = string.IsNullOrWhiteSpace(path) ? null : config.Clone();
        IsDirty = _savedConfig is null;
        _undoStack.Clear();
        _redoStack.Clear();
        return Result.Success();
    }

    public Result<EditorConfigSessionSnapshot> CaptureCurrentConfigSnapshot()
    {
        if (CurrentConfig is null)
        {
            return Result.Failure<EditorConfigSessionSnapshot>(
                Errors.Conflict("There is no active config to capture."));
        }

        return Result.Success(
            new EditorConfigSessionSnapshot(
                CurrentConfig.Clone(),
                CurrentConfigPath,
                _savedConfig?.Clone()));
    }

    public Result RestoreCurrentConfigSnapshot(EditorConfigSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var compatibility = ValidateConfigCompatibility(snapshot.Config);
        if (compatibility.IsFailure)
        {
            return compatibility;
        }

        CurrentConfig = snapshot.Config.Clone();
        CurrentConfigPath = snapshot.ConfigPath;
        _savedConfig = snapshot.SavedConfigBaseline?.Clone();
        UpdateDirtyState();
        _undoStack.Clear();
        _redoStack.Clear();
        return Result.Success();
    }

    public Result SetCurrentConfigPath(string? path)
    {
        if (CurrentConfig is null)
        {
            return Result.Failure(Errors.Conflict("There is no active config to update."));
        }

        CurrentConfigPath = path;
        _savedConfig = CurrentConfig.Clone();
        IsDirty = false;
        return Result.Success();
    }

    public Result<SpriteConfig> RenameCurrentConfig(string name)
    {
        if (CurrentConfig is null)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict("There is no active config to rename."));
        }

        CurrentConfig = CurrentConfig.WithName(name);
        UpdateDirtyState();
        return Result.Success(CurrentConfig);
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

        if (CurrentConfig is not null && !CurrentConfig.SupportedDirections.Supports(direction))
        {
            return Result.Failure(Errors.Validation($"Direction '{direction}' is not supported by the active config."));
        }

        SelectedDirection = direction;
        return Result.Success();
    }

    public Result<SpriteConfig> UpsertMapping(SpriteDirection direction, PixelCoordinate source, PixelCoordinate? target)
    {
        var mutation = new EditorMappingMutation(
            target is null ? EditorMappingMutationKind.SetTransparent : EditorMappingMutationKind.SetSource,
            direction,
            source,
            target);
        var result = ApplyMutation(new EditorMutationPlan([mutation]));
        return result.IsFailure
            ? Result.Failure<SpriteConfig>(result.Error)
            : Result.Success(result.Value.Config);
    }

    public Result<SpriteConfig> RemoveMapping(SpriteDirection direction, PixelCoordinate source)
    {
        var result = ApplyMutation(
            new EditorMutationPlan(
                [new EditorMappingMutation(EditorMappingMutationKind.Restore, direction, source)]));
        return result.IsFailure
            ? Result.Failure<SpriteConfig>(result.Error)
            : Result.Success(result.Value.Config);
    }

    public Result<EditorMutationApplyResult> ApplyMutation(EditorMutationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (CurrentConfig is null)
        {
            return Result.Failure<EditorMutationApplyResult>(Errors.Conflict("There is no active config to edit."));
        }

        var snapshot = CurrentConfig.Clone();
        var applyResult = EditorMutationEngine.Apply(snapshot, plan);
        if (applyResult.IsFailure)
        {
            return applyResult;
        }

        if (!applyResult.Value.IsChanged)
        {
            return Result.Success(
                applyResult.Value with
                {
                    Config = CurrentConfig,
                    AppliedOperationCount = 0
                });
        }

        _undoStack.Push(snapshot);
        _redoStack.Clear();
        CurrentConfig = applyResult.Value.Config;
        UpdateDirtyState();
        return Result.Success(applyResult.Value);
    }

    public Result<SpriteConfig> ApplyTransform(Func<SpriteConfig, SpriteConfig> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        if (CurrentConfig is null)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict("There is no active config to edit."));
        }

        var snapshot = CurrentConfig.Clone();
        try
        {
            var updatedConfig = transform(CurrentConfig.Clone());
            if (updatedConfig is null)
            {
                return Result.Failure<SpriteConfig>(Errors.Validation("Config transform returned no config."));
            }

            var validation = updatedConfig.Validate();
            if (!validation.IsValid)
            {
                return Result.Failure<SpriteConfig>(Errors.Validation(validation.Errors[0].Message));
            }

            if (snapshot.HasSameMappingContent(updatedConfig))
            {
                return Result.Success(CurrentConfig);
            }

            _undoStack.Push(snapshot);
            _redoStack.Clear();
            CurrentConfig = updatedConfig;
            UpdateDirtyState();
            return Result.Success(CurrentConfig);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<SpriteConfig>(Errors.Validation(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict(exception.Message));
        }
    }

    public Result<SpriteConfig> Undo()
    {
        if (CurrentConfig is null || _undoStack.Count == 0)
        {
            return Result.Failure<SpriteConfig>(Errors.Conflict("There is no change to undo."));
        }

        _redoStack.Push(CurrentConfig.Clone());
        CurrentConfig = _undoStack.Pop();
        UpdateDirtyState();
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
        UpdateDirtyState();
        return Result.Success(CurrentConfig);
    }

    private void UpdateDirtyState() =>
        IsDirty = CurrentConfig is not null &&
            (_savedConfig is null || !_savedConfig.HasSameMappingContent(CurrentConfig));

    private Result ValidateConfigCompatibility(SpriteConfig config)
    {
        if (LoadedAsset is null)
        {
            return Result.Success();
        }

        var compatibility = config.ValidateCompatibility(LoadedAsset.Resolution, LoadedAsset.SupportedDirections);
        return compatibility.IsValid
            ? Result.Success()
            : Result.Failure(Errors.Validation(compatibility.Errors[0].Message));
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
