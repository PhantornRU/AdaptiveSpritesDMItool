using AdaptiveSpritesDmiTool.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using System.IO;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public enum ShellTabKind
{
    Start = 0,
    Editor = 1,
    Batch = 2
}

public enum AutoPreviewMode
{
    Enabled = 0,
    Disabled = 1
}

public sealed class PreviewRefreshCoordinator(TimeSpan? debounce = null) : IDisposable
{
    private readonly TimeSpan _debounce = debounce ?? TimeSpan.FromMilliseconds(250);
    private CancellationTokenSource? _refreshCts;

    public void Request(Func<CancellationToken, Task> refreshAsync)
    {
        ArgumentNullException.ThrowIfNull(refreshAsync);
        Cancel();

        var cts = new CancellationTokenSource();
        _refreshCts = cts;
        _ = RunAsync(refreshAsync, cts);
    }

    public async Task RefreshNowAsync(Func<CancellationToken, Task> refreshAsync)
    {
        ArgumentNullException.ThrowIfNull(refreshAsync);
        Cancel();

        var cts = new CancellationTokenSource();
        _refreshCts = cts;

        try
        {
            await refreshAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_refreshCts, cts))
            {
                _refreshCts = null;
            }

            cts.Dispose();
        }
    }

    public void Cancel()
    {
        var cts = Interlocked.Exchange(ref _refreshCts, null);
        if (cts is null)
        {
            return;
        }

        cts.Cancel();
        cts.Dispose();
    }

    public void Dispose() => Cancel();

    private async Task RunAsync(Func<CancellationToken, Task> refreshAsync, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_debounce, cts.Token);
            await refreshAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_refreshCts, cts))
            {
                _refreshCts = null;
            }

            cts.Dispose();
        }
    }
}

public abstract class ShellSectionViewModel(WorkspaceShellViewModel shell) : ObservableObject
{
    protected WorkspaceShellViewModel Shell { get; } = shell;

    public virtual void Attach()
    {
        Shell.PropertyChanged += OnShellPropertyChanged;
    }

    public virtual void Detach()
    {
        Shell.PropertyChanged -= OnShellPropertyChanged;
    }

    protected virtual void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(string.Empty);
    }
}

public sealed class StartTabViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public string WelcomeTitle => Shell.HasLoadedAsset ? "Continue with this sprite" : "Open or import a workspace";

    public string WelcomeBody => Shell.HasLoadedAsset
        ? "The sprite is ready. Create or load a config to move into the editor."
        : "Open a DMI first, then load JSON or import a legacy CSV when you need an existing mapping set.";

    public bool ShowCreateConfigAction => Shell.HasLoadedAsset && !Shell.HasActiveConfig;

    public bool ShowResumeEditorHint => Shell.HasEditorWorkflow;

    public bool ShowContinueEditorAction => Shell.HasActiveConfig;

    public bool ShowOpenDmiPrimaryAction => !ShowCreateConfigAction && !ShowContinueEditorAction;

    public bool HasRecentDmi => !string.IsNullOrWhiteSpace(Shell.DmiPath);

    public bool HasRecentConfig => !string.IsNullOrWhiteSpace(Shell.ConfigPath);

    public bool HasRecentLegacyCsv => !string.IsNullOrWhiteSpace(Shell.LegacyCsvPath);

    public bool HasRecentWorkspace => Shell.HasEditorWorkflow || HasRecentDmi || HasRecentConfig || HasRecentLegacyCsv;

    public bool ShowRecentPaths => HasRecentDmi || HasRecentConfig || HasRecentLegacyCsv || !string.IsNullOrWhiteSpace(Shell.BatchOutputDirectory);

    public string ResumeEditorHint => Shell.HasActiveConfig
        ? "A sprite and config are already staged. Switch to Editor to continue working."
        : Shell.HasLoadedAsset
            ? "A sprite is loaded. Create a config to begin editing."
            : "Start by opening a DMI.";

    public string LastDmiPathSummary => string.IsNullOrWhiteSpace(Shell.DmiPath) ? "No DMI selected yet." : Shell.DmiPath;

    public string LastConfigPathSummary => string.IsNullOrWhiteSpace(Shell.ConfigPath) ? "No JSON config selected yet." : Shell.ConfigPath;

    public string LastLegacyImportSummary => string.IsNullOrWhiteSpace(Shell.LegacyCsvPath) ? "No legacy CSV imported yet." : Shell.LegacyCsvPath;

    public string LastBatchOutputSummary => string.IsNullOrWhiteSpace(Shell.BatchOutputDirectory) ? "No batch output folder selected yet." : Shell.BatchOutputDirectory;

    public string LastDmiDisplayName => HasRecentDmi ? Path.GetFileName(Shell.DmiPath) : "No DMI selected yet.";

    public string LastConfigDisplayName => HasRecentConfig ? Path.GetFileName(Shell.ConfigPath) : "No JSON config selected yet.";

    public string LastLegacyCsvDisplayName => HasRecentLegacyCsv ? Path.GetFileName(Shell.LegacyCsvPath) : "No legacy CSV imported yet.";

    public string DraftConfigName
    {
        get => Shell.DraftConfigName;
        set => Shell.DraftConfigName = value;
    }

    public string RecentSummary => string.Join(
        Environment.NewLine,
        new[]
        {
            $"Last DMI: {LastDmiPathSummary}",
            $"Last JSON: {LastConfigPathSummary}",
            $"Last CSV: {LastLegacyImportSummary}",
            $"Last Batch Output: {LastBatchOutputSummary}"
        });

    public IAsyncRelayCommand OpenDmiCommand => Shell.OpenDmiCommand;

    public IRelayCommand ContinueToEditorCommand => Shell.ContinueToEditorCommand;

    public IAsyncRelayCommand ResumeLastWorkspaceCommand => Shell.ResumeLastWorkspaceCommand;

    public IAsyncRelayCommand OpenRecentDmiCommand => Shell.OpenRecentDmiCommand;

    public IAsyncRelayCommand OpenRecentConfigCommand => Shell.OpenRecentConfigCommand;

    public IAsyncRelayCommand ImportRecentLegacyCsvCommand => Shell.ImportRecentLegacyCsvCommand;

    public IRelayCommand CreateConfigCommand => Shell.CreateConfigCommand;

    public IAsyncRelayCommand LoadConfigCommand => Shell.LoadConfigCommand;

    public IAsyncRelayCommand ImportLegacyConfigCommand => Shell.ImportLegacyConfigCommand;

    public IAsyncRelayCommand ResetWorkspaceCommand => Shell.ResetWorkspaceCommand;
}

public sealed class EditorTabViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public bool IsAvailable => Shell.HasEditorWorkflow;

    public bool HasLoadedAsset => Shell.HasLoadedAsset;

    public bool HasActiveConfig => Shell.HasActiveConfig;

    public string SpriteContractSummary => Shell.SpriteContractSummary;

    public string ConfigSummary => Shell.ConfigSummary;

    public string CurrentStateSummary => Shell.CurrentStateSummary;

    public string PreviewSelectionSummary => Shell.PreviewSelectionSummary;

    public string LeftRailSummary => Shell.HasActiveConfig
        ? Shell.ConfigSummary
        : "No config yet. Load JSON, import CSV, or create a fresh config from Start.";

    public string EditorStatus => Shell.EditorStatus;

    public string SelectedSourceSummary => Shell.SelectedSourceSummary;

    public string SelectedAreaSummary => Shell.SelectedAreaSummary;

    public string HoverSummary => Shell.HoverSummary;

    public string BaseStateSummary => string.IsNullOrWhiteSpace(Shell.BaseStateName) ? "Base: not selected" : $"Base: {Shell.BaseStateName}";

    public string LandmarkStateSummary => string.IsNullOrWhiteSpace(Shell.LandmarkStateName) ? "Landmark: none" : $"Landmark: {Shell.LandmarkStateName}";

    public string OverlayStateSummary => string.IsNullOrWhiteSpace(Shell.OverlayStateName) ? "Overlay: none" : $"Overlay: {Shell.OverlayStateName}";

    public string RolesSummary => $"{BaseStateSummary}   {LandmarkStateSummary}   {OverlayStateSummary}";

    public string SelectionSummary =>
        $"{Shell.SelectedSourceSummary}  {Shell.SelectedAreaSummary}";

    public string HoverAndStatusSummary =>
        $"{Shell.EditorStatus}  {Shell.HoverSummary}";

    public ObservableCollection<SpriteDirection> AvailableDirections => Shell.AvailableDirections;

    public ObservableCollection<string> AvailableStates => Shell.AvailableStates;

    public ObservableCollection<PixelRowViewModel> SourceRows => Shell.SourceRows;

    public ObservableCollection<PixelRowViewModel> TargetRows => Shell.TargetRows;

    public ObservableCollection<MappingRowViewModel> MappingRows => Shell.MappingRows;

    public int MappingCount => MappingRows.Count;

    public string MappingsHeader => MappingCount == 0 ? "Mappings" : $"Mappings ({MappingCount})";

    public bool HasMappings => MappingCount > 0;

    public IReadOnlyList<EditorTool> EditorTools => Shell.EditorTools;

    public IReadOnlyList<DirectionScope> DirectionScopes => Shell.DirectionScopes;

    public SpriteDirection SelectedDirection
    {
        get => Shell.SelectedDirection;
        set => Shell.SelectedDirection = value;
    }

    public EditorTool SelectedEditorTool
    {
        get => Shell.SelectedEditorTool;
        set => Shell.SelectedEditorTool = value;
    }

    public DirectionScope SelectedDirectionScope
    {
        get => Shell.SelectedDirectionScope;
        set => Shell.SelectedDirectionScope = value;
    }

    public bool MirrorAcrossDirections
    {
        get => Shell.MirrorAcrossDirections;
        set => Shell.MirrorAcrossDirections = value;
    }

    public bool UseCentralizedPropagation
    {
        get => Shell.UseCentralizedPropagation;
        set => Shell.UseCentralizedPropagation = value;
    }

    public bool ShowGrid
    {
        get => Shell.ShowGrid;
        set => Shell.ShowGrid = value;
    }

    public bool GridAboveImage
    {
        get => Shell.GridAboveImage;
        set => Shell.GridAboveImage = value;
    }

    public bool ShowOverlay
    {
        get => Shell.ShowOverlay;
        set => Shell.ShowOverlay = value;
    }

    public bool ShowTextGrid
    {
        get => Shell.ShowTextGrid;
        set => Shell.ShowTextGrid = value;
    }

    public string SelectedExplorerState
    {
        get => Shell.SelectedExplorerState;
        set => Shell.SelectedExplorerState = value;
    }

    public MappingRowViewModel? SelectedMapping
    {
        get => Shell.SelectedMapping;
        set => Shell.SelectedMapping = value;
    }

    public IRelayCommand UseSelectedStateAsBaseCommand => Shell.UseSelectedStateAsBaseCommand;

    public IRelayCommand UseSelectedStateAsLandmarkCommand => Shell.UseSelectedStateAsLandmarkCommand;

    public IRelayCommand UseSelectedStateAsOverlayCommand => Shell.UseSelectedStateAsOverlayCommand;

    public IRelayCommand ClearOptionalPreviewLayersCommand => Shell.ClearOptionalPreviewLayersCommand;

    public IRelayCommand ClearSelectionCommand => Shell.ClearSelectionCommand;

    public IRelayCommand UndoCommand => Shell.UndoCommand;

    public IRelayCommand RedoCommand => Shell.RedoCommand;

    public IRelayCommand RemoveSelectedMappingCommand => Shell.RemoveSelectedMappingCommand;
}

public sealed class BatchTabViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public bool IsAvailable => Shell.HasActiveConfig;

    public string BatchInputDirectory
    {
        get => Shell.BatchInputDirectory;
        set => Shell.BatchInputDirectory = value;
    }

    public string BatchOutputDirectory
    {
        get => Shell.BatchOutputDirectory;
        set => Shell.BatchOutputDirectory = value;
    }

    public IReadOnlyList<OverwritePolicy> OverwritePolicies => Shell.OverwritePolicies;

    public OverwritePolicy SelectedOverwritePolicy
    {
        get => Shell.SelectedOverwritePolicy;
        set => Shell.SelectedOverwritePolicy = value;
    }

    public ObservableCollection<BatchResultRowViewModel> BatchResults => Shell.BatchResults;

    public string BatchSummary => Shell.BatchSummary;

    public string BatchCurrentFile => Shell.BatchCurrentFile;

    public int BatchProcessedFiles => Shell.BatchProcessedFiles;

    public int BatchTotalFiles => Shell.BatchTotalFiles;

    public IRelayCommand BrowseBatchInputDirectoryCommand => Shell.BrowseBatchInputDirectoryCommand;

    public IRelayCommand BrowseBatchOutputDirectoryCommand => Shell.BrowseBatchOutputDirectoryCommand;

    public IAsyncRelayCommand RunBatchCommand => Shell.RunBatchCommand;

    public IRelayCommand CancelCommand => Shell.CancelCommand;
}

public sealed class PreviewPanelViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public IReadOnlyList<PreviewDisplayMode> PreviewDisplayModes => Shell.PreviewDisplayModes;

    public PreviewDisplayMode SelectedPreviewDisplayMode
    {
        get => Shell.SelectedPreviewDisplayMode;
        set => Shell.SelectedPreviewDisplayMode = value;
    }

    public AutoPreviewMode AutoPreviewMode
    {
        get => Shell.AutoPreviewMode;
        set => Shell.AutoPreviewMode = value;
    }

    public bool IsAutoPreviewEnabled
    {
        get => Shell.AutoPreviewMode == AutoPreviewMode.Enabled;
        set => Shell.AutoPreviewMode = value ? AutoPreviewMode.Enabled : AutoPreviewMode.Disabled;
    }

    public string PreviewSelectionSummary => Shell.PreviewSelectionSummary;

    public string PreviewSummary => Shell.PreviewSummary;

    public string BaseStateSummary => string.IsNullOrWhiteSpace(Shell.BaseStateName) ? "Base: not selected" : $"Base: {Shell.BaseStateName}";

    public string LandmarkStateSummary => string.IsNullOrWhiteSpace(Shell.LandmarkStateName) ? "Landmark: none" : $"Landmark: {Shell.LandmarkStateName}";

    public string OverlayStateSummary => string.IsNullOrWhiteSpace(Shell.OverlayStateName) ? "Overlay: none" : $"Overlay: {Shell.OverlayStateName}";

    public string PreviewModeSummary => Shell.IsPreviewRefreshing
        ? "Refreshing preview..."
        : Shell.PreviewSummary;

    public BitmapSource? CurrentPreviewImage => Shell.CurrentPreviewImage;

    public bool IsPreviewImageVisible => Shell.IsPreviewImageVisible;

    public bool IsPreviewGridVisible => Shell.IsPreviewGridVisible;

    public bool IsPreviewTextVisible => Shell.IsPreviewTextVisible;

    public ObservableCollection<PixelRowViewModel> PreviewGridRows => Shell.PreviewGridRows;

    public string PreviewTextGrid => Shell.PreviewTextGrid;

    public bool IsPreviewRefreshing => Shell.IsPreviewRefreshing;

    public IAsyncRelayCommand BuildPreviewCommand => Shell.BuildPreviewCommand;
}

public sealed class StatusBarViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public string StatusMessage => Shell.StatusMessage;

    public string BatchSummary => Shell.BatchSummary;

    public string BatchCurrentFile => Shell.BatchCurrentFile;

    public bool IsBusy => Shell.IsBusy;

    public bool IsProgressIndeterminate => Shell.IsProgressIndeterminate;

    public double OperationProgressValue => Shell.OperationProgressValue;

    public double OperationProgressMaximum => Shell.OperationProgressMaximum;

    public IRelayCommand CancelCommand => Shell.CancelCommand;
}
