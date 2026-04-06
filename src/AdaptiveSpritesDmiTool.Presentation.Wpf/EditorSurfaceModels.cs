using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
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
    Assets = 0,
    Configs = 1,
    Mappings = 2,
    BatchResults = 3
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
    public PixelCoordinate Source { get; } = mapping.Source;

    public PixelCoordinate? Target { get; } = mapping.Target;

    public bool IsTransparent => mapping.IsTransparent;

    public string SourceText => mapping.Source.ToString();

    public string TargetText => mapping.Target?.ToString() ?? "transparent";
}

public sealed class BatchResultRowViewModel(BatchFileResult result)
{
    public BatchFileStatus Status { get; } = result.Status;

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
        string glyph,
        Func<bool> isAvailable)
    {
        _shell = shell;
        Section = section;
        Label = label;
        Glyph = glyph;
        _isAvailable = isAvailable;
    }

    public ShellSectionKind Section { get; }

    public string Label { get; }

    public string Glyph { get; }

    public bool IsAvailable => _isAvailable();

    public bool IsSelected => _shell.SelectedShellSection == Section;

    public void Refresh()
    {
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(IsSelected));
    }
}

public sealed class EditorCommandBarOptions
{
    public static IReadOnlyList<EditorTool> EditorTools { get; } = Enum.GetValues<EditorTool>();

    public static IReadOnlyList<EditorViewportMode> ViewportModes { get; } = Enum.GetValues<EditorViewportMode>();
}

public sealed record ConfigQueueItemViewModel(string Name, string PathSummary, bool IsActive);

public sealed record BatchStateStripItemViewModel(string Name);

public sealed class BatchSourceTreeItemViewModel(
    string name,
    string fullPath,
    bool isDirectory,
    IEnumerable<BatchSourceTreeItemViewModel>? children = null)
{
    public string Name { get; } = name;

    public string FullPath { get; } = fullPath;

    public bool IsDirectory { get; } = isDirectory;

    public ObservableCollection<BatchSourceTreeItemViewModel> Children { get; } = children is null
        ? []
        : new ObservableCollection<BatchSourceTreeItemViewModel>(children);
}
