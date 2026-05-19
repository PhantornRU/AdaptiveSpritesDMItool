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

    private void RebuildPixelRows(ObservableCollection<PixelRowViewModel> targetRows, bool useCompositeImage, bool isEditable)
        => RebuildPixelRows(targetRows, GetSafeSelectedDirection(), useCompositeImage, isEditable);

    private void RefreshInteractionState()
    {
        SelectedSourceCoordinateView = _selectedSourceCoordinate;
        SelectedAreaBounds = _selectedArea is { } area
            ? new PixelAreaBounds(area.Left, area.Top, area.Right, area.Bottom)
            : null;
        SelectedTargetCoordinate = ResolveSelectedEditableCoordinate();
        OppositeHighlightedCoordinate = SelectedTargetCoordinate;
    }

    private PixelCoordinate? ResolveSelectedEditableCoordinate()
        => _selectedEditableCoordinate;

    private void RebuildActiveSurfaceRenderStates()
    {
        var direction = GetSafeSelectedDirection();
        ActiveSourceSurface = BuildSourceSurfaceRenderState(direction);
        ActiveTargetSurface = BuildEditableSurfaceRenderState(direction, useCompositeImage: ShowOverlay);
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

    private void RequestBatchQuickPreviewRefresh()
    {
        _batchQuickPreviewRefreshCts?.Cancel();
        _batchQuickPreviewRefreshCts?.Dispose();
        _batchQuickPreviewRefreshCts = null;

        if (ResolveBatchPreviewAsset() is null ||
            string.IsNullOrWhiteSpace(SelectedBatchStateStripItem?.Name))
        {
            BatchQuickPreviewOriginalImage = null;
            BatchQuickPreviewEditedImage = null;
            return;
        }

        var cancellationSource = new CancellationTokenSource();
        _batchQuickPreviewRefreshCts = cancellationSource;
        var version = ++_batchQuickPreviewRefreshVersion;
        _ = RefreshBatchQuickPreviewAsync(version, cancellationSource.Token);
    }

    private async Task RefreshBatchQuickPreviewAsync(int requestVersion, CancellationToken cancellationToken)
    {
        try
        {
            var stateName = SelectedBatchStateStripItem?.Name;
            var previewAsset = ResolveBatchPreviewAsset();
            var sourcePath = previewAsset?.SourcePath;
            if (previewAsset is null ||
                string.IsNullOrWhiteSpace(sourcePath) ||
                string.IsNullOrWhiteSpace(stateName))
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    BatchQuickPreviewOriginalImage = null;
                    BatchQuickPreviewEditedImage = null;
                });
                return;
            }

            var direction = GetSafeSelectedDirection();
            var originalFrameResult = await _readStateFrameUseCase.ExecuteAsync(sourcePath, stateName, direction, cancellationToken);
            if (originalFrameResult.IsFailure)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (requestVersion != _batchQuickPreviewRefreshVersion)
                    {
                        return;
                    }

                    BatchQuickPreviewOriginalImage = null;
                    BatchQuickPreviewEditedImage = null;
                });
                return;
            }

            var originalFrame = originalFrameResult.Value;
            var editedFrame = _editorSession.CurrentConfig is null
                ? originalFrame
                : RenderEditableSurfaceImage(originalFrame, direction) ?? originalFrame;

            var original = _bitmapSourceFactory.Create(originalFrame);
            var edited = _bitmapSourceFactory.Create(editedFrame);

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (requestVersion != _batchQuickPreviewRefreshVersion)
                {
                    return;
                }

                BatchQuickPreviewOriginalImage = original;
                BatchQuickPreviewEditedImage = edited;
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to refresh batch quick preview.");
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (requestVersion != _batchQuickPreviewRefreshVersion)
                {
                    return;
                }

                BatchQuickPreviewOriginalImage = null;
                BatchQuickPreviewEditedImage = null;
            });
        }
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

    private void RemoveImportedStateItem(ImportedDmiStateItemViewModel item)
    {
        if (ReferenceEquals(SelectedImportedDmiStateItem, item))
        {
            SelectedImportedDmiStateItem = null;
            if (string.Equals(SelectedExplorerState, item.StateName, StringComparison.OrdinalIgnoreCase))
            {
                SelectedExplorerState = ImportedDmiStateItems
                    .Where(existing => !ReferenceEquals(existing, item))
                    .Select(existing => existing.StateName)
                    .FirstOrDefault() ?? string.Empty;
            }
        }

        item.PropertyChanged -= OnImportedStateItemPropertyChanged;
        ImportedDmiStateItems.Remove(item);
        InvalidateImportedStateFrameCache();
        RefreshImportedStateComposition();
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
        SelectedImportedDmiStateItem = null;
        InvalidateImportedStateFrameCache();
    }

    private void DeactivateConfigQueueSelection()
    {
        foreach (var item in ConfigQueueItems)
        {
            item.IsActive = false;
        }

        _activeConfigQueueItemId = null;
    }

    private string GenerateNextDraftConfigName()
    {
        var usedNames = ConfigQueueItems
            .Select(item => item.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        const string rootName = "Unsaved Draft";
        if (!usedNames.Any(name => string.Equals(name, rootName, StringComparison.OrdinalIgnoreCase)))
        {
            return rootName;
        }

        var nextIndex = 2;
        while (usedNames.Any(name => string.Equals(name, $"{rootName} {nextIndex}", StringComparison.OrdinalIgnoreCase)))
        {
            nextIndex++;
        }

        return $"{rootName} {nextIndex}";
    }

    private void AdoptCurrentConfigDisplayName(string? preferredName)
    {
        if (_editorSession.CurrentConfig is null || string.IsNullOrWhiteSpace(preferredName))
        {
            return;
        }

        if (string.Equals(_editorSession.CurrentConfig.Name, preferredName, StringComparison.Ordinal))
        {
            DraftConfigName = preferredName;
            return;
        }

        var renameResult = _editorSession.RenameCurrentConfig(preferredName);
        if (renameResult.IsFailure)
        {
            StatusMessage = renameResult.Error.Message;
            return;
        }

        DraftConfigName = preferredName;
    }

    private void InvalidateImportedStateFrameCache() => _importedStateFrameCache.Clear();

    private void OnImportedStateItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImportedDmiStateItemViewModel.Order))
        {
            RefreshImportedStateComposition();
        }
    }

    private ConfigQueueItemViewModel? FindActiveConfigQueueItem()
        => _activeConfigQueueItemId is not { } activeId
            ? null
            : ConfigQueueItems.FirstOrDefault(item => item.Id == activeId);

    private static string BuildConfigPathSummary(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? "Unsaved draft"
            : Path.GetFileName(path);

    private void SyncCurrentConfigIntoActiveQueueItem()
    {
        var activeItem = FindActiveConfigQueueItem();
        if (activeItem is null || _editorSession.CurrentConfig is null)
        {
            return;
        }

        activeItem.Name = _editorSession.CurrentConfig.Name;
        activeItem.ConfigPath = string.IsNullOrWhiteSpace(_editorSession.CurrentConfigPath) ? null : _editorSession.CurrentConfigPath;
        activeItem.PathSummary = BuildConfigPathSummary(activeItem.ConfigPath);
        activeItem.ConfigSnapshot = _editorSession.CurrentConfig.Clone();
        activeItem.IsActive = true;
    }

    private ConfigQueueItemViewModel AddCurrentConfigQueueItemAsNewActive()
    {
        if (_editorSession.CurrentConfig is null)
        {
            throw new InvalidOperationException("Cannot create a config queue item without an active config.");
        }

        foreach (var existingItem in ConfigQueueItems)
        {
            existingItem.IsActive = false;
        }

        var configPath = string.IsNullOrWhiteSpace(_editorSession.CurrentConfigPath) ? null : _editorSession.CurrentConfigPath;
        var item = new ConfigQueueItemViewModel(
            Guid.NewGuid(),
            _editorSession.CurrentConfig.Name,
            BuildConfigPathSummary(configPath),
            configPath,
            _editorSession.CurrentConfig.Clone(),
            isActive: true);
        ConfigQueueItems.Add(item);
        _activeConfigQueueItemId = item.Id;
        return item;
    }

    private void UpsertCurrentSessionIntoConfigQueue(bool forceAddNewItem = false)
    {
        if (_editorSession.CurrentConfig is null)
        {
            ConfigQueueItems.Clear();
            _activeConfigQueueItemId = null;
            return;
        }

        if (forceAddNewItem)
        {
            AddCurrentConfigQueueItemAsNewActive();
            return;
        }

        var activeItem = FindActiveConfigQueueItem();
        if (activeItem is not null)
        {
            SyncCurrentConfigIntoActiveQueueItem();
            return;
        }

        var configPath = string.IsNullOrWhiteSpace(_editorSession.CurrentConfigPath) ? null : _editorSession.CurrentConfigPath;
        ConfigQueueItemViewModel? matchedItem = null;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            matchedItem = ConfigQueueItems.FirstOrDefault(item =>
                string.Equals(item.ConfigPath, configPath, StringComparison.OrdinalIgnoreCase));
        }

        if (matchedItem is null)
        {
            AddCurrentConfigQueueItemAsNewActive();
            return;
        }

        foreach (var existingItem in ConfigQueueItems)
        {
            existingItem.IsActive = existingItem.Id == matchedItem.Id;
        }

        _activeConfigQueueItemId = matchedItem.Id;
        matchedItem.Name = _editorSession.CurrentConfig.Name;
        matchedItem.ConfigPath = configPath;
        matchedItem.PathSummary = BuildConfigPathSummary(configPath);
        matchedItem.ConfigSnapshot = _editorSession.CurrentConfig.Clone();
        matchedItem.IsActive = true;
    }

    private void ActivateConfigQueueItemCore(ConfigQueueItemViewModel item)
    {
        if (item.IsActive && _activeConfigQueueItemId == item.Id)
        {
            return;
        }

        SyncCurrentConfigIntoActiveQueueItem();

        var result = _editorSession.SetCurrentConfig(item.ConfigSnapshot.Clone(), item.ConfigPath);
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        foreach (var existingItem in ConfigQueueItems)
        {
            existingItem.IsActive = existingItem.Id == item.Id;
        }

        _activeConfigQueueItemId = item.Id;
        ConfigPath = item.ConfigPath ?? string.Empty;
        SaveConfigPath = item.ConfigPath ?? string.Empty;
        DraftConfigName = item.Name;
        StatusMessage = $"Activated config '{item.Name}'.";
        RefreshWorkspaceState();
        ApplyAdaptiveEditorZoom(force: true);
        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();
        RequestAutoPreviewRefresh();
        PersistWorkspaceSettingsInBackground();
    }

    private void RemoveConfigQueueItemCore(ConfigQueueItemViewModel item)
    {
        var wasActive = item.IsActive || _activeConfigQueueItemId == item.Id;
        ConfigQueueItems.Remove(item);

        if (!wasActive)
        {
            PersistWorkspaceSettingsInBackground();
            return;
        }

        _activeConfigQueueItemId = null;

        if (ConfigQueueItems.Count > 0)
        {
            ActivateConfigQueueItemCore(ConfigQueueItems[0]);
            return;
        }

        if (_editorSession.LoadedAsset is not null)
        {
            CreateImplicitDraftConfig(forceNewQueueItem: true);
            RefreshWorkspaceState();
            ApplyAdaptiveEditorZoom(force: true);
            RefreshPreviewSelectionSummary();
            RefreshEditorSurface();
            RequestAutoPreviewRefresh();
            PersistWorkspaceSettingsInBackground();
            return;
        }

        ConfigPath = string.Empty;
        SaveConfigPath = string.Empty;
        DraftConfigName = "Unsaved Draft";
        _editorSession.Reset();
        StatusMessage = "Removed the active config.";
        RefreshWorkspaceState();
        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();
        PersistWorkspaceSettingsInBackground();
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

    private EditorSurfaceRenderState? BuildSourceSurfaceRenderState(SpriteDirection direction)
    {
        var resolution = ResolveEditorResolution();
        if (resolution is null)
        {
            return null;
        }

        var referenceImage = ComposeImportedLayers(ResolvePreviewImage(direction, useCompositeImage: false), direction);
        var colors = new Color[resolution.Value.Width * resolution.Value.Height];
        var captions = new string[colors.Length];

        for (var y = 0; y < resolution.Value.Height; y++)
        {
            for (var x = 0; x < resolution.Value.Width; x++)
            {
                var coordinate = new PixelCoordinate(x, y);
                var index = (y * resolution.Value.Width) + x;
                colors[index] = ResolveSurfaceCellColor(referenceImage, coordinate);
                captions[index] = string.Empty;
            }
        }

        return new EditorSurfaceRenderState(direction, resolution.Value.Width, resolution.Value.Height, colors, captions);
    }

    private EditorSurfaceRenderState? BuildEditableSurfaceRenderState(SpriteDirection direction, bool useCompositeImage)
    {
        var resolution = ResolveEditorResolution();
        if (resolution is null)
        {
            return null;
        }

        var renderedEditableImage = BuildRenderedEditableSurfaceImage(direction, useCompositeImage);
        var mappings = GetEditableMappings(direction);
        var colors = new Color[resolution.Value.Width * resolution.Value.Height];
        var captions = new string[colors.Length];

        for (var y = 0; y < resolution.Value.Height; y++)
        {
            for (var x = 0; x < resolution.Value.Width; x++)
            {
                var coordinate = new PixelCoordinate(x, y);
                var index = (y * resolution.Value.Width) + x;
                var hasMapping = mappings.TryGetValue(coordinate, out var mapping);
                colors[index] = ResolveSurfaceCellColor(renderedEditableImage, coordinate, hasMapping, mapping);
                captions[index] = BuildMappingCaption(hasMapping, mapping);
            }
        }

        return new EditorSurfaceRenderState(
            direction,
            resolution.Value.Width,
            resolution.Value.Height,
            colors,
            captions,
            ResolveEditableBackingOrigins(direction));
    }

    private void RebuildPixelRows(
        ObservableCollection<PixelRowViewModel> targetRows,
        SpriteDirection direction,
        bool useCompositeImage,
        bool isEditable)
    {
        targetRows.Clear();
        var resolution = ResolveEditorResolution();
        if (resolution is null)
        {
            return;
        }

        var referenceImage = isEditable
            ? null
            : ComposeImportedLayers(ResolvePreviewImage(direction, useCompositeImage: false), direction);
        var renderedEditableImage = isEditable
            ? BuildRenderedEditableSurfaceImage(direction, useCompositeImage)
            : null;
        var mappings = GetEditableMappings(direction);

        for (var y = 0; y < resolution.Value.Height; y++)
        {
            var cells = new List<PixelCellViewModel>(resolution.Value.Width);
            for (var x = 0; x < resolution.Value.Width; x++)
            {
                var coordinate = new PixelCoordinate(x, y);
                var hasMapping = mappings.TryGetValue(coordinate, out var pixelMapping);
                var cell = new PixelCellViewModel(direction, x, y)
                {
                    Fill = isEditable
                        ? CreateEditableCellFill(renderedEditableImage, coordinate, hasMapping, pixelMapping)
                        : CreateSourceCellFill(referenceImage, coordinate),
                    Border = isEditable
                        ? CreateEditableCellBorder(coordinate)
                        : CreateSourceCellBorder(coordinate),
                    Foreground = hasMapping && pixelMapping.IsTransparent ? Brushes.Black : Brushes.Transparent,
                    Caption = isEditable
                        ? BuildEditableCellCaption(coordinate, hasMapping, pixelMapping)
                        : BuildSourceCellCaption(coordinate),
                    ToolTip = isEditable
                        ? BuildEditableCellToolTip(coordinate, hasMapping, pixelMapping)
                        : BuildSourceCellToolTip(coordinate)
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
                    ToolTip = isTransparent
                        ? $"Editable {coordinate} -> transparent"
                        : $"Editable {coordinate} <- Source {effectiveTarget}"
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

            RebuildPixelRows(tile.SourceRows, direction, useCompositeImage: false, isEditable: false);
            RebuildPixelRows(tile.TargetRows, direction, useCompositeImage: ShowOverlay, isEditable: true);
            DirectionTiles.Add(tile);
        }

        FocusedDirectionTile = DirectionTiles.FirstOrDefault(tile => tile.Direction == activeDirection);
    }

    private void RefreshConfigQueueItems()
    {
        UpsertCurrentSessionIntoConfigQueue();
    }

    private SpriteImage? ResolvePreviewImage(SpriteDirection direction, bool useCompositeImage)
    {
        var activeDirection = GetSafeSelectedDirection();
        if (direction == activeDirection)
        {
            return useCompositeImage ? _compositeImage ?? _baseImage : _baseImage;
        }

        if (useCompositeImage && _navigatorCompositeImages.TryGetValue(direction, out var composite))
        {
            return composite ?? (_navigatorBaseImages.TryGetValue(direction, out var compositeBase) ? compositeBase : null);
        }

        if (_navigatorBaseImages.TryGetValue(direction, out var baseImage))
        {
            return baseImage;
        }

        return useCompositeImage ? _compositeImage ?? _baseImage : _baseImage;
    }

    private SpriteImage? BuildRenderedEditableSurfaceImage(SpriteDirection direction, bool includePreviewDecorations)
    {
        var referenceImage = ComposeImportedLayers(ResolvePreviewImage(direction, useCompositeImage: false), direction);
        var renderedEditableImage = RenderEditableSurfaceImage(referenceImage, direction);
        if (!includePreviewDecorations || renderedEditableImage is null)
        {
            return renderedEditableImage;
        }

        return ComposePreviewDecorations(renderedEditableImage, direction);
    }

    private SpriteImage ComposePreviewDecorations(SpriteImage editableBase, SpriteDirection direction)
    {
        if (direction != GetSafeSelectedDirection() || (_landmarkImage is null && _overlayImage is null))
        {
            return editableBase;
        }

        var composed = new SpriteImage(
            editableBase.Width,
            editableBase.Height,
            new byte[editableBase.Width * editableBase.Height * 4]);

        if (_landmarkImage is not null)
        {
            BlendOver(composed, _landmarkImage);
        }

        BlendOver(composed, editableBase);

        if (_overlayImage is not null)
        {
            BlendOver(composed, _overlayImage);
        }

        return composed;
    }

    private IReadOnlyDictionary<PixelCoordinate, PixelCoordinate?> ResolveEditableBackingOrigins(SpriteDirection direction)
    {
        var activeDirection = GetSafeSelectedDirection();
        if (direction == activeDirection)
        {
            return _editableBackingOrigins;
        }

        return _navigatorEditableBackingOrigins.TryGetValue(direction, out var origins)
            ? origins
            : _editableBackingOrigins;
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

            SampleConfigItems.Add(
                new SampleConfigItemViewModel(
                    Path.GetFileNameWithoutExtension(path),
                    path,
                    isLegacyCsv ? "Legacy CSV" : "JSON config",
                    Path.GetFileName(path),
                    isLegacyCsv,
                    false));
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

    private void RefreshBatchPipelineState(bool rebuildSourceTree = true)
    {
        var previousSelectedStateName = SelectedBatchStateStripItem?.Name;
        BatchStateStripItems.Clear();
        foreach (var state in ResolveBatchPreviewAsset()?.States ?? Array.Empty<DmiStateInfo>())
        {
            BatchStateStripItems.Add(new BatchStateStripItemViewModel(state.Name));
        }

        SelectedBatchStateStripItem = BatchStateStripItems.FirstOrDefault(item =>
            string.Equals(item.Name, previousSelectedStateName, StringComparison.OrdinalIgnoreCase))
            ?? BatchStateStripItems.FirstOrDefault(item =>
                string.Equals(item.Name, SelectedExplorerState, StringComparison.OrdinalIgnoreCase))
            ?? BatchStateStripItems.FirstOrDefault(item =>
                string.Equals(item.Name, "human32x", StringComparison.OrdinalIgnoreCase))
            ?? BatchStateStripItems.FirstOrDefault();

        var previousSelectedBatchSourcePath = SelectedBatchSourceItem?.FullPath;
        if (rebuildSourceTree)
        {
            BatchSourceTreeItems.Clear();
            if (!string.IsNullOrWhiteSpace(BatchInputDirectory) && Directory.Exists(BatchInputDirectory))
            {
                foreach (var item in BuildBatchSourceTreeItems(BatchInputDirectory))
                {
                    BatchSourceTreeItems.Add(item);
                }
            }

            if (!string.IsNullOrWhiteSpace(previousSelectedBatchSourcePath))
            {
                SelectedBatchSourceItem = FindBatchSourceTreeItem(BatchSourceTreeItems, previousSelectedBatchSourcePath!);
            }

            BeginBatchSourceTreeValidation();
        }

        if (SelectedBatchSourceItem is null && !string.IsNullOrWhiteSpace(previousSelectedBatchSourcePath))
        {
            _selectedBatchPreviewAsset = null;
        }

        OnPropertyChanged(nameof(BatchStateStripItems));
        if (rebuildSourceTree)
        {
            OnPropertyChanged(nameof(BatchSourceTreeItems));
        }
        RequestBatchQuickPreviewRefresh();
    }

    private static BatchSourceTreeItemViewModel? FindBatchSourceTreeItem(
        IEnumerable<BatchSourceTreeItemViewModel> items,
        string fullPath)
    {
        foreach (var item in items)
        {
            if (string.Equals(item.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }

            var nested = FindBatchSourceTreeItem(item.Children, fullPath);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private DmiAssetInfo? ResolveBatchPreviewAsset()
        => SelectedBatchSourceItem is null
            ? _editorSession.LoadedAsset
            : _selectedBatchPreviewAsset;

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
        RequestBatchQuickPreviewRefresh();
    }

    private void ClearPreviewArtifacts()
    {
        _baseImage = null;
        _landmarkImage = null;
        _overlayImage = null;
        _compositeImage = null;
        _navigatorBaseImages.Clear();
        _navigatorCompositeImages.Clear();
        CurrentPreviewImage = null;
        BatchQuickPreviewOriginalImage = null;
        BatchQuickPreviewEditedImage = null;
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

    // The persisted config keeps legacy semantics: mapping.Source is the editable cell,
    // mapping.Target is the source/palette coordinate used to draw that editable cell.
    private Dictionary<PixelCoordinate, PixelMapping> GetEditableMappings(SpriteDirection direction) =>
        _editorSession.CurrentConfig?.GetMappings(direction).ToDictionary(static mapping => mapping.Source) ?? [];

    private SpriteImage? RenderEditableSurfaceImage(SpriteImage? referenceImage, SpriteDirection direction)
    {
        if (referenceImage is null)
        {
            return null;
        }

        var rendered = new SpriteImage(referenceImage.Width, referenceImage.Height, referenceImage.RgbaBytes[..]);
        if (_editorSession.CurrentConfig is null)
        {
            return rendered;
        }

        foreach (var mapping in _editorSession.CurrentConfig.GetMappings(direction))
        {
            var destCoordinate = mapping.Source;
            var destinationOffset = ((destCoordinate.Y * rendered.Width) + destCoordinate.X) * 4;

            if (mapping.Target is not { } targetCoordinate)
            {
                rendered.RgbaBytes[destinationOffset] = 0;
                rendered.RgbaBytes[destinationOffset + 1] = 0;
                rendered.RgbaBytes[destinationOffset + 2] = 0;
                rendered.RgbaBytes[destinationOffset + 3] = 0;
                continue;
            }

            if (!TryReadPixelColor(referenceImage, targetCoordinate, out var sourceColor))
            {
                rendered.RgbaBytes[destinationOffset] = 0;
                rendered.RgbaBytes[destinationOffset + 1] = 0;
                rendered.RgbaBytes[destinationOffset + 2] = 0;
                rendered.RgbaBytes[destinationOffset + 3] = 0;
                continue;
            }

            rendered.RgbaBytes[destinationOffset] = sourceColor.R;
            rendered.RgbaBytes[destinationOffset + 1] = sourceColor.G;
            rendered.RgbaBytes[destinationOffset + 2] = sourceColor.B;
            rendered.RgbaBytes[destinationOffset + 3] = sourceColor.A;
        }

        return rendered;
    }

    private static Brush CreateSourceCellFill(SpriteImage? image, PixelCoordinate coordinate)
        => TryReadPixelColor(image, coordinate, out var color)
            ? CreateBrush(color)
            : NeutralBrush;

    private Brush CreateEditableCellFill(SpriteImage? image, PixelCoordinate coordinate, bool hasMapping, PixelMapping mapping)
    {
        if (_selectedArea?.Contains(coordinate) == true)
        {
            return AreaBrush;
        }

        if (_selectedEditableCoordinate == coordinate)
        {
            return SelectedBrush;
        }

        if (hasMapping && mapping.IsTransparent)
        {
            return TransparentBrush;
        }

        if (TryReadPixelColor(image, coordinate, out var color))
        {
            return CreateBrush(color);
        }

        if (hasMapping && mapping.Target is not null)
        {
            return MappedBrush;
        }

        return NeutralBrush;
    }

    private static Color ResolveSurfaceCellColor(SpriteImage? image, PixelCoordinate coordinate)
    {
        return TryReadPixelColor(image, coordinate, out var color)
            ? color
            : NeutralColor;
    }

    private static Color ResolveSurfaceCellColor(SpriteImage? image, PixelCoordinate coordinate, bool hasMapping, PixelMapping mapping)
    {
        if (hasMapping && mapping.IsTransparent)
        {
            return TransparentColor;
        }

        return ResolveSurfaceCellColor(image, coordinate);
    }

    private Brush CreateSourceCellBorder(PixelCoordinate coordinate)
    {
        if (!ShowGrid)
        {
            return Brushes.Transparent;
        }

        return GridBrush;
    }

    private Brush CreateEditableCellBorder(PixelCoordinate coordinate)
    {
        if (!ShowGrid)
        {
            return Brushes.Transparent;
        }

        if (_selectedEditableCoordinate == coordinate || _selectedArea?.Contains(coordinate) == true)
        {
            return SelectedBrush;
        }

        return GridBrush;
    }

    private static string BuildSourceCellCaption(PixelCoordinate coordinate) => string.Empty;

    private string BuildEditableCellCaption(PixelCoordinate coordinate, bool hasMapping, PixelMapping mapping)
    {
        if (_selectedEditableCoordinate == coordinate)
        {
            return "T";
        }

        if (_selectedArea?.Contains(coordinate) == true)
        {
            return "A";
        }

        return BuildMappingCaption(hasMapping, mapping);
    }

    private static string BuildSourceCellToolTip(PixelCoordinate coordinate) =>
        $"Source {coordinate}";

    private static string BuildEditableCellToolTip(PixelCoordinate coordinate, bool hasMapping, PixelMapping mapping) =>
        !hasMapping
            ? $"Editable {coordinate} <- Source {coordinate}"
            : mapping.Target is null
                ? $"Editable {coordinate} -> transparent"
                : $"Editable {coordinate} <- Source {mapping.Target}";

    private string BuildMappingCaption(bool hasMapping, PixelMapping mapping)
    {
        if (!hasMapping)
        {
            return string.Empty;
        }

        if (ShowSourceCoordinateCaptions && mapping.Target is { } sourceCoordinate)
        {
            return FormatCoordinateCaption(sourceCoordinate);
        }

        if (!GridAboveImage)
        {
            return string.Empty;
        }

        return mapping.IsTransparent ? "X" : mapping.Target is not null ? "M" : string.Empty;
    }

    private static string FormatCoordinateCaption(PixelCoordinate coordinate) => $"{coordinate.X},{coordinate.Y}";

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

    private static (int DeltaX, int DeltaY) ComputeClampedAreaDelta(
        PixelAreaSelection area,
        PixelCoordinate dragAnchor,
        PixelCoordinate currentCoordinate,
        SpriteResolution resolution)
    {
        var deltaX = currentCoordinate.X - dragAnchor.X;
        var deltaY = currentCoordinate.Y - dragAnchor.Y;

        deltaX = Math.Clamp(deltaX, -area.Left, (resolution.Width - 1) - area.Right);
        deltaY = Math.Clamp(deltaY, -area.Top, (resolution.Height - 1) - area.Bottom);

        return (deltaX, deltaY);
    }

    private static PixelAreaSelection TranslateArea(PixelAreaSelection area, int deltaX, int deltaY) =>
        new(
            new PixelCoordinate(area.Start.X + deltaX, area.Start.Y + deltaY),
            new PixelCoordinate(area.End.X + deltaX, area.End.Y + deltaY));

    private static string NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();

    private static string? NormalizeOptionalPath(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IEnumerable<BatchSourceTreeItemViewModel> BuildBatchSourceTreeItems(string rootDirectory) =>
        BuildBatchSourceTreeItems(
            rootDirectory,
            Directory.EnumerateDirectories,
            static directory => Directory.EnumerateFiles(directory, "*.dmi"));

    private static IEnumerable<BatchSourceTreeItemViewModel> BuildBatchSourceTreeItems(
        string rootDirectory,
        Func<string, IEnumerable<string>> enumerateDirectories,
        Func<string, IEnumerable<string>> enumerateDmiFiles) =>
        TryBuildBatchSourceTreeItems(rootDirectory, enumerateDirectories, enumerateDmiFiles, out var items)
            ? items
            : [];

    private static bool TryBuildBatchSourceTreeItems(
        string rootDirectory,
        Func<string, IEnumerable<string>> enumerateDirectories,
        Func<string, IEnumerable<string>> enumerateDmiFiles,
        out IReadOnlyList<BatchSourceTreeItemViewModel> items)
    {
        items = [];
        if (!TryEnumerateBatchSourcePaths(rootDirectory, enumerateDirectories, out var directories) ||
            !TryEnumerateBatchSourcePaths(rootDirectory, enumerateDmiFiles, out var files))
        {
            return false;
        }

        var treeItems = new List<BatchSourceTreeItemViewModel>();
        foreach (var directory in directories)
        {
            if (!TryBuildBatchSourceTreeItems(directory, enumerateDirectories, enumerateDmiFiles, out var children))
            {
                continue;
            }

            treeItems.Add(
                new BatchSourceTreeItemViewModel(
                    Path.GetFileName(directory),
                    directory,
                    isDirectory: true,
                    children));
        }

        foreach (var file in files)
        {
            treeItems.Add(
                new BatchSourceTreeItemViewModel(
                    Path.GetFileName(file),
                    file,
                    isDirectory: false));
        }

        items = treeItems;
        return true;
    }

    private static bool TryEnumerateBatchSourcePaths(
        string rootDirectory,
        Func<string, IEnumerable<string>> enumeratePaths,
        out IReadOnlyList<string> paths)
    {
        try
        {
            paths = enumeratePaths(rootDirectory)
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            paths = [];
            return false;
        }
        catch (IOException)
        {
            paths = [];
            return false;
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
