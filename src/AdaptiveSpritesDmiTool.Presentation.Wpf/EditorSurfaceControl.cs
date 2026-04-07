using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using Size = System.Windows.Size;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Typeface = System.Windows.Media.Typeface;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public sealed class EditorSurfaceControl : FrameworkElement
{
    private static readonly Dictionary<uint, SolidColorBrush> BrushCache = [];
    private static readonly Brush SelectionAreaBrush = CreateBrush(Color.FromArgb(72, 55, 122, 246));
    private static readonly Brush SelectedSourceBrush = CreateBrush(Color.FromArgb(112, 30, 92, 84));
    private static readonly Brush HoverBrush = CreateBrush(Color.FromArgb(72, 255, 193, 7));
    private static readonly Pen SelectionPen = CreatePen(Color.FromRgb(30, 92, 84), 1.4d);
    private static readonly Pen HoverPen = CreatePen(Color.FromRgb(196, 137, 16), 1d);
    private static readonly Pen GridPen = CreatePen(Color.FromRgb(217, 207, 192), 0.5d);
    private static readonly Typeface CaptionTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private DrawingGroup? _contentDrawing;
    private bool _isContentDirty = true;

#if DEBUG
    private static int _contentRedrawCount;
    private static int _overlayRenderCount;
#endif

    public static readonly DependencyProperty SurfaceProperty = DependencyProperty.Register(
        nameof(Surface),
        typeof(EditorSurfaceRenderState),
        typeof(EditorSurfaceControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnContentInputChanged));

    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom),
        typeof(double),
        typeof(EditorSurfaceControl),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnContentInputChanged));

    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid),
        typeof(bool),
        typeof(EditorSurfaceControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnContentInputChanged));

    public static readonly DependencyProperty ShowCaptionsProperty = DependencyProperty.Register(
        nameof(ShowCaptions),
        typeof(bool),
        typeof(EditorSurfaceControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnContentInputChanged));

    public static readonly DependencyProperty SelectedSourceCoordinateProperty = DependencyProperty.Register(
        nameof(SelectedSourceCoordinate),
        typeof(PixelCoordinate?),
        typeof(EditorSurfaceControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectedTargetCoordinateProperty = DependencyProperty.Register(
        nameof(SelectedTargetCoordinate),
        typeof(PixelCoordinate?),
        typeof(EditorSurfaceControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HoveredCoordinateProperty = DependencyProperty.Register(
        nameof(HoveredCoordinate),
        typeof(PixelCoordinate?),
        typeof(EditorSurfaceControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectedAreaBoundsProperty = DependencyProperty.Register(
        nameof(SelectedAreaBounds),
        typeof(PixelAreaBounds?),
        typeof(EditorSurfaceControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public EditorSurfaceRenderState? Surface
    {
        get => (EditorSurfaceRenderState?)GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public bool ShowCaptions
    {
        get => (bool)GetValue(ShowCaptionsProperty);
        set => SetValue(ShowCaptionsProperty, value);
    }

    public PixelCoordinate? SelectedSourceCoordinate
    {
        get => (PixelCoordinate?)GetValue(SelectedSourceCoordinateProperty);
        set => SetValue(SelectedSourceCoordinateProperty, value);
    }

    public PixelCoordinate? SelectedTargetCoordinate
    {
        get => (PixelCoordinate?)GetValue(SelectedTargetCoordinateProperty);
        set => SetValue(SelectedTargetCoordinateProperty, value);
    }

    public PixelCoordinate? HoveredCoordinate
    {
        get => (PixelCoordinate?)GetValue(HoveredCoordinateProperty);
        set => SetValue(HoveredCoordinateProperty, value);
    }

    public PixelAreaBounds? SelectedAreaBounds
    {
        get => (PixelAreaBounds?)GetValue(SelectedAreaBoundsProperty);
        set => SetValue(SelectedAreaBoundsProperty, value);
    }

    public bool TryGetCoordinate(Point point, out PixelCoordinate coordinate)
    {
        if (Surface is null)
        {
            coordinate = default;
            return false;
        }

        var cellSize = GetCellSize();
        var x = (int)Math.Floor(point.X / cellSize);
        var y = (int)Math.Floor(point.Y / cellSize);
        if (x < 0 || y < 0 || x >= Surface.Width || y >= Surface.Height)
        {
            coordinate = default;
            return false;
        }

        coordinate = new PixelCoordinate(x, y);
        return true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Surface is null)
        {
            return new Size(0, 0);
        }

        var cellSize = GetCellSize();
        return new Size(Surface.Width * cellSize, Surface.Height * cellSize);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (Surface is null)
        {
            return;
        }

        EnsureContentDrawing();
        var cellSize = GetCellSize();
        drawingContext.DrawDrawing(_contentDrawing);

#if DEBUG
        var overlayRenderCount = Interlocked.Increment(ref _overlayRenderCount);
        Debug.WriteLine($"[EditorSurfaceControl] overlay render #{overlayRenderCount} for {Surface.Direction} {Surface.Width}x{Surface.Height}");
#endif

        if (SelectedAreaBounds is { } area)
        {
            var areaRect = new Rect(area.Left * cellSize, area.Top * cellSize, area.Width * cellSize, area.Height * cellSize);
            drawingContext.DrawRectangle(SelectionAreaBrush, SelectionPen, areaRect);
        }

        if (SelectedSourceCoordinate is { } selectedSource)
        {
            var rect = new Rect(selectedSource.X * cellSize, selectedSource.Y * cellSize, cellSize, cellSize);
            drawingContext.DrawRectangle(SelectedSourceBrush, SelectionPen, rect);
            if (ShowCaptions)
            {
                DrawCaption(drawingContext, "S", rect);
            }
        }

        if (SelectedTargetCoordinate is { } selectedTarget)
        {
            var rect = new Rect(selectedTarget.X * cellSize, selectedTarget.Y * cellSize, cellSize, cellSize);
            drawingContext.DrawRectangle(null, SelectionPen, rect);
            if (ShowCaptions)
            {
                DrawCaption(drawingContext, "T", rect);
            }
        }

        if (HoveredCoordinate is { } hovered)
        {
            var rect = new Rect(hovered.X * cellSize, hovered.Y * cellSize, cellSize, cellSize);
            drawingContext.DrawRectangle(HoverBrush, HoverPen, rect);
        }

    }

    public void InvalidateOverlayOnly() => InvalidateVisual();

    private static void OnContentInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EditorSurfaceControl control)
        {
            control._isContentDirty = true;
        }
    }

    private void EnsureContentDrawing()
    {
        if (!_isContentDirty && _contentDrawing is not null)
        {
            return;
        }

        _contentDrawing = BuildContentDrawing();
        _isContentDirty = false;

#if DEBUG
        var contentRedrawCount = Interlocked.Increment(ref _contentRedrawCount);
        if (Surface is not null)
        {
            Debug.WriteLine($"[EditorSurfaceControl] content redraw #{contentRedrawCount} for {Surface.Direction} {Surface.Width}x{Surface.Height}");
        }
#endif
    }

    private DrawingGroup BuildContentDrawing()
    {
        var drawing = new DrawingGroup();
        if (Surface is null)
        {
            return drawing;
        }

        var cellSize = GetCellSize();
        using (var context = drawing.Open())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Surface.Width * cellSize, Surface.Height * cellSize));

            for (var y = 0; y < Surface.Height; y++)
            {
                for (var x = 0; x < Surface.Width; x++)
                {
                    var index = Surface.GetIndex(x, y);
                    var rect = new Rect(x * cellSize, y * cellSize, cellSize, cellSize);
                    context.DrawRectangle(GetBrush(Surface.FillColors[index]), null, rect);

                    if (ShowCaptions && !string.IsNullOrEmpty(Surface.Captions[index]))
                    {
                        DrawCaption(context, Surface.Captions[index], rect);
                    }
                }
            }

            if (ShowGrid)
            {
                for (var x = 0; x <= Surface.Width; x++)
                {
                    var offset = x * cellSize;
                    context.DrawLine(GridPen, new Point(offset, 0), new Point(offset, Surface.Height * cellSize));
                }

                for (var y = 0; y <= Surface.Height; y++)
                {
                    var offset = y * cellSize;
                    context.DrawLine(GridPen, new Point(0, offset), new Point(Surface.Width * cellSize, offset));
                }
            }
        }

        drawing.Freeze();
        return drawing;
    }

    private double GetCellSize() => Math.Max(1d, EditorSurfaceRenderState.BaseCellSize * Zoom);

    private void DrawCaption(DrawingContext drawingContext, string caption, Rect rect)
    {
        var formattedText = new FormattedText(
            caption,
            CultureInfo.InvariantCulture,
            System.Windows.FlowDirection.LeftToRight,
            CaptionTypeface,
            Math.Max(6, rect.Width * 0.45d),
            Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var origin = new Point(
            rect.Left + Math.Max(0, (rect.Width - formattedText.Width) / 2),
            rect.Top + Math.Max(0, (rect.Height - formattedText.Height) / 2));
        drawingContext.DrawText(formattedText, origin);
    }

    private static SolidColorBrush GetBrush(Color color)
    {
        var key = ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        if (BrushCache.TryGetValue(key, out var brush))
        {
            return brush;
        }

        brush = CreateBrush(color);
        BrushCache[key] = brush;
        return brush;
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Pen CreatePen(Color color, double thickness)
    {
        var pen = new Pen(CreateBrush(color), thickness);
        pen.Freeze();
        return pen;
    }
}
