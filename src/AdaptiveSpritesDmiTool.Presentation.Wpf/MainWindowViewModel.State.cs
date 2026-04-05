using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string workspaceTitle = "Empty workspace";

    [ObservableProperty]
    private string spriteContractSummary = "No sprite loaded yet. Open a DMI to populate the workspace.";

    [ObservableProperty]
    private string configSummary = "No config loaded";

    [ObservableProperty]
    private string workspaceNotes =
        "Empty workspace first. Open a DMI, create or load a JSON config, then edit mappings through the new MVVM shell.";

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
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool isBusy;

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
    private OverwritePolicy selectedOverwritePolicy = OverwritePolicy.SkipExisting;

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
    private BitmapSource? currentPreviewImage;

    [ObservableProperty]
    private bool isPreviewImageVisible = true;

    [ObservableProperty]
    private bool isPreviewGridVisible;

    [ObservableProperty]
    private bool isPreviewTextVisible;
}
