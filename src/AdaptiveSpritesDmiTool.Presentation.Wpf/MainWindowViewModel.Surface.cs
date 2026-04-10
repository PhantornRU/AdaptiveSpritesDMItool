using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Microsoft.Extensions.Logging;
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
        => RebuildPixelRows(targetRows, GetSafeSelectedDirection(), useCompositeImage);

    private void RefreshInteractionState()
    {
        SelectedSourceCoordinateView = _selectedSourceCoordinate;
        SelectedAreaBounds = _selectedArea is { } area
            ? new PixelAreaBounds(area.Left, area.Top, area.Right, area.Bottom)
            : null;
        SelectedTargetCoordinate = ResolveSelectedTargetCoordinate();
        OppositeHighlightedCoordinate = SelectedTargetCoordinate;
    }

    private PixelCoordinate? ResolveSelectedTargetCoordinate()
    {
        if (_selectedSourceCoordinate is not { } source || _editorSession.CurrentConfig is null)
        {
            return null;
        }

        var direction = GetSafeSelectedDirection();
        foreach (var mapping in _editorSession.CurrentConfig.GetMappings(direction))
        {
            if (mapping.Source == source)
            {
                return mapping.Target;
            }
        }

        return null;
    }

    private void RebuildActiveSurfaceRenderStates()
    {
        var direction = GetSafeSelectedDirection();
        ActiveSourceSurface = BuildEditorSurfaceRenderState(direction, useCompositeImage: false);
        ActiveTargetSurface = BuildEditorSurfaceRenderState(direction, useCompositeImage: ShowOverlay);
    }

    private void RefreshImportedStateComposition()
    {
        InvalidateNavigatorSnapshotCache();
        RefreshEditorSurface();
        PersistWorkspaceSettingsInBackground();
    }

    private void RequestImportedStateCompositionRefresh(bool warmFrames)
    {
        _importedStateRefreshCts?.Cancel();
        _importedStateRefreshCts?.Dispose();
        _importedStateRefreshCts = null;

        if (!warmFrames)
        {
            RefreshImportedStateComposition();
            return;
        }

        var cancellationSource = new CancellationTokenSource();
        _importedStateRefreshCts = cancellationSource;
        var version = ++_importedStateRefreshVersion;
        _ = RefreshImportedStateCompositionAsync(version, cancellationSource.Token);
    }

    private async Task RefreshImportedStateCompositionAsync(int requestVersion, CancellationToken cancellationToken)
    {
        try
        {
            await PreloadImportedStateFramesAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested || requestVersion != _importedStateRefreshVersion)
            {
                return;
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    if (cancellationToken.IsCancellationRequested || requestVersion != _importedStateRefreshVersion)
                    {
                        return;
                    }

                    RefreshImportedStateComposition();
                },
                System.Windows.Threading.DispatcherPriority.Background,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to refresh imported state composition.");
            StatusMessage = $"Unexpected error: {exception.Message}";
        }
    }

    private async Task PreloadImportedStateFramesAsync(CancellationToken cancellationToken)
    {
        var activeLayers = ImportedDmiStateItems
            .Where(static item => item.PlacementMode != ImportedStatePlacementMode.None)
            .ToArray();
        if (activeLayers.Length == 0)
        {
            return;
        }

        var directions = (AvailableDirections.Count == 0 ? [GetSafeSelectedDirection()] : AvailableDirections.ToArray())
            .Distinct()
            .ToArray();

        foreach (var layer in activeLayers)
        {
            if (string.IsNullOrWhiteSpace(layer.SourcePath))
            {
                continue;
            }

            foreach (var direction in directions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cacheKey = (layer.SourcePath, layer.StateName, direction);
                if (_importedStateFrameCache.ContainsKey(cacheKey))
                {
                    continue;
                }

                var result = await _readStateFrameUseCase
                    .ExecuteAsync(layer.SourcePath, layer.StateName, direction, cancellationToken);
                _importedStateFrameCache[cacheKey] = result.IsSuccess ? result.Value : null;
            }
        }
    }

    private void AttachImportedStateItem(ImportedDmiStateItemViewModel item)
    {
        item.PropertyChanged -= OnImportedStateItemPropertyChanged;
        item.PropertyChanged += OnImportedStateItemPropertyChanged;
    }

    private void ClearImportedStateItems()
    {
        _importedStateRefreshCts?.Cancel();
        _importedStateRefreshCts?.Dispose();
        _importedStateRefreshCts = null;

        foreach (var item in ImportedDmiStateItems)
        {
            item.PropertyChanged -= OnImportedStateItemPropertyChanged;
        }

        ImportedDmiStateItems.Clear();
        InvalidateImportedStateFrameCache();
    }

    private void InvalidateImportedStateFrameCache() => _importedStateFrameCache.Clear();

    private void OnImportedStateItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImportedDmiStateItemViewModel.Order))
        {
            RefreshImportedStateComposition();
        }
    }

    private SpriteImage? ComposeImportedLayers(SpriteImage? baseImage, SpriteDirection direction)
    {
        var resolution = ResolveEditorResolution();
        if (resolution is null)
        {
            return baseImage;
        }

        var backgroundLayers = ImportedDmiStateItems
            .Where(static item => item.PlacementMode == ImportedStatePlacementMode.Background)
            .OrderBy(static item => item.Order)
            .ToArray();
        var overlayLayers = ImportedDmiStateItems
            .Where(static item => item.PlacementMode == ImportedStatePlacementMode.Overlay)
            .OrderBy(static item => item.Order)
            .ToArray();

        if (backgroundLayers.Length == 0 && overlayLayers.Length == 0)
        {
            return baseImage;
        }

        var composed = new SpriteImage(
            resolution.Value.Width,
            resolution.Value.Height,
            new byte[resolution.Value.Width * resolution.Value.Height * 4]);

        foreach (var layer in backgroundLayers)
        {
            var image = GetImportedStateFrame(layer, direction);
            if (image is not null)
            {
                BlendOver(composed, image);
            }
        }

        if (baseImage is not null)
        {
            BlendOver(composed, baseImage);
        }

        foreach (var layer in overlayLayers)
        {
            var image = GetImportedStateFrame(layer, direction);
            if (image is not null)
            {
                BlendOver(composed, image);
            }
        }

        return composed;
    }

    private SpriteImage? GetImportedStateFrame(ImportedDmiStateItemViewModel item, SpriteDirection direction)
    {
        if (string.IsNullOrWhiteSpace(item.SourcePath))
        {
            return null;
        }

        var cacheKey = (item.SourcePath, item.StateName, direction);
        if (_importedStateFrameCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return null;
    }

    private static void BlendOver(SpriteImage canvas, SpriteImage layer)
    {
        if (canvas.Width != layer.Width || canvas.Height != layer.Height)
        {
            return;
        }

        for (var index = 0; index < canvas.RgbaBytes.Length; index += 4)
        {
            var bgR = canvas.RgbaBytes[index];
            var bgG = canvas.RgbaBytes[index + 1];
            var bgB = canvas.RgbaBytes[index + 2];
            var bgA = canvas.RgbaBytes[index + 3];

            var fgR = layer.RgbaBytes[index];
            var fgG = layer.RgbaBytes[index + 1];
            var fgB = layer.RgbaBytes[index + 2];
            var fgA = layer.RgbaBytes[index + 3];

            var fgAlpha = fgA / 255f;
            if (fgAlpha <= 0f)
            {
                continue;
            }

            var bgAlpha = bgA / 255f;
            var outAlpha = fgAlpha + (bgAlpha * (1f - fgAlpha));
            if (outAlpha <= 0f)
            {
                continue;
            }

            static byte Channel(byte bg, byte fg, float bgAlpha, float fgAlpha, float outAlpha)
            {
                var value = ((fg * fgAlpha) + (bg * bgAlpha * (1f - fgAlpha))) / outAlpha;
                return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
            }

            canvas.RgbaBytes[index] = Channel(bgR, fgR, bgAlpha, fgAlpha, outAlpha);
            canvas.RgbaBytes[index + 1] = Channel(bgG, fgG, bgAlpha, fgAlpha, outAlpha);
            canvas.RgbaBytes[index + 2] = Channel(bgB, fgB, bgAlpha, fgAlpha, outAlpha);
            canvas.RgbaBytes[index + 3] = (byte)Math.Clamp((int)Math.Round(outAlpha * 255f), 0, 255);
        }
    }

    private EditorSurfaceRenderState? BuildEditorSurfaceRenderState(SpriteDirection direction, bool useCompositeImage)
    {
        var resolution = ResolveEditorResolution();
        if (resolution is null)
        {
            return null;
        }

        var referenceImage = ComposeImportedLayers(useCompositeImage ? _compositeImage ?? _baseImage : _baseImage, direction);
        var mappings = _editorSession.CurrentConfig?.GetMappings(direction).ToDictionary(static mapping => mapping.Source) ?? [];
        var colors = new Color[resolution.Value.Width * resolution.Value.Height];
        var captions = new string[colors.Length];

        for (var y = 0; y < resolution.Value.Height; y++)
        {
            for (var x = 0; x < resolution.Value.Width; x++)
            {
                var coordinate = new PixelCoordinate(x, y);
                var index = (y * resolution.Value.Width) + x;
                var hasMapping = mappings.TryGetValue(coordinate, out var mapping);
                colors[index] = ResolveSurfaceCellColor(referenceImage, coordinate, hasMapping, mapping);
                captions[index] = BuildStaticCaption(hasMapping, mapping);
            }
        }

        return new EditorSurfaceRenderState(direction, resolution.Value.Width, resolution.Value.Height, colors, captions);
    }

    private void RebuildPixelRows(ObservableCollection<PixelRowViewModel> targetRows, SpriteDirection direction, bool useCompositeImage)
    {
        targetRows.Clear();
        var resolution = ResolveEditorResolution();
        if (resolution is null)
        {
            return;
        }

        var referenceImage = ComposeImportedLayers(useCompositeImage ? _compositeImage ?? _baseImage : _baseImage, direction);
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
                var cell = new PixelCellViewModel(direction, x, y)
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

                cells.Add(new PixelCellViewModel(direction, x, y)
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

    private void RebuildDirectionTiles()
    {
        DirectionTiles.Clear();
        var activeDirection = GetSafeSelectedDirection();

        foreach (var direction in AvailableDirections)
        {
            var tile = new DirectionTileViewModel(direction)
            {
                IsActive = direction == activeDirection
            };

            RebuildPixelRows(tile.SourceRows, direction, useCompositeImage: false);
            RebuildPixelRows(tile.TargetRows, direction, useCompositeImage: ShowOverlay);
            DirectionTiles.Add(tile);
        }

        FocusedDirectionTile = DirectionTiles.FirstOrDefault(tile => tile.Direction == activeDirection);
    }

    private void RefreshConfigQueueItems()
    {
        ConfigQueueItems.Clear();
        if (_editorSession.CurrentConfig is null)
        {
            return;
        }

        var pathSummary = string.IsNullOrWhiteSpace(_editorSession.CurrentConfigPath)
            ? "Unsaved draft"
            : _editorSession.CurrentConfigPath!;
        ConfigQueueItems.Add(new ConfigQueueItemViewModel(_editorSession.CurrentConfig.Name, pathSummary, IsActive: true));
    }

    private void RefreshEditorAssetItems()
    {
        EditorAssetItems.Clear();

        if (_editorSession.LoadedAsset is null)
        {
            return;
        }

        var assetName = string.IsNullOrWhiteSpace(SelectedExplorerState)
            ? _editorSession.LoadedAsset.DisplayName ?? "Loaded DMI"
            : SelectedExplorerState;

        EditorAssetItems.Add(
            new EditorAssetItemViewModel(
                assetName,
                "Active session resource",
                string.IsNullOrWhiteSpace(_editorSession.LoadedAsset.SourcePath)
                    ? "Current in-memory DMI"
                    : _editorSession.LoadedAsset.SourcePath,
                isActive: true,
                SelectedEditorAssetTargetSurface,
                SelectedEditorAssetTargetLayer));
    }

    private void RefreshSampleConfigItems()
    {
        SampleConfigItems.Clear();

        var sampleConfigDirectory = ResolveSampleConfigDirectory();
        if (sampleConfigDirectory is null || !Directory.Exists(sampleConfigDirectory))
        {
            return;
        }

        foreach (var path in Directory
                     .EnumerateFiles(sampleConfigDirectory, "*.*", SearchOption.TopDirectoryOnly)
                     .Where(static file => file.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(static file => Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase))
        {
            var isLegacyCsv = path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
            var isActive = isLegacyCsv
                ? string.Equals(path, LegacyCsvPath, StringComparison.OrdinalIgnoreCase)
                : string.Equals(path, ConfigPath, StringComparison.OrdinalIgnoreCase);

            SampleConfigItems.Add(
                new SampleConfigItemViewModel(
                    Path.GetFileNameWithoutExtension(path),
                    path,
                    isLegacyCsv ? "Legacy CSV" : "JSON config",
                    Path.GetFileName(path),
                    isLegacyCsv,
                    isActive));
        }
    }

    private static string? ResolveSampleConfigDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "samples", "configs", "legacy");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private void RefreshBatchPipelineState()
    {
        BatchStateStripItems.Clear();
        foreach (var state in _editorSession.LoadedAsset?.States ?? Array.Empty<DmiStateInfo>())
        {
            BatchStateStripItems.Add(new BatchStateStripItemViewModel(state.Name));
        }

        BatchSourceTreeItems.Clear();
        if (!string.IsNullOrWhiteSpace(BatchInputDirectory) && Directory.Exists(BatchInputDirectory))
        {
            foreach (var item in BuildBatchSourceTreeItems(BatchInputDirectory))
            {
                BatchSourceTreeItems.Add(item);
            }
        }

        if (SelectedBatchSourceItem is not null &&
            BatchSourceTreeItems.All(item => !string.Equals(item.FullPath, SelectedBatchSourceItem.FullPath, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedBatchSourceItem = null;
        }

        OnPropertyChanged(nameof(BatchStateStripItems));
        OnPropertyChanged(nameof(BatchSourceTreeItems));
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
        FitViewportCommand.NotifyCanExecuteChanged();
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

    private static Color ResolveSurfaceCellColor(SpriteImage? image, PixelCoordinate coordinate, bool hasMapping, PixelMapping mapping)
    {
        if (hasMapping && mapping.IsTransparent)
        {
            return TransparentColor;
        }

        if (hasMapping && mapping.Target is not null)
        {
            return MappedColor;
        }

        return TryReadPixelColor(image, coordinate, out var color)
            ? color
            : NeutralColor;
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

    private string BuildStaticCaption(bool hasMapping, PixelMapping mapping)
    {
        if (!GridAboveImage || !hasMapping)
        {
            return string.Empty;
        }

        return mapping.IsTransparent ? "X" : mapping.Target is not null ? "M" : string.Empty;
    }

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

    private static IEnumerable<BatchSourceTreeItemViewModel> BuildBatchSourceTreeItems(string rootDirectory)
    {
        foreach (var directory in Directory.EnumerateDirectories(rootDirectory).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            yield return new BatchSourceTreeItemViewModel(
                Path.GetFileName(directory),
                directory,
                isDirectory: true,
                BuildBatchSourceTreeItems(directory));
        }

        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*.dmi").OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            yield return new BatchSourceTreeItemViewModel(
                Path.GetFileName(file),
                file,
                isDirectory: false);
        }
    }

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
