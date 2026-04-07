using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public enum EditorViewMode
{
    EditableOnly = 0,
    CompareSplit = 1,
    OverlayCompare = 2
}

public partial class WorkspaceShellViewModel
{
    private readonly double _minEditorZoom = 1.0;
    private readonly double _maxEditorZoom = 12.0;
    private readonly double _zoomStep = 0.25;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedShellSectionIndex))]
    [NotifyPropertyChangedFor(nameof(IsStartSectionSelected))]
    [NotifyPropertyChangedFor(nameof(IsEditorSectionSelected))]
    [NotifyPropertyChangedFor(nameof(IsBatchSectionSelected))]
    [NotifyPropertyChangedFor(nameof(IsSettingsSectionSelected))]
    private ShellSectionKind selectedShellSection = ShellSectionKind.Start;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string workspaceTitle = "Empty workspace";

    [ObservableProperty]
    private string spriteContractSummary = "No sprite loaded yet. Open a DMI to populate the workspace.";

    [ObservableProperty]
    private string configSummary = "No config loaded";

    [ObservableProperty]
    private string workspaceNotes =
        "Open a DMI, create or load a config, then edit mappings directly in the matrix.";

    [ObservableProperty]
    private string statusMessage = "Ready.";

    [ObservableProperty]
    private string currentDirectionText = SpriteDirection.South.ToString();

    [ObservableProperty]
    private string currentStateSummary = "No DMI state selected yet.";

    [ObservableProperty]
    private string editorStatus = "Edit mappings directly in the matrix.";

    [ObservableProperty]
    private string selectedSourceSummary = "Source: none selected.";

    [ObservableProperty]
    private string selectedAreaSummary = "Area: none selected.";

    [ObservableProperty]
    private string hoverSummary = "Hover to inspect coordinates.";

    [ObservableProperty]
    private string previewSelectionSummary = "Preview follows the current base, landmark, and overlay states.";

    [ObservableProperty]
    private string previewSummary = "Preview becomes available after loading a sprite and config.";

    [ObservableProperty]
    private string previewTextGrid = "No config grid is available yet.";

    [ObservableProperty]
    private string batchSummary = "Batch processing is idle.";

    [ObservableProperty]
    private string batchCurrentFile = string.Empty;

    [ObservableProperty]
    private string dmiPath = string.Empty;

    [ObservableProperty]
    private string configPath = string.Empty;

    [ObservableProperty]
    private string saveConfigPath = string.Empty;

    [ObservableProperty]
    private string legacyCsvPath = string.Empty;

    [ObservableProperty]
    private string batchInputDirectory = string.Empty;

    [ObservableProperty]
    private string batchOutputDirectory = string.Empty;

    [ObservableProperty]
    private string draftConfigName = "New Config";

    [ObservableProperty]
    private string baseStateName = string.Empty;

    [ObservableProperty]
    private string landmarkStateName = string.Empty;

    [ObservableProperty]
    private string overlayStateName = string.Empty;

    [ObservableProperty]
    private string selectedExplorerState = string.Empty;

    [ObservableProperty]
    private MappingRowViewModel? selectedMapping;

    [ObservableProperty]
    private BatchSourceTreeItemViewModel? selectedBatchSourceItem;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool isBusy;

    [ObservableProperty]
    private bool isPreviewRefreshing;

    [ObservableProperty]
    private bool isProgressIndeterminate;

    [ObservableProperty]
    private double operationProgressValue;

    [ObservableProperty]
    private double operationProgressMaximum = 1;

    [ObservableProperty]
    private int batchProcessedFiles;

    [ObservableProperty]
    private int batchTotalFiles;

    [ObservableProperty]
    private SpriteDirection selectedDirection = SpriteDirection.South;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveEditorZoomLabel))]
    private double activeEditorZoom = 2.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditableOnlyMode))]
    [NotifyPropertyChangedFor(nameof(IsCompareSplitMode))]
    [NotifyPropertyChangedFor(nameof(IsOverlayCompareMode))]
    [NotifyPropertyChangedFor(nameof(IsReferencePaneVisible))]
    [NotifyPropertyChangedFor(nameof(ShowOverlayCompareLayer))]
    private EditorViewMode editorViewMode = EditorViewMode.EditableOnly;

    [ObservableProperty]
    private PixelCoordinate? hoveredCoordinate;

    [ObservableProperty]
    private PixelCoordinate? selectedSourceCoordinateView;

    [ObservableProperty]
    private PixelCoordinate? selectedTargetCoordinate;

    [ObservableProperty]
    private PixelAreaBounds? selectedAreaBounds;

    [ObservableProperty]
    private EditorSurfaceRenderState? activeSourceSurface;

    [ObservableProperty]
    private EditorSurfaceRenderState? activeTargetSurface;

    [ObservableProperty]
    private EditorTool selectedEditorTool = EditorTool.Single;

    [ObservableProperty]
    private DirectionScope selectedDirectionScope = DirectionScope.Single;

    [ObservableProperty]
    private PreviewDisplayMode selectedPreviewDisplayMode = PreviewDisplayMode.Composite;

    [ObservableProperty]
    private AutoPreviewMode autoPreviewMode = AutoPreviewMode.Enabled;

    [ObservableProperty]
    private OverwritePolicy selectedOverwritePolicy = OverwritePolicy.SkipExisting;

    [ObservableProperty]
    private EditorViewportMode selectedEditorViewportMode = EditorViewportMode.Matrix;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBottomWorkspaceTabIndex))]
    private BottomWorkspaceTab selectedBottomWorkspaceTab = BottomWorkspaceTab.Assets;

    [ObservableProperty]
    private bool isBottomWorkspaceExpanded;

    [ObservableProperty]
    private bool isPreviewInspectorExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReferencePaneVisible))]
    [NotifyPropertyChangedFor(nameof(ShowStatesRail))]
    [NotifyPropertyChangedFor(nameof(ShowSingleStateStrip))]
    [NotifyPropertyChangedFor(nameof(UseHorizontalDirectionsStrip))]
    [NotifyPropertyChangedFor(nameof(UseVerticalDirectionsRail))]
    [NotifyPropertyChangedFor(nameof(HasDirectionSelector))]
    [NotifyPropertyChangedFor(nameof(ShowOverlayCompareLayer))]
    private bool isFocusMode;

    [ObservableProperty]
    private bool mirrorAcrossDirections;

    [ObservableProperty]
    private bool useCentralizedPropagation = true;

    [ObservableProperty]
    private bool showGrid = true;

    [ObservableProperty]
    private bool gridAboveImage;

    [ObservableProperty]
    private bool showOverlay = true;

    [ObservableProperty]
    private bool showTextGrid = true;

    [ObservableProperty]
    private int directionMatrixColumns = 2;

    [ObservableProperty]
    private DirectionTileViewModel? focusedDirectionTile;

    [ObservableProperty]
    private BitmapSource? currentPreviewImage;

    [ObservableProperty]
    private bool isPreviewImageVisible = true;

    [ObservableProperty]
    private bool isPreviewGridVisible;

    [ObservableProperty]
    private bool isPreviewTextVisible;

    [ObservableProperty]
    private int directionNavigatorColumns = 2;

    [ObservableProperty]
    private int navigatorSnapshotVersion;

    public ObservableCollection<DirectionNavigatorItemViewModel> DirectionNavigatorItems { get; } = [];

    internal Dictionary<(SpriteDirection Direction, bool ShowOverlay, int Version), BitmapSource> NavigatorSnapshotCache { get; } = [];

    public double MinEditorZoom => _minEditorZoom;

    public double MaxEditorZoom => _maxEditorZoom;

    public double ZoomStep => _zoomStep;

    public string ActiveEditorZoomLabel => $"{Math.Round(ActiveEditorZoom * 100):0}%";

    public bool IsEditableOnlyMode => EditorViewMode == EditorViewMode.EditableOnly;

    public bool IsCompareSplitMode => EditorViewMode == EditorViewMode.CompareSplit;

    public bool IsOverlayCompareMode => EditorViewMode == EditorViewMode.OverlayCompare;

    public bool IsReferencePaneVisible => IsCompareSplitMode && !IsFocusMode;

    public bool ShowOverlayCompareLayer => IsOverlayCompareMode && !IsFocusMode;

    public bool HasSelectedAreaBounds => SelectedAreaBounds is not null;

    public bool HasMultipleStates => AvailableStates.Count > 1;

    public bool IsSingleStateWorkflow => AvailableStates.Count <= 1;

    public bool ShowStatesRail => HasMultipleStates && !IsFocusMode;

    public bool ShowSingleStateStrip => IsSingleStateWorkflow && !IsFocusMode && !string.IsNullOrWhiteSpace(SelectedExplorerState);

    public bool UseHorizontalDirectionsStrip => AvailableDirections.Count == 4 && !IsFocusMode;

    public bool UseVerticalDirectionsRail => AvailableDirections.Count > 4 && !IsFocusMode;

    public bool HasDirectionSelector => AvailableDirections.Count > 1 && !IsFocusMode;

    public bool HasLoadedAsset => _editorSession.LoadedAsset is not null;

    public bool HasActiveConfig => _editorSession.CurrentConfig is not null;

    public bool HasEditorWorkflow => HasLoadedAsset || HasActiveConfig;

    public bool CanFitViewport => ResolveEditorResolution() is not null && !IsBusy;

    public int SelectedShellSectionIndex
    {
        get => (int)SelectedShellSection;
        set
        {
            if (Enum.IsDefined(typeof(ShellSectionKind), value))
            {
                NavigateToSection((ShellSectionKind)value);
            }
        }
    }

    public int SelectedBottomWorkspaceTabIndex
    {
        get => (int)SelectedBottomWorkspaceTab;
        set
        {
            if (Enum.IsDefined(typeof(BottomWorkspaceTab), value))
            {
                SelectedBottomWorkspaceTab = (BottomWorkspaceTab)value;
            }
        }
    }

    public bool IsStartSectionSelected => SelectedShellSection == ShellSectionKind.Start;

    public bool IsEditorSectionSelected => SelectedShellSection == ShellSectionKind.Editor;

    public bool IsBatchSectionSelected => SelectedShellSection == ShellSectionKind.Batch;

    public bool IsSettingsSectionSelected => SelectedShellSection == ShellSectionKind.Settings;
}
