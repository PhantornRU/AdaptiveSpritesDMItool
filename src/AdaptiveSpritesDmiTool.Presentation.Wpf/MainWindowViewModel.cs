using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly StartEmptyWorkspaceUseCase _startEmptyWorkspaceUseCase;
    private readonly CreateConfigUseCase _createConfigUseCase;
    private readonly SaveConfigUseCase _saveConfigUseCase;
    private readonly LoadConfigUseCase _loadConfigUseCase;
    private readonly ImportLegacyCsvConfigUseCase _importLegacyCsvConfigUseCase;
    private readonly LoadDmiFileUseCase _loadDmiFileUseCase;
    private readonly BuildPreviewUseCase _buildPreviewUseCase;
    private readonly SpriteImageBitmapSourceFactory _bitmapSourceFactory;
    private readonly EditorSession _editorSession;
    private CancellationTokenSource? _activeOperationCts;

    public MainWindowViewModel(
        StartEmptyWorkspaceUseCase startEmptyWorkspaceUseCase,
        CreateConfigUseCase createConfigUseCase,
        SaveConfigUseCase saveConfigUseCase,
        LoadConfigUseCase loadConfigUseCase,
        ImportLegacyCsvConfigUseCase importLegacyCsvConfigUseCase,
        LoadDmiFileUseCase loadDmiFileUseCase,
        BuildPreviewUseCase buildPreviewUseCase,
        SpriteImageBitmapSourceFactory bitmapSourceFactory,
        EditorSession editorSession)
    {
        _startEmptyWorkspaceUseCase = startEmptyWorkspaceUseCase;
        _createConfigUseCase = createConfigUseCase;
        _saveConfigUseCase = saveConfigUseCase;
        _loadConfigUseCase = loadConfigUseCase;
        _importLegacyCsvConfigUseCase = importLegacyCsvConfigUseCase;
        _loadDmiFileUseCase = loadDmiFileUseCase;
        _buildPreviewUseCase = buildPreviewUseCase;
        _bitmapSourceFactory = bitmapSourceFactory;
        _editorSession = editorSession;

        DirectionBadges = [];
        IntegrationBlockers =
        [
            "The shell still relies on manual path entry because dialog contracts are not introduced yet.",
            "Batch execution is not wired into the shell yet.",
            "Workspace settings persistence is still outside the startup flow."
        ];
        NextActions =
        [
            "Load a DMI from a manual path and create a config against the loaded sprite contract.",
            "Load or save a JSON config after a DMI-backed editor session is available.",
            "Build preview safely even when landmark or overlay are missing.",
            "Wire batch execution and overwrite policy into the shell."
        ];
    }

    public string WindowTitle => WorkspaceTitle == "Empty workspace"
        ? "Adaptive Sprites DMI Tool"
        : $"Adaptive Sprites DMI Tool - {WorkspaceTitle}";

    public ObservableCollection<string> DirectionBadges { get; }

    public ObservableCollection<string> IntegrationBlockers { get; }

    public ObservableCollection<string> NextActions { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string workspaceTitle = "Empty workspace";

    [ObservableProperty]
    private string spriteContractSummary = "No sprite loaded yet. Resolution, direction set, and states will appear after DMI load.";

    [ObservableProperty]
    private string configSummary = "No config loaded";

    [ObservableProperty]
    private string currentDirectionText = SpriteDirection.South.ToString();

    [ObservableProperty]
    private string manualFlowSummary =
        "The shell executes open, save, load, import, and preview flows through Application use cases without file logic in code-behind.";

    [ObservableProperty]
    private string pendingDmiPath = string.Empty;

    [ObservableProperty]
    private string draftConfigName = "New Config";

    [ObservableProperty]
    private string pendingConfigPath = string.Empty;

    [ObservableProperty]
    private string pendingSaveConfigPath = string.Empty;

    [ObservableProperty]
    private string pendingLegacyCsvPath = string.Empty;

    [ObservableProperty]
    private string baseStateName = string.Empty;

    [ObservableProperty]
    private string landmarkStateName = string.Empty;

    [ObservableProperty]
    private string overlayStateName = string.Empty;

    [ObservableProperty]
    private string previewSelectionSummary = "Preview selection is not staged yet.";

    [ObservableProperty]
    private string basePreviewText = "No base state selected yet.";

    [ObservableProperty]
    private string landmarkPreviewText = "No landmark selected. Optional input is safely ignored.";

    [ObservableProperty]
    private string overlayPreviewText = "No overlay selected. Optional input is safely ignored.";

    [ObservableProperty]
    private string compositePreviewText = "Composite preview is not built yet.";

    [ObservableProperty]
    private BitmapSource? basePreviewImage;

    [ObservableProperty]
    private BitmapSource? landmarkPreviewImage;

    [ObservableProperty]
    private BitmapSource? overlayPreviewImage;

    [ObservableProperty]
    private BitmapSource? compositePreviewImage;

    [ObservableProperty]
    private string workspaceNotes =
        "The shell starts with an empty workspace and no demo assets. Presentation only stages UI state; execution stays behind Application contracts.";

    [ObservableProperty]
    private string statusMessage = "Ready. Empty workspace created. No demo assets were loaded.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool isBusy;

    public void Initialize()
    {
        var result = _startEmptyWorkspaceUseCase.Execute();
        StatusMessage = result.IsSuccess
            ? "Ready. Empty workspace created. No demo assets were loaded."
            : $"Startup failed: {result.Error.Message}";

        ResetStagingInputs();
        RefreshShellState();
        RefreshPreviewSelection();
        ClearPreviewImages();
    }

    [RelayCommand]
    private async Task OpenDmiAsync()
    {
        if (string.IsNullOrWhiteSpace(PendingDmiPath))
        {
            StatusMessage = "Enter a DMI path to load a sprite asset.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var result = await _loadDmiFileUseCase.ExecuteAsync(PendingDmiPath.Trim(), cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                BaseStateName = result.Value.States.Count > 0
                    ? result.Value.States[0].Name
                    : string.Empty;
                LandmarkStateName = string.Empty;
                OverlayStateName = string.Empty;
                _editorSession.SetPreviewSelection(new PreviewSelection(BaseStateName, null, null));
                ManualFlowSummary = $"Loaded DMI '{result.Value.DisplayName}' from a user-supplied path. No demo assets were required.";
                StatusMessage = $"Loaded DMI '{result.Value.DisplayName}' with {result.Value.States.Count} states.";
                RefreshShellState();
                RefreshPreviewSelection();
                ClearPreviewImages();
            });
    }

    [RelayCommand]
    private void CreateConfig()
    {
        var configName = DraftConfigName?.Trim();
        if (string.IsNullOrWhiteSpace(configName))
        {
            StatusMessage = "Config name is required.";
            return;
        }

        var metadata = ConfigMetadata.CreateNew(ConfigSource.UserCreated, sourceIdentifier: "presentation-shell");
        var result = _createConfigUseCase.Execute(configName, metadata);
        StatusMessage = result.IsSuccess
            ? "Draft JSON config created in the editor session."
            : result.Error.Message;

        RefreshShellState();
        ClearPreviewImages();
    }

    [RelayCommand]
    private async Task SaveConfigAsync()
    {
        var requestedPath = string.IsNullOrWhiteSpace(PendingSaveConfigPath)
            ? PendingConfigPath
            : PendingSaveConfigPath;

        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            StatusMessage = "Enter a JSON config path to save.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var normalizedPath = requestedPath.Trim();
                var result = await _saveConfigUseCase.ExecuteAsync(normalizedPath, cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                PendingConfigPath = normalizedPath;
                PendingSaveConfigPath = normalizedPath;
                ManualFlowSummary = $"Saved the active config to '{Path.GetFileName(normalizedPath)}' through the versioned JSON repository.";
                StatusMessage = $"Saved config to '{normalizedPath}'.";
                RefreshShellState();
            });
    }

    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(PendingConfigPath))
        {
            StatusMessage = "Enter a JSON config path to load.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var result = await _loadConfigUseCase.ExecuteAsync(PendingConfigPath.Trim(), cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                PendingSaveConfigPath = PendingConfigPath.Trim();
                ManualFlowSummary = $"Loaded JSON config '{result.Value.Name}' through the versioned config repository.";
                StatusMessage = $"Loaded JSON config '{result.Value.Name}'.";
                RefreshShellState();
                RefreshPreviewSelection();
                ClearPreviewImages();
            });
    }

    [RelayCommand]
    private async Task ImportLegacyConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(PendingLegacyCsvPath))
        {
            StatusMessage = "Enter a legacy CSV path to import.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var result = await _importLegacyCsvConfigUseCase.ExecuteAsync(PendingLegacyCsvPath.Trim(), cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                ManualFlowSummary = $"Imported legacy CSV '{Path.GetFileName(PendingLegacyCsvPath)}' as a validated editor config.";
                StatusMessage = $"Imported legacy CSV into config '{result.Value.Name}'.";
                RefreshShellState();
                RefreshPreviewSelection();
                ClearPreviewImages();
            });
    }

    [RelayCommand]
    private async Task StagePreviewSelectionAsync()
    {
        var selection = new PreviewSelection(
            BaseStateName?.Trim() ?? string.Empty,
            NormalizeOptionalState(LandmarkStateName),
            NormalizeOptionalState(OverlayStateName));

        var result = _editorSession.SetPreviewSelection(selection);
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        BaseStateName = selection.BaseState;
        LandmarkStateName = selection.LandmarkState ?? string.Empty;
        OverlayStateName = selection.OverlayState ?? string.Empty;
        RefreshPreviewSelection();

        if (_editorSession.LoadedAsset is null || _editorSession.CurrentConfig is null)
        {
            StatusMessage = "Preview selection staged. Load a DMI and config to build a preview.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var previewResult = await _buildPreviewUseCase.ExecuteAsync(cancellationToken);
                if (previewResult.IsFailure)
                {
                    StatusMessage = previewResult.Error.Message;
                    ClearPreviewImages();
                    return;
                }

                ApplyPreviewSummary(previewResult.Value);
                StatusMessage = "Preview built successfully.";
            });
    }

    [RelayCommand]
    private void ResetWorkspace()
    {
        var result = _startEmptyWorkspaceUseCase.Execute();
        StatusMessage = result.IsSuccess
            ? "Workspace reset to the empty-shell state."
            : result.Error.Message;

        ResetStagingInputs();
        RefreshShellState();
        RefreshPreviewSelection();
        ClearPreviewImages();
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        if (_activeOperationCts is null)
        {
            IsBusy = false;
            StatusMessage = "No cancellable task is active in the shell yet.";
            return;
        }

        _activeOperationCts.Cancel();
        StatusMessage = "Cancellation requested.";
    }

    private bool CanCancel() => IsBusy;

    private void ResetStagingInputs()
    {
        PendingDmiPath = string.Empty;
        DraftConfigName = "New Config";
        PendingConfigPath = string.Empty;
        PendingSaveConfigPath = string.Empty;
        PendingLegacyCsvPath = string.Empty;
        BaseStateName = string.Empty;
        LandmarkStateName = string.Empty;
        OverlayStateName = string.Empty;
        ManualFlowSummary =
            "The shell executes open, save, load, import, and preview flows through Application use cases without file logic in code-behind.";
    }

    private void RefreshShellState()
    {
        var workspace = _editorSession.Workspace;
        WorkspaceTitle = workspace.IsEmpty
            ? "Empty workspace"
            : workspace.DisplayName ?? "Sprite workspace";

        CurrentDirectionText = _editorSession.SelectedDirection.ToString();

        if (_editorSession.LoadedAsset is { } asset)
        {
            SpriteContractSummary =
                $"{asset.Resolution} | {asset.SupportedDirections} directions | {asset.States.Count} states";
            RefreshDirectionBadges(asset.SupportedDirections.GetDirections());
        }
        else
        {
            SpriteContractSummary = "No sprite loaded yet. Resolution, direction set, and states will appear after DMI load.";
            RefreshDirectionBadges(SupportedDirectionSet.Eight.GetDirections());
        }

        if (_editorSession.CurrentConfig is { } config)
        {
            var mappingCount = config.Directions.Sum(direction => config.GetMappings(direction).Count);
            var storageLabel = string.IsNullOrWhiteSpace(_editorSession.CurrentConfigPath)
                ? "unsaved draft"
                : Path.GetFileName(_editorSession.CurrentConfigPath);

            ConfigSummary = $"{config.Name} | {mappingCount} mappings | {storageLabel}";
            WorkspaceNotes =
                "A config is active in the editor session. Preview and JSON save now run through application services while batch wiring still remains to be added.";
        }
        else
        {
            ConfigSummary = "No config loaded";
            WorkspaceNotes =
                "The shell starts with an empty workspace and no demo assets. Presentation only stages UI state; execution stays behind Application contracts.";
        }
    }

    private void RefreshDirectionBadges(IEnumerable<SpriteDirection> directions)
    {
        DirectionBadges.Clear();

        foreach (var direction in directions)
        {
            DirectionBadges.Add(direction == _editorSession.SelectedDirection
                ? $"{direction} (selected)"
                : direction.ToString());
        }
    }

    private void RefreshPreviewSelection()
    {
        CurrentDirectionText = _editorSession.SelectedDirection.ToString();

        var selection = _editorSession.PreviewSelection;
        PreviewSelectionSummary = string.IsNullOrWhiteSpace(selection.BaseState)
            ? $"Direction: {_editorSession.SelectedDirection}. Base state is not selected yet."
            : $"Direction: {_editorSession.SelectedDirection}. Base state '{selection.BaseState}' is staged.";

        BasePreviewText = string.IsNullOrWhiteSpace(selection.BaseState)
            ? "No base state selected yet."
            : $"Base state '{selection.BaseState}' is staged. Image rendering awaits preview generation.";

        LandmarkPreviewText = string.IsNullOrWhiteSpace(selection.LandmarkState)
            ? "No landmark selected. Optional input is safely ignored."
            : $"Landmark state '{selection.LandmarkState}' is staged. Missing landmark remains non-fatal.";

        OverlayPreviewText = string.IsNullOrWhiteSpace(selection.OverlayState)
            ? "No overlay selected. Optional input is safely ignored."
            : $"Overlay state '{selection.OverlayState}' is staged. Missing overlay remains non-fatal.";

        CompositePreviewText = "Composite preview is not built yet.";
    }

    private async Task RunBusyOperationAsync(Func<CancellationToken, Task> operation)
    {
        if (IsBusy)
        {
            StatusMessage = "Wait for the current operation to finish or cancel it first.";
            return;
        }

        using var cancellationSource = new CancellationTokenSource();
        _activeOperationCts = cancellationSource;
        IsBusy = true;

        try
        {
            await operation(cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
            ClearPreviewImages();
        }
        finally
        {
            _activeOperationCts = null;
            IsBusy = false;
        }
    }

    private void ApplyPreviewSummary(PreviewBuildResult preview)
    {
        BasePreviewImage = _bitmapSourceFactory.Create(preview.BaseImage);
        LandmarkPreviewImage = _bitmapSourceFactory.Create(preview.LandmarkImage);
        OverlayPreviewImage = _bitmapSourceFactory.Create(preview.OverlayImage);
        CompositePreviewImage = _bitmapSourceFactory.Create(preview.CompositeImage);
        BasePreviewText = DescribePreview("Base", preview.BaseImage);
        LandmarkPreviewText = preview.LandmarkImage is null
            ? "No landmark selected or the requested landmark state was missing. This remains non-fatal."
            : DescribePreview("Landmark", preview.LandmarkImage);
        OverlayPreviewText = preview.OverlayImage is null
            ? "No overlay selected or the requested overlay state was missing. This remains non-fatal."
            : DescribePreview("Overlay", preview.OverlayImage);
        CompositePreviewText = DescribePreview("Composite", preview.CompositeImage);
        PreviewSelectionSummary =
            $"Direction: {_editorSession.SelectedDirection}. Composite preview built at {preview.CompositeImage?.Width}x{preview.CompositeImage?.Height}.";
    }

    private void ClearPreviewImages()
    {
        BasePreviewImage = null;
        LandmarkPreviewImage = null;
        OverlayPreviewImage = null;
        CompositePreviewImage = null;
        CompositePreviewText = "Composite preview is not built yet.";
    }

    private static string DescribePreview(string label, SpriteImage? image) =>
        image is null
            ? $"{label} preview is unavailable."
            : $"{label} preview built at {image.Width}x{image.Height} with {image.RgbaBytes.Length} RGBA bytes.";

    private static string? NormalizeOptionalState(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}