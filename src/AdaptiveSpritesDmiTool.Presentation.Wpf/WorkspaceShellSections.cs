using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public enum ShellSectionKind
{
    Start = 0,
    Editor = 1,
    Batch = 2,
    Settings = 3
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

public sealed class NavigationRailViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public ObservableCollection<NavigationRailItemViewModel> Items { get; } =
    [
        new NavigationRailItemViewModel(shell, ShellSectionKind.Start, "Start", "\uE80F", () => true),
        new NavigationRailItemViewModel(shell, ShellSectionKind.Editor, "Editor", "\uE70F", () => shell.EditorWorkspace.IsAvailable),
        new NavigationRailItemViewModel(shell, ShellSectionKind.Batch, "Batch", "\uE8B7", () => shell.BatchWorkspace.IsAvailable),
        new NavigationRailItemViewModel(shell, ShellSectionKind.Settings, "Settings", "\uE713", () => true)
    ];

    public ShellSectionKind SelectedSection
    {
        get => Shell.SelectedShellSection;
        set => Shell.NavigateToSection(value);
    }

    protected override void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        base.OnShellPropertyChanged(sender, e);

        foreach (var item in Items)
        {
            item.Refresh();
        }
    }
}

public sealed class StartTabViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public string WelcomeTitle => Shell.HasLoadedAsset ? "Continue with this sprite" : "Open or import a workspace";

    public string WelcomeBody => Shell.HasLoadedAsset
        ? "The sprite is ready. Create, load, or resume a config to move into the editor."
        : "Open a DMI first, then load JSON or import a legacy CSV when you need an existing mapping set.";

    public bool ShowCreateConfigAction => Shell.HasLoadedAsset && !Shell.HasActiveConfig;

    public bool ShowContinueEditorAction => Shell.HasActiveConfig;

    public bool ShowOpenDmiPrimaryAction => !ShowCreateConfigAction && !ShowContinueEditorAction;

    public bool HasRecentDmi => !string.IsNullOrWhiteSpace(Shell.DmiPath);

    public bool HasRecentConfig => !string.IsNullOrWhiteSpace(Shell.ConfigPath);

    public bool HasRecentLegacyCsv => !string.IsNullOrWhiteSpace(Shell.LegacyCsvPath);

    public bool HasRecentWorkspace => Shell.HasEditorWorkflow || HasRecentDmi || HasRecentConfig || HasRecentLegacyCsv;

    public bool ShowRecentPaths => HasRecentDmi || HasRecentConfig || HasRecentLegacyCsv || !string.IsNullOrWhiteSpace(Shell.BatchOutputDirectory);

    public string ResumeEditorHint => Shell.HasActiveConfig
        ? "A sprite and config are already staged. Continue in Editor to keep working."
        : Shell.HasLoadedAsset
            ? "A sprite is loaded. Create a config to begin editing."
            : "Start by opening a DMI.";

    public string LastDmiPathSummary => string.IsNullOrWhiteSpace(Shell.DmiPath) ? "No DMI selected yet." : Shell.DmiPath;

    public string LastConfigPathSummary => string.IsNullOrWhiteSpace(Shell.ConfigPath) ? "No JSON config selected yet." : Shell.ConfigPath;

    public string LastLegacyImportSummary => string.IsNullOrWhiteSpace(Shell.LegacyCsvPath) ? "No legacy CSV imported yet." : Shell.LegacyCsvPath;

    public string LastBatchOutputSummary => string.IsNullOrWhiteSpace(Shell.BatchOutputDirectory) ? "No batch output folder selected yet." : Shell.BatchOutputDirectory;

    public string DraftConfigName
    {
        get => Shell.DraftConfigName;
        set => Shell.DraftConfigName = value;
    }

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

public sealed class EditorCommandBarViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public IReadOnlyList<EditorTool> PaintTools { get; } = [EditorTool.Single, EditorTool.Fill];

    public IReadOnlyList<EditorTool> EditTools { get; } = [EditorTool.Move, EditorTool.Select, EditorTool.Delete, EditorTool.Undo, EditorTool.UndoArea];

    public IReadOnlyList<EditorTool> EditorTools { get; } = [EditorTool.Single, EditorTool.Fill, EditorTool.Delete, EditorTool.Undo, EditorTool.UndoArea, EditorTool.Select, EditorTool.Move];

    public IReadOnlyList<EditorViewportMode> ViewportModes { get; } = [EditorViewportMode.Matrix, EditorViewportMode.Focused];

    public IReadOnlyList<DirectionScope> DirectionScopes => Shell.DirectionScopes;

    public IReadOnlyList<SpriteDirection> AvailableDirections => Shell.AvailableDirections;

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

    public EditorViewportMode SelectedViewportMode
    {
        get => Shell.SelectedEditorViewportMode;
        set => Shell.SelectedEditorViewportMode = value;
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

    public bool IsSingleToolSelected => SelectedEditorTool == EditorTool.Single;

    public bool IsFillToolSelected => SelectedEditorTool == EditorTool.Fill;

    public bool IsMoveToolSelected => SelectedEditorTool == EditorTool.Move;

    public bool IsSelectToolSelected => SelectedEditorTool == EditorTool.Select;

    public bool IsDeleteToolSelected => SelectedEditorTool == EditorTool.Delete;

    public bool IsUndoToolSelected => SelectedEditorTool == EditorTool.Undo;

    public bool IsUndoAreaToolSelected => SelectedEditorTool == EditorTool.UndoArea;

    public bool IsSingleScopeSelected => SelectedDirectionScope == DirectionScope.Single;

    public bool IsParallelScopeSelected => SelectedDirectionScope == DirectionScope.Parallel;

    public bool IsAllScopeSelected => SelectedDirectionScope == DirectionScope.All;

    public bool IsMatrixViewportSelected => SelectedViewportMode == EditorViewportMode.Matrix;

    public bool IsFocusedViewportSelected => SelectedViewportMode == EditorViewportMode.Focused;

    public bool ShowFocusedDirectionPicker => IsFocusedViewportSelected && AvailableDirections.Count > 1;

    public IRelayCommand<EditorTool> SelectEditorToolCommand => Shell.SelectEditorToolCommand;

    public IRelayCommand<DirectionScope> SelectDirectionScopeCommand => Shell.SelectDirectionScopeCommand;

    public IRelayCommand<EditorViewportMode> SelectViewportModeCommand => Shell.SelectViewportModeCommand;

    public IRelayCommand ClearSelectionCommand => Shell.ClearSelectionCommand;

    public IRelayCommand UndoCommand => Shell.UndoCommand;

    public IRelayCommand RedoCommand => Shell.RedoCommand;
}

public sealed class DirectionMatrixViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public ObservableCollection<DirectionTileViewModel> Tiles => Shell.DirectionTiles;

    public bool IsMatrixMode => Shell.SelectedEditorViewportMode == EditorViewportMode.Matrix;

    public bool IsFocusedMode => Shell.SelectedEditorViewportMode == EditorViewportMode.Focused;

    public int MatrixColumns => Shell.DirectionMatrixColumns;

    public DirectionTileViewModel? FocusedTile => Shell.FocusedDirectionTile;
}

public sealed class EditorWorkspaceViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public bool IsAvailable => Shell.HasEditorWorkflow;

    public string SpriteContractSummary => Shell.SpriteContractSummary;

    public string ConfigSummary => Shell.ConfigSummary;

    public string RolesSummary =>
        $"{BaseStateSummary} • {LandmarkStateSummary} • {OverlayStateSummary}";

    public string BaseStateSummary => string.IsNullOrWhiteSpace(Shell.BaseStateName) ? "Base: not selected" : $"Base: {Shell.BaseStateName}";

    public string LandmarkStateSummary => string.IsNullOrWhiteSpace(Shell.LandmarkStateName) ? "Landmark: none" : $"Landmark: {Shell.LandmarkStateName}";

    public string OverlayStateSummary => string.IsNullOrWhiteSpace(Shell.OverlayStateName) ? "Overlay: none" : $"Overlay: {Shell.OverlayStateName}";

    public string LeftRailSummary => Shell.HasActiveConfig
        ? $"Config: {Shell.ConfigSummary}"
        : "Load or create a config to edit mappings.";

    public string HoverAndStatusSummary => $"{Shell.SelectedSourceSummary}  {Shell.SelectedAreaSummary}";

    public string SelectionSummary => $"{Shell.SelectedSourceSummary}  {Shell.SelectedAreaSummary}";

    public ObservableCollection<string> AvailableStates => Shell.AvailableStates;

    public string SelectedExplorerState
    {
        get => Shell.SelectedExplorerState;
        set => Shell.SelectedExplorerState = value;
    }

    public DirectionMatrixViewModel DirectionMatrix { get; } = new(shell);

    public EditorCommandBarViewModel CommandBar { get; } = new(shell);

    public IRelayCommand UseSelectedStateAsBaseCommand => Shell.UseSelectedStateAsBaseCommand;

    public IRelayCommand UseSelectedStateAsLandmarkCommand => Shell.UseSelectedStateAsLandmarkCommand;

    public IRelayCommand UseSelectedStateAsOverlayCommand => Shell.UseSelectedStateAsOverlayCommand;

    public IRelayCommand ClearOptionalPreviewLayersCommand => Shell.ClearOptionalPreviewLayersCommand;
}

public sealed class ConfigWorkspaceViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public ObservableCollection<ConfigQueueItemViewModel> Items => Shell.ConfigQueueItems;

    public string ActiveConfigSummary => Shell.ConfigSummary;

    public IRelayCommand CreateConfigCommand => Shell.CreateConfigCommand;

    public IAsyncRelayCommand LoadConfigCommand => Shell.LoadConfigCommand;

    public IAsyncRelayCommand SaveConfigCommand => Shell.SaveConfigCommand;
}

public sealed class BottomWorkspaceViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public BottomWorkspaceTab SelectedTab
    {
        get => Shell.SelectedBottomWorkspaceTab;
        set => Shell.SelectedBottomWorkspaceTab = value;
    }

    public bool IsExpanded
    {
        get => Shell.IsBottomWorkspaceExpanded;
        set => Shell.IsBottomWorkspaceExpanded = value;
    }

    public ObservableCollection<string> AvailableStates => Shell.AvailableStates;

    public ObservableCollection<ConfigQueueItemViewModel> ConfigQueueItems => Shell.ConfigQueueItems;

    public ObservableCollection<MappingRowViewModel> MappingRows => Shell.MappingRows;

    public ObservableCollection<BatchResultRowViewModel> BatchResults => Shell.BatchResults;

    public string MappingSummary => Shell.MappingRows.Count == 0
        ? "No mappings yet."
        : $"{Shell.MappingRows.Count} mapping(s) in the active direction.";

    public bool HasBatchResults => Shell.BatchResults.Count > 0;

    public MappingRowViewModel? SelectedMapping
    {
        get => Shell.SelectedMapping;
        set => Shell.SelectedMapping = value;
    }

    public IRelayCommand<BottomWorkspaceTab> SelectBottomWorkspaceTabCommand => Shell.SelectBottomWorkspaceTabCommand;

    public IRelayCommand RemoveSelectedMappingCommand => Shell.RemoveSelectedMappingCommand;
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

    public bool IsExpanded
    {
        get => Shell.IsPreviewInspectorExpanded;
        set => Shell.IsPreviewInspectorExpanded = value;
    }

    public string PreviewSelectionSummary => Shell.PreviewSelectionSummary;

    public string PreviewSummary => Shell.PreviewSummary;

    public BitmapSource? CurrentPreviewImage => Shell.CurrentPreviewImage;

    public bool IsPreviewImageVisible => Shell.IsPreviewImageVisible;

    public bool IsPreviewGridVisible => Shell.IsPreviewGridVisible;

    public bool IsPreviewTextVisible => Shell.IsPreviewTextVisible;

    public ObservableCollection<PixelRowViewModel> PreviewGridRows => Shell.PreviewGridRows;

    public string PreviewTextGrid => Shell.PreviewTextGrid;

    public IAsyncRelayCommand BuildPreviewCommand => Shell.BuildPreviewCommand;
}

public sealed class BatchWorkspaceViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public bool IsAvailable => Shell.HasActiveConfig;

    public ObservableCollection<BatchSourceTreeItemViewModel> SourceTreeItems => Shell.BatchSourceTreeItems;

    public ObservableCollection<BatchStateStripItemViewModel> StateStripItems => Shell.BatchStateStripItems;

    public ObservableCollection<ConfigQueueItemViewModel> ConfigQueueItems => Shell.ConfigQueueItems;

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

    public IRelayCommand BrowseBatchInputDirectoryCommand => Shell.BrowseBatchInputDirectoryCommand;

    public IRelayCommand BrowseBatchOutputDirectoryCommand => Shell.BrowseBatchOutputDirectoryCommand;

    public IAsyncRelayCommand LoadConfigCommand => Shell.LoadConfigCommand;

    public IAsyncRelayCommand RunBatchCommand => Shell.RunBatchCommand;

    public IRelayCommand CancelCommand => Shell.CancelCommand;
}

public sealed class SettingsTabViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public IReadOnlyList<EditorViewportMode> ViewportModes { get; } = [EditorViewportMode.Matrix, EditorViewportMode.Focused];

    public AutoPreviewMode AutoPreviewMode
    {
        get => Shell.AutoPreviewMode;
        set => Shell.AutoPreviewMode = value;
    }

    public EditorViewportMode SelectedViewportMode
    {
        get => Shell.SelectedEditorViewportMode;
        set => Shell.SelectedEditorViewportMode = value;
    }

    public bool IsPreviewInspectorExpanded
    {
        get => Shell.IsPreviewInspectorExpanded;
        set => Shell.IsPreviewInspectorExpanded = value;
    }

    public bool IsBottomWorkspaceExpanded
    {
        get => Shell.IsBottomWorkspaceExpanded;
        set => Shell.IsBottomWorkspaceExpanded = value;
    }
}

public sealed class OperationalStatusBarViewModel(WorkspaceShellViewModel shell) : ShellSectionViewModel(shell)
{
    public string StatusMessage => Shell.StatusMessage;

    public string CurrentStateSummary => Shell.CurrentStateSummary;

    public string ActiveDirection => Shell.SelectedDirection.ToString();

    public string ActiveTool => Shell.SelectedEditorTool.ToString();

    public string HoverSummary => Shell.HoverSummary;

    public string SelectionSummary => $"{Shell.SelectedSourceSummary} | {Shell.SelectedAreaSummary}";

    public string MappingSummary => Shell.MappingRows.Count == 0 ? "Mappings 0" : $"Mappings {Shell.MappingRows.Count}";

    public string BatchSummary => Shell.BatchSummary;

    public bool IsBusy => Shell.IsBusy;

    public bool IsProgressIndeterminate => Shell.IsProgressIndeterminate;

    public double OperationProgressValue => Shell.OperationProgressValue;

    public double OperationProgressMaximum => Shell.OperationProgressMaximum;

    public IRelayCommand CancelCommand => Shell.CancelCommand;
}
