using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class WorkspaceShellViewModel
{
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
        "Empty workspace first. Open a DMI, create or load a JSON config, then edit mappings through the MVVM shell.";

    [ObservableProperty]
    private string statusMessage = "Ready. Empty workspace created. No demo assets were loaded.";

    [ObservableProperty]
    private string currentDirectionText = SpriteDirection.South.ToString();

    [ObservableProperty]
    private string currentStateSummary = "No DMI state selected yet.";

    [ObservableProperty]
    private string editorStatus = "Select a source pixel or drag an area to begin editing.";

    [ObservableProperty]
    private string selectedSourceSummary = "No source pixel selected.";

    [ObservableProperty]
    private string selectedAreaSummary = "No area selected.";

    [ObservableProperty]
    private string hoverSummary = "Hover a cell to inspect coordinates.";

    [ObservableProperty]
    private string previewSelectionSummary = "Preview selection is not staged yet.";

    [ObservableProperty]
    private string previewSummary = "Build a preview to render the selected base, landmark, and overlay states.";

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
    private bool isPreviewInspectorExpanded = true;

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

    public bool HasLoadedAsset => _editorSession.LoadedAsset is not null;

    public bool HasActiveConfig => _editorSession.CurrentConfig is not null;

    public bool HasEditorWorkflow => HasLoadedAsset || HasActiveConfig;

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
