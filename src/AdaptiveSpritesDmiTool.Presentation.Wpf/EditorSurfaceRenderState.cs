using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using Color = System.Windows.Media.Color;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public readonly record struct PixelAreaBounds(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left + 1;

    public int Height => Bottom - Top + 1;

    public bool Contains(PixelCoordinate coordinate) =>
        coordinate.X >= Left && coordinate.X <= Right && coordinate.Y >= Top && coordinate.Y <= Bottom;
}

public sealed class EditorSurfaceRenderState(
    SpriteDirection direction,
    int width,
    int height,
    Color[] fillColors,
    string[] captions,
    IReadOnlyDictionary<PixelCoordinate, PixelCoordinate?>? editableBackingOrigins = null)
{
    public const double BaseCellSize = 5d;

    public SpriteDirection Direction { get; } = direction;

    public int Width { get; } = width;

    public int Height { get; } = height;

    public IReadOnlyList<Color> FillColors { get; } = fillColors;

    public IReadOnlyList<string> Captions { get; } = captions;

    public IReadOnlyDictionary<PixelCoordinate, PixelCoordinate?> EditableBackingOrigins { get; } =
        editableBackingOrigins ?? new Dictionary<PixelCoordinate, PixelCoordinate?>();

    public int GetIndex(int x, int y) => (y * Width) + x;
}
