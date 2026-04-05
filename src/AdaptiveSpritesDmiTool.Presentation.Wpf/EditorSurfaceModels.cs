using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.ComponentModel;
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

public sealed partial class PixelCellViewModel(int x, int y) : ObservableObject
{
    public PixelCoordinate Coordinate { get; } = new(x, y);

    public string CoordinateText => $"{x},{y}";

    [ObservableProperty]
    private Brush fill = Brushes.WhiteSmoke;

    [ObservableProperty]
    private Brush border = Brushes.Transparent;

    [ObservableProperty]
    private Brush foreground = Brushes.Black;

    [ObservableProperty]
    private string caption = string.Empty;

    [ObservableProperty]
    private string toolTip = $"{x},{y}";
}

public sealed class PixelRowViewModel(IEnumerable<PixelCellViewModel> cells)
{
    public IReadOnlyList<PixelCellViewModel> Cells { get; } = cells.ToArray();
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
