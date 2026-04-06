using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using System.Collections.ObjectModel;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class WorkspaceShellViewModel
{
    private void RefreshMappingRows()
    {
        MappingRows.Clear();
        if (_editorSession.CurrentConfig is null)
        {
            return;
        }

        var direction = GetSafeSelectedDirection();

        foreach (var mapping in _editorSession.CurrentConfig.GetMappings(direction)
                     .OrderBy(static mapping => mapping.Source.Y)
                     .ThenBy(static mapping => mapping.Source.X))
        {
            MappingRows.Add(new MappingRowViewModel(mapping));
        }

        SelectedSourceSummary = _selectedSourceCoordinate is { } source
            ? $"Source pixel {source} selected."
            : "No source pixel selected.";
        SelectedAreaSummary = _selectedArea is { } area
            ? DescribeArea(area)
            : "No area selected.";
    }

    private void RebuildPixelRows(ObservableCollection<PixelRowViewModel> targetRows, bool useCompositeImage)
    {
        targetRows.Clear();
        var resolution = _editorSession.CurrentConfig?.Resolution ?? _editorSession.LoadedAsset?.Resolution;
        if (resolution is null)
        {
            return;
        }

        var referenceImage = useCompositeImage ? _compositeImage ?? _baseImage : _baseImage;
        var direction = GetSafeSelectedDirection();
        var mappings = _editorSession.CurrentConfig?.GetMappings(direction).ToDictionary(static mapping => mapping.Source) ?? [];
        var selectedTarget = _selectedSourceCoordinate is { } source && mappings.TryGetValue(source, out var mapping)
            ? mapping.Target
            : null;

        for (var y = 0; y < resolution.Value.Height; y++)
        {
            var cells = new List<PixelCellViewModel>(resolution.Value.Width);
            for (var x = 0; x < resolution.Value.Width; x++)
            {
                var coordinate = new PixelCoordinate(x, y);
                var hasMapping = mappings.TryGetValue(coordinate, out var pixelMapping);
                var cell = new PixelCellViewModel(x, y)
                {
                    Fill = CreateCellFill(referenceImage, coordinate, hasMapping, pixelMapping),
                    Border = CreateCellBorder(coordinate, hasMapping, selectedTarget),
                    Foreground = hasMapping && pixelMapping.IsTransparent ? Brushes.Black : Brushes.Transparent,
                    Caption = BuildCellCaption(coordinate, hasMapping, pixelMapping, selectedTarget),
                    ToolTip = BuildCellToolTip(coordinate, hasMapping, pixelMapping)
                };

                cells.Add(cell);
            }

            targetRows.Add(new PixelRowViewModel(cells));
        }
    }

    private void RebuildPreviewGridRows()
    {
        PreviewGridRows.Clear();
        var resolution = _editorSession.CurrentConfig?.Resolution;
        if (resolution is null || _editorSession.CurrentConfig is null)
        {
            PreviewTextGrid = "No config grid is available yet.";
            return;
        }

        var textRows = new List<string>(resolution.Value.Height);
        var direction = GetSafeSelectedDirection();
        for (var y = 0; y < resolution.Value.Height; y++)
        {
            var cells = new List<PixelCellViewModel>(resolution.Value.Width);
            var rowText = new char[resolution.Value.Width];
            for (var x = 0; x < resolution.Value.Width; x++)
            {
                var coordinate = new PixelCoordinate(x, y);
                var isTransparent = _editorSession.CurrentConfig.IsTransparent(direction, coordinate);
                var effectiveTarget = _editorSession.CurrentConfig.GetEffectiveTarget(direction, coordinate);
                var isMoved = effectiveTarget != coordinate;
                rowText[x] = isTransparent ? 'T' : isMoved ? 'M' : '.';

                cells.Add(new PixelCellViewModel(x, y)
                {
                    Fill = isTransparent ? TransparentBrush : isMoved ? MappedBrush : NeutralBrush,
                    Border = ShowGrid ? GridBrush : Brushes.Transparent,
                    Foreground = Brushes.Transparent,
                    Caption = GridAboveImage ? rowText[x].ToString() : string.Empty,
                    ToolTip = $"{coordinate} -> {(isTransparent ? "transparent" : effectiveTarget.ToString())}"
                });
            }

            textRows.Add(new string(rowText));
            PreviewGridRows.Add(new PixelRowViewModel(cells));
        }

        PreviewTextGrid = string.Join(Environment.NewLine, textRows);
    }

    private void RefreshActivePreviewPresentation()
    {
        var selectedImage = SelectedPreviewDisplayMode switch
        {
            PreviewDisplayMode.Base => _baseImage,
            PreviewDisplayMode.Landmark => _landmarkImage,
            PreviewDisplayMode.Overlay => _overlayImage,
            PreviewDisplayMode.Composite => ShowOverlay ? _compositeImage ?? _baseImage : _baseImage,
            _ => null
        };

        CurrentPreviewImage = selectedImage is null ? null : _bitmapSourceFactory.Create(selectedImage);
        IsPreviewImageVisible = SelectedPreviewDisplayMode is PreviewDisplayMode.Base
            or PreviewDisplayMode.Landmark
            or PreviewDisplayMode.Overlay
            or PreviewDisplayMode.Composite;
        IsPreviewGridVisible = SelectedPreviewDisplayMode == PreviewDisplayMode.Grid;
        IsPreviewTextVisible = SelectedPreviewDisplayMode == PreviewDisplayMode.TextGrid;
    }

    private void ClearPreviewArtifacts()
    {
        _baseImage = null;
        _landmarkImage = null;
        _overlayImage = null;
        _compositeImage = null;
        CurrentPreviewImage = null;
        PreviewSummary = "Build a preview to render the selected base, landmark, and overlay states.";
        PreviewTextGrid = "No config grid is available yet.";
    }

    private void RefreshCommandStates()
    {
        CreateConfigCommand.NotifyCanExecuteChanged();
        SaveConfigCommand.NotifyCanExecuteChanged();
        BuildPreviewCommand.NotifyCanExecuteChanged();
        RunBatchCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        ContinueToEditorCommand.NotifyCanExecuteChanged();
        ResumeLastWorkspaceCommand.NotifyCanExecuteChanged();
        OpenRecentDmiCommand.NotifyCanExecuteChanged();
        OpenRecentConfigCommand.NotifyCanExecuteChanged();
        ImportRecentLegacyCsvCommand.NotifyCanExecuteChanged();
    }

    private Brush CreateCellFill(SpriteImage? image, PixelCoordinate coordinate, bool hasMapping, PixelMapping mapping)
    {
        if (hasMapping && mapping.IsTransparent)
        {
            return TransparentBrush;
        }

        if (_selectedSourceCoordinate == coordinate)
        {
            return SelectedBrush;
        }

        if (_selectedArea?.Contains(coordinate) == true)
        {
            return AreaBrush;
        }

        if (hasMapping && mapping.Target is not null)
        {
            return MappedBrush;
        }

        return TryReadPixelColor(image, coordinate, out var color)
            ? CreateBrush(color)
            : NeutralBrush;
    }

    private Brush CreateCellBorder(PixelCoordinate coordinate, bool hasMapping, PixelCoordinate? selectedTarget)
    {
        if (!ShowGrid)
        {
            return Brushes.Transparent;
        }

        if (selectedTarget == coordinate || _selectedArea?.Contains(coordinate) == true)
        {
            return SelectedBrush;
        }

        return GridBrush;
    }

    private string BuildCellCaption(PixelCoordinate coordinate, bool hasMapping, PixelMapping mapping, PixelCoordinate? selectedTarget)
    {
        if (_selectedSourceCoordinate == coordinate)
        {
            return "S";
        }

        if (selectedTarget == coordinate)
        {
            return "T";
        }

        if (_selectedArea?.Contains(coordinate) == true)
        {
            return "A";
        }

        if (!GridAboveImage)
        {
            return string.Empty;
        }

        return !hasMapping ? string.Empty : mapping.IsTransparent ? "X" : mapping.Target is not null ? "M" : string.Empty;
    }

    private static string BuildCellToolTip(PixelCoordinate coordinate, bool hasMapping, PixelMapping mapping) =>
        !hasMapping
            ? $"{coordinate} -> unchanged"
            : mapping.Target is null
            ? $"{coordinate} -> transparent"
            : mapping.Target == coordinate
                ? $"{coordinate} -> unchanged"
                : $"{coordinate} -> {mapping.Target}";

    private static bool TryReadPixelColor(SpriteImage? image, PixelCoordinate coordinate, out Color color)
    {
        if (image is null || coordinate.X >= image.Width || coordinate.Y >= image.Height)
        {
            color = default;
            return false;
        }

        var index = ((coordinate.Y * image.Width) + coordinate.X) * 4;
        color = Color.FromArgb(
            image.RgbaBytes[index + 3],
            image.RgbaBytes[index],
            image.RgbaBytes[index + 1],
            image.RgbaBytes[index + 2]);
        return true;
    }

    private static Brush CreateBrush(Color color)
    {
        var brush = new System.Windows.Media.SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static string DescribeArea(PixelAreaSelection area) =>
        $"Area {area.Left},{area.Top} to {area.Right},{area.Bottom} ({area.Width}x{area.Height}).";

    private static string NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();

    private static string? NormalizeOptionalPath(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private readonly record struct PixelAreaSelection(PixelCoordinate Start, PixelCoordinate End)
    {
        public int Left => Math.Min(Start.X, End.X);

        public int Top => Math.Min(Start.Y, End.Y);

        public int Right => Math.Max(Start.X, End.X);

        public int Bottom => Math.Max(Start.Y, End.Y);

        public int Width => Right - Left + 1;

        public int Height => Bottom - Top + 1;

        public bool Contains(PixelCoordinate coordinate) =>
            coordinate.X >= Left && coordinate.X <= Right && coordinate.Y >= Top && coordinate.Y <= Bottom;

        public IEnumerable<PixelCoordinate> Enumerate()
        {
            for (var y = Top; y <= Bottom; y++)
            {
                for (var x = Left; x <= Right; x++)
                {
                    yield return new PixelCoordinate(x, y);
                }
            }
        }
    }
}
