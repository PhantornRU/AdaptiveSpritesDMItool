using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class WorkspaceShellViewModel : ObservableObject, IDisposable
{
    private readonly StartEmptyWorkspaceUseCase _startEmptyWorkspaceUseCase;
    private readonly CreateConfigUseCase _createConfigUseCase;
    private readonly SaveConfigUseCase _saveConfigUseCase;
    private readonly LoadConfigUseCase _loadConfigUseCase;
    private readonly ImportLegacyCsvConfigUseCase _importLegacyCsvConfigUseCase;
    private readonly LoadDmiFileUseCase _loadDmiFileUseCase;
    private readonly BuildPreviewUseCase _buildPreviewUseCase;
    private readonly ApplyConfigToDmiBatchUseCase _applyConfigToDmiBatchUseCase;
    private readonly UndoChangeUseCase _undoChangeUseCase;
    private readonly RedoChangeUseCase _redoChangeUseCase;
    private readonly SetPreviewSelectionUseCase _setPreviewSelectionUseCase;
    private readonly SetSelectedDirectionUseCase _setSelectedDirectionUseCase;
    private readonly ApplyConfigTransformUseCase _applyConfigTransformUseCase;
    private readonly LoadWorkspaceSettingsUseCase _loadWorkspaceSettingsUseCase;
    private readonly SaveWorkspaceSettingsUseCase _saveWorkspaceSettingsUseCase;
    private readonly SpriteImageBitmapSourceFactory _bitmapSourceFactory;
    private readonly IFileDialogService _fileDialogService;
    private readonly EditorSession _editorSession;
    private readonly ILogger<WorkspaceShellViewModel> _logger;
    private readonly SemaphoreSlim _workspaceSettingsPersistenceGate = new(1, 1);
    private readonly PreviewRefreshCoordinator _previewRefreshCoordinator = new(TimeSpan.FromMilliseconds(250));
    private CancellationTokenSource? _activeOperationCts;
    private PixelCoordinate? _selectedSourceCoordinate;
    private PixelCoordinate? _dragAnchor;
    private PixelAreaSelection? _selectedArea;
    private bool _isDraggingSourceArea;
    private bool _isSynchronizingSelectedDirection;
    private SpriteImage? _baseImage;
    private SpriteImage? _landmarkImage;
    private SpriteImage? _overlayImage;
    private SpriteImage? _compositeImage;

    public WorkspaceShellViewModel(
        StartEmptyWorkspaceUseCase startEmptyWorkspaceUseCase,
        CreateConfigUseCase createConfigUseCase,
        SaveConfigUseCase saveConfigUseCase,
        LoadConfigUseCase loadConfigUseCase,
        ImportLegacyCsvConfigUseCase importLegacyCsvConfigUseCase,
        LoadDmiFileUseCase loadDmiFileUseCase,
        BuildPreviewUseCase buildPreviewUseCase,
        ApplyConfigToDmiBatchUseCase applyConfigToDmiBatchUseCase,
        UndoChangeUseCase undoChangeUseCase,
        RedoChangeUseCase redoChangeUseCase,
        SetPreviewSelectionUseCase setPreviewSelectionUseCase,
        SetSelectedDirectionUseCase setSelectedDirectionUseCase,
        ApplyConfigTransformUseCase applyConfigTransformUseCase,
        LoadWorkspaceSettingsUseCase loadWorkspaceSettingsUseCase,
        SaveWorkspaceSettingsUseCase saveWorkspaceSettingsUseCase,
        SpriteImageBitmapSourceFactory bitmapSourceFactory,
        IFileDialogService fileDialogService,
        EditorSession editorSession,
        ILogger<WorkspaceShellViewModel> logger)
    {
        _startEmptyWorkspaceUseCase = startEmptyWorkspaceUseCase;
        _createConfigUseCase = createConfigUseCase;
        _saveConfigUseCase = saveConfigUseCase;
        _loadConfigUseCase = loadConfigUseCase;
        _importLegacyCsvConfigUseCase = importLegacyCsvConfigUseCase;
        _loadDmiFileUseCase = loadDmiFileUseCase;
        _buildPreviewUseCase = buildPreviewUseCase;
        _applyConfigToDmiBatchUseCase = applyConfigToDmiBatchUseCase;
        _undoChangeUseCase = undoChangeUseCase;
        _redoChangeUseCase = redoChangeUseCase;
        _setPreviewSelectionUseCase = setPreviewSelectionUseCase;
        _setSelectedDirectionUseCase = setSelectedDirectionUseCase;
        _applyConfigTransformUseCase = applyConfigTransformUseCase;
        _loadWorkspaceSettingsUseCase = loadWorkspaceSettingsUseCase;
        _saveWorkspaceSettingsUseCase = saveWorkspaceSettingsUseCase;
        _bitmapSourceFactory = bitmapSourceFactory;
        _fileDialogService = fileDialogService;
        _editorSession = editorSession;
        _logger = logger;

        AvailableDirections = [];
        AvailableStates = [];
        SourceRows = [];
        TargetRows = [];
        PreviewGridRows = [];
        MappingRows = [];
        BatchResults = [];
        DirectionTiles = [];
        ConfigQueueItems = [];
        SampleConfigItems = [];
        BatchStateStripItems = [];
        BatchSourceTreeItems = [];
        PreviewDisplayModes = Enum.GetValues<PreviewDisplayMode>();
        OverwritePolicies = Enum.GetValues<OverwritePolicy>();
        DirectionScopes = Enum.GetValues<DirectionScope>();

        NavigationRail = new NavigationRailViewModel(this);
        StartTab = new StartTabViewModel(this);
        EditorWorkspace = new EditorWorkspaceViewModel(this);
        ConfigWorkspace = new ConfigWorkspaceViewModel(this);
        BottomWorkspace = new BottomWorkspaceViewModel(this);
        PreviewPanel = new PreviewPanelViewModel(this);
        BatchWorkspace = new BatchWorkspaceViewModel(this);
        SettingsTab = new SettingsTabViewModel(this);
        OperationalStatusBar = new OperationalStatusBarViewModel(this);

        NavigationRail.Attach();
        StartTab.Attach();
        EditorWorkspace.Attach();
        ConfigWorkspace.Attach();
        BottomWorkspace.Attach();
        PreviewPanel.Attach();
        BatchWorkspace.Attach();
        SettingsTab.Attach();
        OperationalStatusBar.Attach();
    }

    public string WindowTitle => WorkspaceTitle == "Empty workspace"
        ? "Adaptive Sprites DMI Tool"
        : $"Adaptive Sprites DMI Tool - {WorkspaceTitle}";

    public ObservableCollection<SpriteDirection> AvailableDirections { get; }

    public ObservableCollection<string> AvailableStates { get; }

    public ObservableCollection<PixelRowViewModel> SourceRows { get; }

    public ObservableCollection<PixelRowViewModel> TargetRows { get; }

    public ObservableCollection<PixelRowViewModel> PreviewGridRows { get; }

    public ObservableCollection<MappingRowViewModel> MappingRows { get; }

    public ObservableCollection<BatchResultRowViewModel> BatchResults { get; }

    public ObservableCollection<DirectionTileViewModel> DirectionTiles { get; }

    public ObservableCollection<ConfigQueueItemViewModel> ConfigQueueItems { get; }

    public ObservableCollection<SampleConfigItemViewModel> SampleConfigItems { get; }

    public ObservableCollection<BatchStateStripItemViewModel> BatchStateStripItems { get; }

    public ObservableCollection<BatchSourceTreeItemViewModel> BatchSourceTreeItems { get; }

    public IReadOnlyList<PreviewDisplayMode> PreviewDisplayModes { get; }

    public IReadOnlyList<OverwritePolicy> OverwritePolicies { get; }

    public IReadOnlyList<DirectionScope> DirectionScopes { get; }

    public NavigationRailViewModel NavigationRail { get; }

    public StartTabViewModel StartTab { get; }

    public EditorWorkspaceViewModel EditorWorkspace { get; }

    public ConfigWorkspaceViewModel ConfigWorkspace { get; }

    public BottomWorkspaceViewModel BottomWorkspace { get; }

    public PreviewPanelViewModel PreviewPanel { get; }

    public BatchWorkspaceViewModel BatchWorkspace { get; }

    public SettingsTabViewModel SettingsTab { get; }

    public OperationalStatusBarViewModel OperationalStatusBar { get; }

    public void Dispose()
    {
        NavigationRail.Detach();
        StartTab.Detach();
        EditorWorkspace.Detach();
        ConfigWorkspace.Detach();
        BottomWorkspace.Detach();
        PreviewPanel.Detach();
        BatchWorkspace.Detach();
        SettingsTab.Detach();
        OperationalStatusBar.Detach();
        _previewRefreshCoordinator.Dispose();
        _workspaceSettingsPersistenceGate.Dispose();
        GC.SuppressFinalize(this);
    }
}
