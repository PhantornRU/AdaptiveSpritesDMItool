using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public enum EditorTool
{
    Single = 0,
    Fill = 1,
    Delete = 2,
    Undo = 3,
    UndoArea = 4,
    Select = 5,
    Move = 6
}

public enum DirectionScope
{
    Single = 0,
    Parallel = 1,
    All = 2
}

public enum PreviewDisplayMode
{
    Composite = 0,
    Base = 1,
    Landmark = 2,
    Overlay = 3,
    Grid = 4,
    TextGrid = 5
}

public enum EditorViewportMode
{
    Matrix = 0,
    Focused = 1
}

public enum BottomWorkspaceTab
{
    Mappings = 0,
    Advanced = 1
}

public enum ImportedStatePlacementMode
{
    None = 0,
    Background = 1,
    Overlay = 2
}

public sealed partial class PixelCellViewModel : ObservableObject
{
    public PixelCellViewModel(SpriteDirection direction, int x, int y)
    {
        Direction = direction;
        Coordinate = new PixelCoordinate(x, y);
        CoordinateText = $"{x},{y}";
        ToolTip = $"{x},{y}";
    }

    public SpriteDirection Direction { get; }

    public PixelCoordinate Coordinate { get; }

    public string CoordinateText { get; }

    [ObservableProperty]
    private Brush fill = Brushes.WhiteSmoke;

    [ObservableProperty]
    private Brush border = Brushes.Transparent;

    [ObservableProperty]
    private Brush foreground = Brushes.Black;

    [ObservableProperty]
    private string caption = string.Empty;

    [ObservableProperty]
    private string toolTip = string.Empty;
}

public sealed class PixelRowViewModel(IEnumerable<PixelCellViewModel> cells)
{
    public IReadOnlyList<PixelCellViewModel> Cells { get; } = cells.ToArray();
}

public sealed partial class DirectionTileViewModel : ObservableObject
{
    public DirectionTileViewModel(SpriteDirection direction)
    {
        Direction = direction;
        Label = direction.ToString();
    }

    public SpriteDirection Direction { get; }

    public string Label { get; }

    public ObservableCollection<PixelRowViewModel> SourceRows { get; } = [];

    public ObservableCollection<PixelRowViewModel> TargetRows { get; } = [];

    [ObservableProperty]
    private bool isActive;
}

public sealed class MappingRowViewModel(PixelMapping mapping)
{
    public PixelCoordinate Editable { get; } = mapping.Source;

    public PixelCoordinate? Source { get; } = mapping.Target;

    public bool IsTransparent => mapping.Target is null;

    public string EditableText => mapping.Source.ToString();

    public string SourceText => mapping.Target?.ToString() ?? "transparent";
}

public sealed class BatchResultRowViewModel(BatchFileResult result)
{
    public BatchFileStatus Status { get; } = result.Status;

    public string StatusText => Status.ToString();

    public string InputPath { get; } = result.InputPath;

    public string OutputPath { get; } = result.OutputPath ?? string.Empty;

    public string Message { get; } = result.Message;
}

public sealed class NavigationRailItemViewModel : ObservableObject
{
    private readonly WorkspaceShellViewModel _shell;
    private readonly Func<bool> _isAvailable;

    public NavigationRailItemViewModel(
        WorkspaceShellViewModel shell,
        ShellSectionKind section,
        string label,
        string iconSymbol,
        Func<bool> isAvailable)
    {
        _shell = shell;
        Section = section;
        LabelFallback = label;
        IconSymbol = iconSymbol;
        _isAvailable = isAvailable;
    }

    public ShellSectionKind Section { get; }

    public string Label => Section switch
    {
        ShellSectionKind.Start => App.Text("Text.Tab.Start", LabelFallback),
        ShellSectionKind.Editor => App.Text("Text.Tab.Editor", LabelFallback),
        ShellSectionKind.Batch => App.Text("Text.Tab.Data", LabelFallback),
        ShellSectionKind.Settings => App.Text("Text.Tab.Settings", LabelFallback),
        _ => LabelFallback
    };

    private string LabelFallback { get; }

    public string IconSymbol { get; }

    public bool IsAvailable => _isAvailable();

    public bool IsSelected => _shell.SelectedShellSection == Section;

    public void Refresh()
    {
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(Label));
    }
}

public sealed class EditorCommandBarOptions
{
    public static IReadOnlyList<EditorTool> EditorTools { get; } = Enum.GetValues<EditorTool>();

    public static IReadOnlyList<EditorViewportMode> ViewportModes { get; } = Enum.GetValues<EditorViewportMode>();
}

public sealed partial class ConfigQueueItemViewModel : ObservableObject
{
    public ConfigQueueItemViewModel(
        Guid id,
        string name,
        string pathSummary,
        string? configPath,
        SpriteConfig configSnapshot,
        bool isActive)
    {
        Id = id;
        this.name = name;
        this.pathSummary = pathSummary;
        this.configPath = configPath;
        this.configSnapshot = configSnapshot;
        this.isActive = isActive;
    }

    public Guid Id { get; }

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string pathSummary;

    [ObservableProperty]
    private string? configPath;

    [ObservableProperty]
    private SpriteConfig configSnapshot;

    [ObservableProperty]
    private bool isActive;
}

public sealed record SampleConfigItemViewModel(
    string Name,
    string Path,
    string KindLabel,
    string PathSummary,
    bool IsLegacyCsv,
    bool IsActive);

public sealed record BatchStateStripItemViewModel(string Name);

public sealed partial class EditorAssetItemViewModel : ObservableObject
{
    public EditorAssetItemViewModel(
        string name,
        string kindLabel,
        string sourceSummary,
        bool isActive,
        EditorAssetTargetSurface targetSurface,
        EditorAssetTargetLayer targetLayer)
    {
        Name = name;
        KindLabel = kindLabel;
        SourceSummary = sourceSummary;
        this.isActive = isActive;
        this.targetSurface = targetSurface;
        this.targetLayer = targetLayer;
    }

    public string Name { get; }

    public string KindLabel { get; }

    public string SourceSummary { get; }

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private EditorAssetTargetSurface targetSurface;

    [ObservableProperty]
    private EditorAssetTargetLayer targetLayer;
}

public sealed partial class ImportedDmiStateItemViewModel : ObservableObject
{
    private string orderText = string.Empty;

    public ImportedDmiStateItemViewModel(
        string stateName,
        string sourcePath,
        string sourceFileLabel,
        BitmapSource? previewImage,
        ImportedStatePlacementMode placementMode,
        int order)
    {
        StateName = stateName;
        this.sourcePath = sourcePath;
        this.sourceFileLabel = sourceFileLabel;
        this.previewImage = previewImage;
        this.placementMode = placementMode;
        this.order = order;
        orderText = order.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public string StateName { get; }

    [ObservableProperty]
    private string sourcePath;

    [ObservableProperty]
    private string sourceFileLabel;

    [ObservableProperty]
    private BitmapSource? previewImage;

    [ObservableProperty]
    private bool isValid = true;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private ImportedStatePlacementMode placementMode;

    [NotifyPropertyChangedFor(nameof(OrderText))]
    [ObservableProperty]
    private int order;

    public string OrderText
    {
        get => orderText;
        set
        {
            if (!SetProperty(ref orderText, value))
            {
                return;
            }

            if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                parsed = Math.Max(0, parsed);
                if (parsed != Order)
                {
                    Order = parsed;
                }
            }
        }
    }

    public bool IsBackgroundAssigned => PlacementMode == ImportedStatePlacementMode.Background;

    public bool IsOverlayAssigned => PlacementMode == ImportedStatePlacementMode.Overlay;

    partial void OnOrderChanged(int value)
    {
        var normalized = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(orderText, normalized, StringComparison.Ordinal))
        {
            orderText = normalized;
            OnPropertyChanged(nameof(OrderText));
        }
    }

    partial void OnPlacementModeChanged(ImportedStatePlacementMode value)
    {
        OnPropertyChanged(nameof(IsBackgroundAssigned));
        OnPropertyChanged(nameof(IsOverlayAssigned));
    }
}

public sealed partial class BatchSourceTreeItemViewModel : ObservableObject
{
    public BatchSourceTreeItemViewModel(
        string name,
        string fullPath,
        bool isDirectory,
        IEnumerable<BatchSourceTreeItemViewModel>? children = null)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        Children = children is null
            ? []
            : new ObservableCollection<BatchSourceTreeItemViewModel>(children);
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public ObservableCollection<BatchSourceTreeItemViewModel> Children { get; }

    [ObservableProperty]
    private bool isValid = true;

    [ObservableProperty]
    private string validationMessage = string.Empty;
}
