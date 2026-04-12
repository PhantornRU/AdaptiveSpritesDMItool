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

public enum EditorCanvasKind
{
    Source = 0,
    Editable = 1
}

public enum EditorLeftDockTab
{
    AssetsDmi = 0,
    Configs = 1
}

public enum EditorAssetTargetSurface
{
    Source = 0,
    Editable = 1,
    EditableBackground = 2
}

public enum EditorAssetTargetLayer
{
    Base = 0,
    Landmark = 1,
    Overlay = 2
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
    private ImportedDmiStateItemViewModel? selectedImportedDmiStateItem;

    [ObservableProperty]
    private MappingRowViewModel? selectedMapping;

    [ObservableProperty]
    private BatchSourceTreeItemViewModel? selectedBatchSourceItem;

    [ObservableProperty]
    private BatchStateStripItemViewModel? selectedBatchStateStripItem;

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
    [NotifyPropertyChangedFor(nameof(ShowCompactCanvasHeader))]
    private EditorViewMode editorViewMode = EditorViewMode.CompareSplit;

    [ObservableProperty]
    private PixelCoordinate? sourceHoveredCoordinate;

    [ObservableProperty]
    private PixelCoordinate? editableHoveredCoordinate;

    [ObservableProperty]
    private IReadOnlyList<PixelCoordinate> sourceLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();

    [ObservableProperty]
    private IReadOnlyList<PixelCoordinate> editableLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();

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
    private BottomWorkspaceTab selectedBottomWorkspaceTab = BottomWorkspaceTab.Mappings;

    [ObservableProperty]
    private EditorLeftDockTab selectedEditorLeftDockTab = EditorLeftDockTab.AssetsDmi;

    [ObservableProperty]
    private EditorAssetTargetSurface selectedEditorAssetTargetSurface = EditorAssetTargetSurface.Source;

    [ObservableProperty]
    private EditorAssetTargetLayer selectedEditorAssetTargetLayer = EditorAssetTargetLayer.Base;

    [ObservableProperty]
    private EditorCanvasKind? hoveredCanvasKind;

    [ObservableProperty]
    private PixelCoordinate? oppositeHighlightedCoordinate;

    [ObservableProperty]
    private string hoverMappingSummary = "No hover mapping.";

    [ObservableProperty]
    private bool isBottomWorkspaceExpanded;

    [ObservableProperty]
    private bool isPreviewInspectorExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReferencePaneVisible))]
    [NotifyPropertyChangedFor(nameof(ShowEditorLeftRail))]
    [NotifyPropertyChangedFor(nameof(ShowStateLayersPanel))]
    [NotifyPropertyChangedFor(nameof(ShowSourcePalettePane))]
    [NotifyPropertyChangedFor(nameof(ShowDirectionsInSidebar))]
    [NotifyPropertyChangedFor(nameof(ShowBottomStatusBar))]
    [NotifyPropertyChangedFor(nameof(ShowCompactCanvasHeader))]
    [NotifyPropertyChangedFor(nameof(ShowStatesRail))]
    [NotifyPropertyChangedFor(nameof(ShowSingleStateStrip))]
    [NotifyPropertyChangedFor(nameof(UseHorizontalDirectionsStrip))]
    [NotifyPropertyChangedFor(nameof(UseVerticalDirectionsRail))]
    [NotifyPropertyChangedFor(nameof(HasDirectionSelector))]
    [NotifyPropertyChangedFor(nameof(ShowOverlayCompareLayer))]
    private bool isFocusMode;

    [ObservableProperty]
    private bool mirrorAcrossDirections = true;

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
    private BitmapSource? batchQuickPreviewOriginalImage;

    [ObservableProperty]
    private BitmapSource? batchQuickPreviewEditedImage;

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

    public ObservableCollection<EditorAssetItemViewModel> EditorAssetItems { get; } = [];

    public ObservableCollection<ImportedDmiStateItemViewModel> ImportedDmiStateItems { get; } = [];

    public IReadOnlyList<EditorLeftDockTab> EditorLeftDockTabs { get; } = Enum.GetValues<EditorLeftDockTab>();

    public IReadOnlyList<EditorAssetTargetSurface> EditorAssetTargetSurfaces { get; } = Enum.GetValues<EditorAssetTargetSurface>();

    public IReadOnlyList<EditorAssetTargetLayer> EditorAssetTargetLayers { get; } = Enum.GetValues<EditorAssetTargetLayer>();

    internal Dictionary<(SpriteDirection Direction, bool ShowOverlay, int Version), BitmapSource> NavigatorSnapshotCache { get; } = [];

    public double MinEditorZoom => Math.Max(_minEditorZoom, DetermineEditorZoomBaseline());

    public double MaxEditorZoom => _maxEditorZoom;

    public double ZoomStep => _zoomStep;

    public string ActiveEditorZoomLabel
    {
        get
        {
            var baseline = Math.Max(DetermineEditorZoomBaseline(), 0.001d);
            return $"{Math.Round((ActiveEditorZoom / baseline) * 100):0}%";
        }
    }

    public bool IsEditableOnlyMode => EditorViewMode == EditorViewMode.EditableOnly;

    public bool IsCompareSplitMode => EditorViewMode == EditorViewMode.CompareSplit;

    public bool IsOverlayCompareMode => EditorViewMode == EditorViewMode.OverlayCompare;

    public bool IsReferencePaneVisible => IsCompareSplitMode && !IsFocusMode;

    public bool ShowOverlayCompareLayer => IsOverlayCompareMode && !IsFocusMode;

    public bool ShowEditorLeftRail => !IsFocusMode;

    public bool ShowStateLayersPanel => !IsFocusMode;

    public bool ShowSourcePalettePane => !IsFocusMode;

    public bool ShowDirectionsInSidebar => HasDirectionSelector && !IsFocusMode;

    public bool ShowRightDirectionStrip => HasDirectionSelector && !IsFocusMode;

    public bool ShowBottomStatusBar => !IsFocusMode;

    public bool ShowCompactCanvasHeader => !IsFocusMode;

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

    public string CurrentAssetDisplayName => string.IsNullOrWhiteSpace(SelectedExplorerState)
        ? (HasLoadedAsset ? WorkspaceTitle : "No DMI loaded")
        : SelectedExplorerState;

    public string CurrentDmiDisplayName => HasLoadedAsset
        ? _editorSession.LoadedAsset?.DisplayName ?? WorkspaceTitle
        : "No DMI loaded";

    public string CurrentDmiPathSummary => !string.IsNullOrWhiteSpace(_editorSession.LoadedAsset?.SourcePath)
        ? _editorSession.LoadedAsset!.SourcePath!
        : !string.IsNullOrWhiteSpace(DmiPath)
            ? DmiPath
            : "Current in-memory DMI";

    public string EditableBackgroundSummary => SelectedEditorAssetTargetSurface == EditorAssetTargetSurface.EditableBackground
        ? "Background target reserved for staged v1. Multi-resource stacking arrives in a later pass."
        : "Choose `EditableBackground` to stage future non-editable reference placement.";

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

    public int SelectedEditorLeftDockTabIndex
    {
        get => (int)SelectedEditorLeftDockTab;
        set
        {
            if (Enum.IsDefined(typeof(EditorLeftDockTab), value))
            {
                SelectedEditorLeftDockTab = (EditorLeftDockTab)value;
            }
        }
    }

    public bool IsStartSectionSelected => SelectedShellSection == ShellSectionKind.Start;

    public bool IsEditorSectionSelected => SelectedShellSection == ShellSectionKind.Editor;

    public bool IsBatchSectionSelected => SelectedShellSection == ShellSectionKind.Batch;

    public bool IsSettingsSectionSelected => SelectedShellSection == ShellSectionKind.Settings;
}
