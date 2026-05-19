using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class WorkspaceShellViewModel
{
    public async Task InitializeAsync()
    {
        ResetWorkspaceCore();

        var settingsResult = await _loadWorkspaceSettingsUseCase.ExecuteAsync(CancellationToken.None);
        if (settingsResult.IsFailure)
        {
            StatusMessage = settingsResult.Error.Message;
            return;
        }

        ApplyWorkspaceSettings(settingsResult.Value);
        await RestoreWorkspaceAsync();
        if (HasLoadedAsset && !HasActiveConfig)
        {
            CreateImplicitDraftConfig(forceNewQueueItem: true);
        }

        RefreshWorkspaceState();
        if (HasEditorWorkflow)
        {
            ApplyAdaptiveEditorZoom(force: true);
        }

        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();
        NavigateToSection(HasEditorWorkflow ? ShellSectionKind.Editor : ShellSectionKind.Start);
    }

    public Task PersistWorkspaceSettingsAsync() =>
        PersistWorkspaceSettingsAsync(CancellationToken.None);

    public async Task PersistWorkspaceSettingsAsync(CancellationToken cancellationToken)
    {
        if (_isDisposed)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var settings = BuildWorkspaceSettings();
        var lockAcquired = false;

        try
        {
            await _workspaceSettingsPersistenceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockAcquired = true;

            if (_isDisposed)
            {
                return;
            }

            var result = await _saveWorkspaceSettingsUseCase
                .ExecuteAsync(settings, cancellationToken)
                .ConfigureAwait(false);
            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to persist workspace settings: {Message}", result.Error.Message);
            }
        }
        catch (ObjectDisposedException) when (_isDisposed)
        {
            return;
        }
        finally
        {
            if (lockAcquired)
            {
                try
                {
                    _workspaceSettingsPersistenceGate.Release();
                }
                catch (ObjectDisposedException) when (_isDisposed)
                {
                }
            }
        }
    }

    private void PersistWorkspaceSettingsInBackground()
    {
        if (_isDisposed)
        {
            return;
        }

        _ = PersistWorkspaceSettingsSafelyAsync();
    }

    private async Task PersistWorkspaceSettingsSafelyAsync()
    {
        try
        {
            await PersistWorkspaceSettingsAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException)
        {
            if (_isDisposed)
            {
                return;
            }

            _logger.LogWarning(exception, "Workspace settings background persistence was interrupted.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Workspace settings background persistence failed.");
        }
    }

    private void EnsureActiveDirection(SpriteDirection direction)
    {
        if (SelectedDirection == direction)
        {
            return;
        }

        if (TryApplySelectedDirection(direction, refreshUi: false))
        {
            RefreshDirectionViewportActivation();
        }
    }

    public void HandleSourceCellPointerDown(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (!CanEditConfig())
        {
            return;
        }

        EnsureActiveDirection(cell.Direction);
        UpdateSourceHoverState(cell.Coordinate);

        switch (SelectedEditorTool)
        {
            case EditorTool.Single:
            case EditorTool.Fill:
                _selectedSourceCoordinate = cell.Coordinate;
                SelectedSourceSummary = $"Source pixel {cell.Coordinate} selected.";
                EditorStatus = SelectedEditorTool == EditorTool.Single
                    ? "Pick an editable pixel to draw the selected source."
                    : "Drag across Editable to fill an area from the selected source pixel.";
                RefreshInteractionState();
                break;
        }
    }

    public void HandleSourceCellPointerEnter(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        EnsureActiveDirection(cell.Direction);
        UpdateSourceHoverState(cell.Coordinate);
    }

    public void HandleSourceSurfacePointerLeave()
    {
        ClearHoverState();
    }

    public void HandleTargetSurfacePointerLeave()
    {
        if (_isDraggingEditableArea)
        {
            return;
        }

        ClearHoverState();
    }

    public void HandleSourceSurfaceHover(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        EnsureActiveDirection(cell.Direction);
        UpdateSourceHoverState(cell.Coordinate);
    }

    public void HandleTargetSurfaceHover(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        EnsureActiveDirection(cell.Direction);
        UpdateEditableHoverState(cell.Coordinate);
    }

    public void HandleSourceCellPointerUp(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        EnsureActiveDirection(cell.Direction);
        UpdateSourceHoverState(cell.Coordinate);
    }

    public void HandleTargetCellPointerDown(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (!CanEditConfig())
        {
            return;
        }

        EnsureActiveDirection(cell.Direction);
        UpdateEditableHoverState(cell.Coordinate);
        _selectedEditableCoordinate = cell.Coordinate;

        switch (SelectedEditorTool)
        {
            case EditorTool.Single:
                EditorStatus = _selectedSourceCoordinate is null
                    ? "Pick a source pixel first."
                    : "Release to draw the selected source into Editable.";
                RefreshInteractionState();
                break;
            case EditorTool.Fill:
                if (_selectedSourceCoordinate is null)
                {
                    EditorStatus = "Pick a source pixel first.";
                    RefreshInteractionState();
                    break;
                }

                StartEditableAreaDrag(cell.Coordinate, EditableDragAction.FillArea, "Fill drag started in Editable.");
                break;
            case EditorTool.Delete:
                ResetEditableDragState();
                QueueRestoreStrokeCoordinate(cell.Coordinate);
                break;
            case EditorTool.Undo:
                EditorStatus = "Release to restore the editable pixel to its original source.";
                RefreshInteractionState();
                break;
            case EditorTool.UndoArea:
                StartEditableAreaDrag(cell.Coordinate, EditableDragAction.RestoreArea, "Restore drag started in Editable.");
                break;
            case EditorTool.Move:
                StartEditableMoveDrag(
                    cell.Coordinate,
                    new PixelAreaSelection(cell.Coordinate, cell.Coordinate),
                    EditableDragAction.MoveSingle,
                    "Move drag started in Editable.");
                break;
            case EditorTool.Select:
                if (_selectedArea is { } existingArea && existingArea.Contains(cell.Coordinate))
                {
                    StartEditableMoveDrag(cell.Coordinate, existingArea, EditableDragAction.MoveSelection, "Moving selected editable area.");
                }
                else
                {
                    StartEditableAreaDrag(cell.Coordinate, EditableDragAction.SelectArea, "Selection drag started in Editable.");
                }
                break;
        }
    }

    public void HandleTargetCellPointerEnter(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        EnsureActiveDirection(cell.Direction);
        UpdateEditableHoverState(cell.Coordinate);

        if (SelectedEditorTool == EditorTool.Delete)
        {
            QueueRestoreStrokeCoordinate(cell.Coordinate);
            return;
        }

        if (!_isDraggingEditableArea || _editableDragAnchor is null)
        {
            return;
        }

        _selectedEditableCoordinate = cell.Coordinate;

        switch (_editableDragAction)
        {
            case EditableDragAction.FillArea:
            case EditableDragAction.RestoreArea:
            case EditableDragAction.SelectArea:
                _selectedArea = new PixelAreaSelection(_editableDragAnchor.Value, cell.Coordinate);
                break;
            case EditableDragAction.MoveSingle:
            case EditableDragAction.MoveSelection:
                if (_editableDragOriginArea is not { } originArea || ResolveEditorResolution() is not { } resolution)
                {
                    return;
                }

                var (deltaX, deltaY) = ComputeClampedAreaDelta(originArea, _editableDragAnchor.Value, cell.Coordinate, resolution);
                _selectedArea = TranslateArea(originArea, deltaX, deltaY);
                _selectedEditableCoordinate = _selectedArea.Value.Start;
                break;
            case EditableDragAction.None:
            default:
                return;
        }

        SelectedAreaSummary = _selectedArea is { } area
            ? DescribeArea(area)
            : "No area selected.";
        RefreshInteractionState();
    }

    public void HandleTargetCellPointerUp(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (!CanEditConfig())
        {
            return;
        }

        EnsureActiveDirection(cell.Direction);
        UpdateEditableHoverState(cell.Coordinate);
        _selectedEditableCoordinate = cell.Coordinate;

        if (_isDraggingEditableArea)
        {
            CompleteEditableDrag(cell.Coordinate);
            return;
        }

        switch (SelectedEditorTool)
        {
            case EditorTool.Single:
                if (_selectedSourceCoordinate is not { } source)
                {
                    StatusMessage = "Pick a source pixel first.";
                    RefreshInteractionState();
                    return;
                }

                ApplySourcePixelToEditable(cell.Coordinate, source, "Applied source pixel to Editable.");
                break;
            case EditorTool.Delete:
                FinalizeRestoreStroke();
                break;
            case EditorTool.Undo:
                ApplyRestoreOperation(new PixelAreaSelection(cell.Coordinate, cell.Coordinate), "Restored editable pixel.");
                break;
        }
    }

    private void QueueRestoreStrokeCoordinate(PixelCoordinate coordinate)
    {
        _selectedEditableCoordinate = coordinate;
        _selectedArea = new PixelAreaSelection(coordinate, coordinate);
        SelectedAreaSummary = DescribeArea(_selectedArea.Value);

        if (_pendingRestoreStrokeCoordinates.Add(coordinate))
        {
            _hasPendingRestoreStrokeFinalize = true;
        }

        ScheduleRestoreStrokeFlush();
    }

    private void ScheduleRestoreStrokeFlush()
    {
        _restoreStrokeFlushCts?.Cancel();
        _restoreStrokeFlushCts?.Dispose();

        var cancellationSource = new CancellationTokenSource();
        _restoreStrokeFlushCts = cancellationSource;
        _ = FlushRestoreStrokeAsync(cancellationSource.Token);
    }

    private async Task FlushRestoreStrokeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(16, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(FlushRestoreStrokeIncremental);
    }

    private void FlushRestoreStrokeIncremental()
    {
        if (_pendingRestoreStrokeCoordinates.Count == 0)
        {
            return;
        }

        var coordinates = _pendingRestoreStrokeCoordinates.ToArray();
        _pendingRestoreStrokeCoordinates.Clear();
        ApplyRestoreOperations(
            coordinates,
            "Restoring editable pixels...",
            refreshWorkspace: false,
            rebuildNavigator: false,
            refreshPreview: false);
    }

    private void FinalizeRestoreStroke()
    {
        _restoreStrokeFlushCts?.Cancel();
        _restoreStrokeFlushCts?.Dispose();
        _restoreStrokeFlushCts = null;

        if (_pendingRestoreStrokeCoordinates.Count > 0)
        {
            var coordinates = _pendingRestoreStrokeCoordinates.ToArray();
            _pendingRestoreStrokeCoordinates.Clear();
            ApplyRestoreOperations(
                coordinates,
                "Restored editable pixels.",
                refreshWorkspace: true,
                rebuildNavigator: true,
                refreshPreview: true);
            _hasPendingRestoreStrokeFinalize = false;
            return;
        }

        if (!_hasPendingRestoreStrokeFinalize)
        {
            return;
        }

        _hasPendingRestoreStrokeFinalize = false;
        InvalidateNavigatorSnapshotCache();
        RefreshWorkspaceState();
        RefreshEditorSurface();
        RequestAutoPreviewRefresh();
        PersistWorkspaceSettingsInBackground();
    }

    private void StartEditableAreaDrag(PixelCoordinate anchor, EditableDragAction action, string statusMessage)
    {
        _editableDragAction = action;
        _editableDragAnchor = anchor;
        _editableDragOriginArea = new PixelAreaSelection(anchor, anchor);
        _editableDragPayload = null;
        _isDraggingEditableArea = true;
        _selectedArea = new PixelAreaSelection(anchor, anchor);
        SelectedAreaSummary = DescribeArea(_selectedArea.Value);
        EditorStatus = statusMessage;
        RefreshInteractionState();
    }

    private void StartEditableMoveDrag(
        PixelCoordinate anchor,
        PixelAreaSelection area,
        EditableDragAction action,
        string statusMessage)
    {
        _editableDragAction = action;
        _editableDragAnchor = anchor;
        _editableDragOriginArea = area;
        _editableDragPayload = CaptureEditablePayload(
            area,
            action is EditableDragAction.MoveSingle or EditableDragAction.MoveSelection);
        _isDraggingEditableArea = true;
        _selectedArea = area;
        _selectedEditableCoordinate = area.Start;
        SelectedAreaSummary = DescribeArea(area);
        EditorStatus = statusMessage;
        RefreshInteractionState();
    }

    private void CompleteEditableDrag(PixelCoordinate releasedCoordinate)
    {
        if (!_isDraggingEditableArea || _editableDragAnchor is null)
        {
            return;
        }

        switch (_editableDragAction)
        {
            case EditableDragAction.FillArea:
            case EditableDragAction.RestoreArea:
            case EditableDragAction.SelectArea:
                _selectedArea = new PixelAreaSelection(_editableDragAnchor.Value, releasedCoordinate);
                break;
            case EditableDragAction.MoveSingle:
            case EditableDragAction.MoveSelection:
                if (_editableDragOriginArea is { } originArea && ResolveEditorResolution() is { } resolution)
                {
                    var (deltaX, deltaY) = ComputeClampedAreaDelta(originArea, _editableDragAnchor.Value, releasedCoordinate, resolution);
                    _selectedArea = TranslateArea(originArea, deltaX, deltaY);
                }
                break;
        }

        var dragAction = _editableDragAction;
        var completedArea = _selectedArea;
        var payload = _editableDragPayload;
        var originAreaSnapshot = _editableDragOriginArea;

        ResetEditableDragState();

        switch (dragAction)
        {
            case EditableDragAction.FillArea:
                if (completedArea is not { } fillArea)
                {
                    RefreshInteractionState();
                    return;
                }

                if (_selectedSourceCoordinate is not { } selectedSource)
                {
                    ClearSelectedArea();
                    StatusMessage = "Pick a source pixel first.";
                    RefreshInteractionState();
                    return;
                }

                ClearSelectedArea();
                ApplySourcePixelToEditableArea(fillArea, selectedSource, "Filled the editable area from the selected source pixel.");
                break;
            case EditableDragAction.RestoreArea:
                if (completedArea is not { } restoreArea)
                {
                    RefreshInteractionState();
                    return;
                }

                ClearSelectedArea();
                ApplyRestoreOperation(restoreArea, "Restored the editable area to original source pixels.");
                break;
            case EditableDragAction.SelectArea:
                if (completedArea is not { } selectedArea)
                {
                    RefreshInteractionState();
                    return;
                }

                _selectedArea = selectedArea;
                _selectedEditableCoordinate = selectedArea.Start;
                SelectedAreaSummary = DescribeArea(selectedArea);
                EditorStatus = "Editable area selected. Drag inside it to move the mapped pixels.";
                RefreshInteractionState();
                break;
            case EditableDragAction.MoveSingle:
            case EditableDragAction.MoveSelection:
                if (payload is null || originAreaSnapshot is not { } originArea || completedArea is not { } movedArea)
                {
                    RefreshInteractionState();
                    return;
                }

                _selectedEditableCoordinate = movedArea.Start;
                ClearSelectedArea();
                ApplyMovedEditableArea(
                    payload,
                    originArea,
                    movedArea,
                    dragAction == EditableDragAction.MoveSingle
                        ? "Moved the editable pixel mapping."
                        : "Moved the selected editable area.");
                RefreshInteractionState();
                break;
            case EditableDragAction.None:
            default:
                RefreshInteractionState();
                break;
        }
    }

    private void ResetEditableDragState()
    {
        _editableDragAnchor = null;
        _editableDragOriginArea = null;
        _editableDragPayload = null;
        _editableDragAction = EditableDragAction.None;
        _isDraggingEditableArea = false;
        _restoreStrokeFlushCts?.Cancel();
        _restoreStrokeFlushCts?.Dispose();
        _restoreStrokeFlushCts = null;
        _pendingRestoreStrokeCoordinates.Clear();
        _hasPendingRestoreStrokeFinalize = false;
    }

    private void ClearSelectedArea()
    {
        _selectedArea = null;
        SelectedAreaSummary = "No area selected.";
    }

    private Dictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelCoordinate?>> CaptureEditablePayload(
        PixelAreaSelection area,
        bool includeIdentityFallback)
    {
        if (_editorSession.CurrentConfig is not { } config)
        {
            return [];
        }

        var payload = new Dictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelCoordinate?>>();
        var selectedDirection = GetSafeSelectedDirection();
        foreach (var direction in ResolveDirections(config.SupportedDirections))
        {
            var mappingsByEditable = config.GetMappings(direction).ToDictionary(static mapping => mapping.Source);
            var directionPayload = new Dictionary<PixelCoordinate, PixelCoordinate?>();
            foreach (var editableCoordinate in area.Enumerate())
            {
                var scopedEditableCoordinate = TransformEditableCoordinate(
                    editableCoordinate,
                    selectedDirection,
                    direction,
                    config.Resolution);

                if (mappingsByEditable.TryGetValue(scopedEditableCoordinate, out var explicitMapping))
                {
                    directionPayload[editableCoordinate] = explicitMapping.Target;
                    continue;
                }

                if (!includeIdentityFallback)
                {
                    continue;
                }

                var source = ResolveEffectiveSourceCoordinate(
                    direction,
                    scopedEditableCoordinate,
                    includeIdentityFallback: true);

                directionPayload[editableCoordinate] = source;
            }

            payload[direction] = directionPayload;
        }

        return payload;
    }

    private void ClearHoverState()
    {
        HoveredCanvasKind = null;
        SourceHoveredCoordinate = null;
        EditableHoveredCoordinate = null;
        SourceLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();
        EditableLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();
        HoverSummary = "Hover to inspect coordinates.";
        HoverMappingSummary = "No hover mapping.";
    }

    private void UpdateSourceHoverState(PixelCoordinate coordinate)
    {
        HoveredCanvasKind = EditorCanvasKind.Source;
        SourceHoveredCoordinate = coordinate;
        EditableHoveredCoordinate = null;
        SourceLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();
        HoverSummary = $"Source {coordinate}.";

        var linkedEditableCoordinates = ResolveEditableCoordinatesForSource(coordinate);
        EditableLinkedHoverCoordinates = linkedEditableCoordinates;
        if (linkedEditableCoordinates.Count > 0)
        {
            HoverMappingSummary = linkedEditableCoordinates.Count == 1
                ? $"Source {coordinate} -> Editable {linkedEditableCoordinates[0]}."
                : $"Source {coordinate} -> {linkedEditableCoordinates.Count} editable pixel(s).";
            return;
        }

        HoverMappingSummary = $"Source {coordinate} is not used in Editable.";
    }

    private void UpdateEditableHoverState(PixelCoordinate coordinate)
    {
        HoveredCanvasKind = EditorCanvasKind.Editable;
        EditableHoveredCoordinate = coordinate;
        SourceHoveredCoordinate = null;
        EditableLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();
        HoverSummary = $"Editable {coordinate}.";

        if (TryResolveSourceFromEditable(coordinate, out var sourceCoordinate))
        {
            SourceLinkedHoverCoordinates = [sourceCoordinate];
            HoverMappingSummary = $"Editable {coordinate} <- Source {sourceCoordinate}.";
            return;
        }

        SourceLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();
        HoverMappingSummary = $"Editable {coordinate} -> transparent.";
    }

    private IReadOnlyList<PixelCoordinate> ResolveEditableCoordinatesForSource(PixelCoordinate sourceCoordinate)
    {
        var resolution = ResolveEditorResolution();
        if (resolution is not { } bounds || !bounds.Contains(sourceCoordinate))
        {
            return Array.Empty<PixelCoordinate>();
        }

        var linked = new List<PixelCoordinate>();
        for (var y = 0; y < bounds.Height; y++)
        {
            for (var x = 0; x < bounds.Width; x++)
            {
                var editableCoordinate = new PixelCoordinate(x, y);
                if (ResolveSourceCoordinateForEditable(editableCoordinate) == sourceCoordinate)
                {
                    linked.Add(editableCoordinate);
                }
            }
        }

        return linked;
    }

    private bool TryResolveSourceFromEditable(PixelCoordinate editableCoordinate, out PixelCoordinate source)
    {
        source = default;

        if (ResolveSourceCoordinateForEditable(editableCoordinate) is not { } resolvedSource)
        {
            return false;
        }

        source = resolvedSource;
        return true;
    }

    private PixelCoordinate? ResolveSourceCoordinateForEditable(PixelCoordinate editableCoordinate)
        => ResolveSourceCoordinateForEditable(GetSafeSelectedDirection(), editableCoordinate);

    private PixelCoordinate? ResolveEffectiveSourceCoordinate(
        SpriteDirection direction,
        PixelCoordinate editableCoordinate,
        bool includeIdentityFallback)
    {
        if (_editorSession.CurrentConfig is null)
        {
            return editableCoordinate;
        }

        foreach (var mapping in _editorSession.CurrentConfig.GetMappings(direction))
        {
            if (mapping.Source == editableCoordinate)
            {
                return mapping.Target;
            }
        }

        if (!includeIdentityFallback)
        {
            return null;
        }

        var backingOrigins = ResolveEditableBackingOrigins(direction);
        if (backingOrigins.TryGetValue(editableCoordinate, out var backingOrigin))
        {
            return backingOrigin;
        }

        return editableCoordinate;
    }

    private PixelCoordinate? ResolveSourceCoordinateForEditable(SpriteDirection direction, PixelCoordinate editableCoordinate)
    {
        if (_editorSession.CurrentConfig is null)
        {
            return editableCoordinate;
        }

        foreach (var mapping in _editorSession.CurrentConfig.GetMappings(direction))
        {
            if (mapping.Source == editableCoordinate)
            {
                return mapping.Target;
            }
        }

        return editableCoordinate;
    }

    public async Task HandleBatchSourceSelectionAsync(BatchSourceTreeItemViewModel? item)
    {
        SelectedBatchSourceItem = item;
        SelectedBatchStateStripItem = null;
        BatchQuickPreviewOriginalImage = null;
        BatchQuickPreviewEditedImage = null;
        BatchCurrentFile = item is null
            ? string.Empty
            : item.IsDirectory
                ? item.FullPath
                : Path.GetFileName(item.FullPath);

        _selectedBatchPreviewAsset = null;
        if (item is { IsDirectory: false } &&
            item.FullPath.EndsWith(".dmi", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(item.FullPath))
        {
            var inspectResult = await _inspectDmiFileUseCase.ExecuteAsync(item.FullPath, CancellationToken.None);
            if (inspectResult.IsSuccess)
            {
                var currentResolution = ResolveEditorResolution();
                item.IsValid = currentResolution is null || inspectResult.Value.Resolution == currentResolution.Value;
                item.ValidationMessage = item.IsValid
                    ? string.Empty
                    : $"Resolution {inspectResult.Value.Resolution} does not match current {currentResolution!.Value}.";
                _selectedBatchPreviewAsset = inspectResult.Value;
            }
            else
            {
                item.IsValid = false;
                item.ValidationMessage = inspectResult.Error.Message;
            }
        }
        else if (item is not null)
        {
            item.IsValid = true;
            item.ValidationMessage = string.Empty;
        }

        RefreshBatchPipelineState(rebuildSourceTree: false);
    }

    [RelayCommand]
    private void BrowseDmiPath()
    {
        var path = _fileDialogService.OpenDmiFile(DmiPath);
        if (!string.IsNullOrWhiteSpace(path))
        {
            DmiPath = path;
        }
    }

    [RelayCommand]
    private void BrowseLoadConfigPath()
    {
        var path = _fileDialogService.OpenConfigFile(ConfigPath);
        if (!string.IsNullOrWhiteSpace(path))
        {
            ConfigPath = path;
        }
    }

    [RelayCommand]
    private void BrowseSaveConfigPath()
    {
        var path = _fileDialogService.SaveConfigFile(SaveConfigPath, DraftConfigName);
        if (!string.IsNullOrWhiteSpace(path))
        {
            SaveConfigPath = path;
            DraftConfigName = Path.GetFileNameWithoutExtension(path);
        }
    }

    [RelayCommand]
    private void BrowseLegacyCsvPath()
    {
        var path = _fileDialogService.OpenLegacyCsvFile(LegacyCsvPath);
        if (!string.IsNullOrWhiteSpace(path))
        {
            LegacyCsvPath = path;
        }
    }

    [RelayCommand]
    private void BrowseBatchInputDirectory()
    {
        var path = _fileDialogService.SelectDirectory("Select the folder with DMI files to process.", BatchInputDirectory);
        if (!string.IsNullOrWhiteSpace(path))
        {
            BatchInputDirectory = path;
        }
    }

    [RelayCommand]
    private void BrowseBatchOutputDirectory()
    {
        var path = _fileDialogService.SelectDirectory("Select the output folder for processed DMI files.", BatchOutputDirectory);
        if (!string.IsNullOrWhiteSpace(path))
        {
            BatchOutputDirectory = path;
        }
    }

    [RelayCommand]
    private async Task OpenDmiAsync()
    {
        var path = _fileDialogService.OpenDmiFile(DmiPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "Choose a DMI file to load.";
            return;
        }

        DmiPath = path;
        await RunBusyOperationAsync(
            cancellationToken => OpenDmiFromPathAsync(DmiPath, navigateToEditor: true, persistSettings: true, cancellationToken));
    }

    [RelayCommand]
    private async Task AddDmiStatesAsync()
    {
        var path = _fileDialogService.OpenDmiFile(DmiPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "Choose a DMI file to add states from.";
            return;
        }

        if (_editorSession.LoadedAsset is null)
        {
            DmiPath = path;
            await RunBusyOperationAsync(
                cancellationToken => OpenDmiFromPathAsync(DmiPath, navigateToEditor: true, persistSettings: true, cancellationToken));
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var result = await _inspectDmiFileUseCase.ExecuteAsync(path, cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                if (!CanMergeImportedStates(result.Value, out var resolutionError))
                {
                    StatusMessage = resolutionError;
                    return;
                }

                await MergeImportedStatesFromAssetAsync(result.Value, cancellationToken);
                StatusMessage = $"Merged {result.Value.States.Count} state(s) from '{result.Value.DisplayName}'.";
                RefreshEditorSurface();
                PersistWorkspaceSettingsInBackground();
            });
    }

    private bool CanMergeImportedStates(DmiAssetInfo asset, out string message)
    {
        var baseline = ResolveEditorResolution();
        if (baseline is null || asset.Resolution == baseline.Value)
        {
            message = string.Empty;
            return true;
        }

        message =
            $"Cannot add states from '{asset.DisplayName}': resolution {asset.Resolution} does not match current {baseline.Value}.";
        return false;
    }

    [RelayCommand(CanExecute = nameof(CanCreateConfig))]
    private void CreateConfig()
    {
        SyncCurrentConfigIntoActiveQueueItem();

        var requestedName = _editorSession.CurrentConfig is null
            ? (string.IsNullOrWhiteSpace(DraftConfigName) ? "Unsaved Draft" : DraftConfigName.Trim())
            : GenerateNextDraftConfigName();
        var metadata = ConfigMetadata.CreateNew(ConfigSource.UserCreated, sourceIdentifier: "presentation-shell");
        var result = _createConfigUseCase.Execute(
            requestedName,
            metadata);

        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        ConfigPath = string.Empty;
        SaveConfigPath = string.Empty;
        LegacyCsvPath = string.Empty;
        DraftConfigName = result.Value.Name;
        UpsertCurrentSessionIntoConfigQueue(forceAddNewItem: true);
        StatusMessage = $"Created config '{result.Value.Name}'.";
        RefreshWorkspaceState();
        ApplyAdaptiveEditorZoom(force: true);
        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();
        NavigateToSection(ShellSectionKind.Editor);
        RequestAutoPreviewRefresh();
        PersistWorkspaceSettingsInBackground();
    }

    [RelayCommand(CanExecute = nameof(CanSaveConfig))]
    private async Task SaveConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(SaveConfigPath))
        {
            BrowseSaveConfigPath();
        }

        if (string.IsNullOrWhiteSpace(SaveConfigPath))
        {
            StatusMessage = "Choose a JSON config path to save.";
            return;
        }

        var desiredConfigName = Path.GetFileNameWithoutExtension(SaveConfigPath);
        if (!string.IsNullOrWhiteSpace(desiredConfigName) &&
            _editorSession.CurrentConfig is not null &&
            !string.Equals(_editorSession.CurrentConfig.Name, desiredConfigName, StringComparison.Ordinal))
        {
            var renameResult = _editorSession.RenameCurrentConfig(desiredConfigName);
            if (renameResult.IsFailure)
            {
                StatusMessage = renameResult.Error.Message;
                return;
            }

            DraftConfigName = desiredConfigName;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var result = await _saveConfigUseCase.ExecuteAsync(SaveConfigPath, cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                ConfigPath = SaveConfigPath;
                UpsertCurrentSessionIntoConfigQueue();
                StatusMessage = $"Saved config to '{SaveConfigPath}'.";
                RefreshWorkspaceState();
                await PersistWorkspaceSettingsAsync(cancellationToken);
            });
    }

    [RelayCommand(CanExecute = nameof(CanSaveConfig))]
    private async Task SaveConfigAsAsync()
    {
        var path = _fileDialogService.SaveConfigFile(SaveConfigPath, DraftConfigName);
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "Choose a JSON config path to save.";
            return;
        }

        SaveConfigPath = path;
        DraftConfigName = Path.GetFileNameWithoutExtension(path);
        await SaveConfigAsync();
    }

    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        SyncCurrentConfigIntoActiveQueueItem();
        DeactivateConfigQueueSelection();

        var path = _fileDialogService.OpenConfigFile(ConfigPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "Choose a JSON config file to load.";
            return;
        }

        ConfigPath = path;
        await RunBusyOperationAsync(
            cancellationToken => LoadConfigFromPathAsync(ConfigPath, navigateToEditor: true, persistSettings: true, cancellationToken));
    }

    [RelayCommand]
    private async Task ImportLegacyConfigAsync()
    {
        SyncCurrentConfigIntoActiveQueueItem();
        DeactivateConfigQueueSelection();

        var path = _fileDialogService.OpenLegacyCsvFile(LegacyCsvPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "Choose a legacy CSV config to import.";
            return;
        }

        LegacyCsvPath = path;
        await RunBusyOperationAsync(
            cancellationToken => ImportLegacyConfigFromPathAsync(LegacyCsvPath, navigateToEditor: true, persistSettings: true, cancellationToken));
    }

    [RelayCommand]
    private async Task ActivateSampleConfigAsync(SampleConfigItemViewModel? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Path) || !File.Exists(item.Path))
        {
            StatusMessage = "Sample config file is unavailable.";
            return;
        }

        SyncCurrentConfigIntoActiveQueueItem();
        DeactivateConfigQueueSelection();

        await RunBusyOperationAsync(
            cancellationToken => item.IsLegacyCsv
                ? ImportLegacyConfigFromPathAsync(item.Path, navigateToEditor: true, persistSettings: true, cancellationToken, item.Name)
                : LoadConfigFromPathAsync(item.Path, navigateToEditor: true, persistSettings: true, cancellationToken, item.Name));
    }

    private async Task OpenDmiFromPathAsync(string path, bool navigateToEditor, bool persistSettings, CancellationToken cancellationToken)
    {
        var result = await _loadDmiFileUseCase.ExecuteAsync(path, cancellationToken);
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        DmiPath = path;
        BaseStateName =
            result.Value.States.FirstOrDefault(static state => string.Equals(state.Name, "human32x", StringComparison.OrdinalIgnoreCase))?.Name
            ?? result.Value.States.FirstOrDefault()?.Name
            ?? string.Empty;
        LandmarkStateName = string.Empty;
        OverlayStateName = string.Empty;
        DraftConfigName = Path.GetFileNameWithoutExtension(result.Value.DisplayName);
        ConfigPath = string.Empty;
        SaveConfigPath = string.Empty;
        LegacyCsvPath = string.Empty;
        BatchInputDirectory = Path.GetDirectoryName(result.Value.SourcePath ?? path) ?? string.Empty;
        BatchOutputDirectory = string.IsNullOrWhiteSpace(BatchOutputDirectory)
            ? Path.Combine(BatchInputDirectory, "processed")
            : BatchOutputDirectory;
        ConfigQueueItems.Clear();
        _activeConfigQueueItemId = null;
        CreateImplicitDraftConfig(forceNewQueueItem: true);
        await MergeImportedStatesFromAssetAsync(result.Value, cancellationToken);
        NormalizeSelectedDirection();
        StatusMessage = $"Loaded DMI '{result.Value.DisplayName}' with {result.Value.States.Count} states.";
        RefreshWorkspaceState();
        ApplyAdaptiveEditorZoom(force: true);
        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();

        if (navigateToEditor)
        {
            NavigateToSection(ShellSectionKind.Editor);
        }

        if (_editorSession.CurrentConfig is not null)
        {
            await TryBuildPreviewAsync(userInitiated: false, cancellationToken);
        }
        else
        {
            ClearPreviewArtifacts();
        }

        if (persistSettings)
        {
            await PersistWorkspaceSettingsAsync(cancellationToken);
        }
    }

    private async Task MergeImportedStatesFromAssetAsync(DmiAssetInfo asset, CancellationToken cancellationToken)
    {
        foreach (var state in asset.States.OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var previewResult = await _readStateFrameUseCase.ExecuteAsync(
                asset.SourcePath ?? string.Empty,
                state.Name,
                SpriteDirection.South,
                cancellationToken);
            _importedStateFrameCache[(asset.SourcePath ?? string.Empty, state.Name, SpriteDirection.South)] =
                previewResult.IsSuccess ? previewResult.Value : null;
            var previewBitmap = previewResult.IsSuccess ? _bitmapSourceFactory.Create(previewResult.Value) : null;
            var isValid = previewResult.IsSuccess;
            var validationMessage = previewResult.IsSuccess ? string.Empty : previewResult.Error.Message;

            var existing = ImportedDmiStateItems.FirstOrDefault(item =>
                string.Equals(item.StateName, state.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                var order = ImportedDmiStateItems.Count == 0 ? 0 : ImportedDmiStateItems.Max(static item => item.Order) + 1;
                var imported = new ImportedDmiStateItemViewModel(
                    state.Name,
                    asset.SourcePath ?? string.Empty,
                    Path.GetFileName(asset.SourcePath ?? asset.DisplayName),
                    previewBitmap,
                    ImportedStatePlacementMode.None,
                    order);
                imported.IsValid = isValid;
                imported.ValidationMessage = validationMessage;
                AttachImportedStateItem(imported);
                ImportedDmiStateItems.Add(imported);
                continue;
            }

            existing.SourceFileLabel = Path.GetFileName(asset.SourcePath ?? asset.DisplayName);
            existing.PreviewImage = previewBitmap;
            existing.SourcePath = asset.SourcePath ?? string.Empty;
            existing.IsValid = isValid;
            existing.ValidationMessage = validationMessage;
        }

        OnPropertyChanged(nameof(ImportedDmiStateItems));
        InvalidateImportedStateFrameCache();
    }

    private void BeginBatchSourceTreeValidation()
    {
        _batchSourceValidationCts?.Cancel();
        _batchSourceValidationCts?.Dispose();
        _batchSourceValidationCts = new CancellationTokenSource();
        var validationVersion = ++_batchSourceValidationVersion;
        _ = ValidateBatchSourceTreeItemsAsync(validationVersion, _batchSourceValidationCts.Token);
    }

    private async Task ValidateBatchSourceTreeItemsAsync(int validationVersion, CancellationToken cancellationToken)
    {
        try
        {
            var resolution = ResolveEditorResolution();
            foreach (var item in EnumerateBatchSourceTreeItems(BatchSourceTreeItems))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (validationVersion != _batchSourceValidationVersion)
                {
                    return;
                }

                if (item.IsDirectory)
                {
                    item.IsValid = true;
                    item.ValidationMessage = string.Empty;
                    continue;
                }

                var inspectResult = await _inspectDmiFileUseCase.ExecuteAsync(item.FullPath, cancellationToken);
                if (validationVersion != _batchSourceValidationVersion)
                {
                    return;
                }

                if (inspectResult.IsFailure)
                {
                    item.IsValid = false;
                    item.ValidationMessage = inspectResult.Error.Message;
                    continue;
                }

                item.IsValid = resolution is null || inspectResult.Value.Resolution == resolution.Value;
                item.ValidationMessage = item.IsValid
                    ? string.Empty
                    : $"Resolution {inspectResult.Value.Resolution} does not match current {resolution!.Value}.";
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static IEnumerable<BatchSourceTreeItemViewModel> EnumerateBatchSourceTreeItems(IEnumerable<BatchSourceTreeItemViewModel> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var child in EnumerateBatchSourceTreeItems(item.Children))
            {
                yield return child;
            }
        }
    }

    private void CreateImplicitDraftConfig(bool forceNewQueueItem = false)
    {
        if (_editorSession.LoadedAsset is null)
        {
            return;
        }

        var metadata = ConfigMetadata.CreateNew(ConfigSource.UserCreated, sourceIdentifier: "presentation-shell:implicit-draft");
        var result = _createConfigUseCase.Execute(forceNewQueueItem ? GenerateNextDraftConfigName() : "Unsaved Draft", metadata);
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        DraftConfigName = result.Value.Name;
        ConfigPath = string.Empty;
        SaveConfigPath = string.Empty;
        LegacyCsvPath = string.Empty;
        UpsertCurrentSessionIntoConfigQueue(forceAddNewItem: forceNewQueueItem);
    }

    private async Task LoadConfigFromPathAsync(string path, bool navigateToEditor, bool persistSettings, CancellationToken cancellationToken, string? preferredDisplayName = null)
    {
        var result = await _loadConfigUseCase.ExecuteAsync(path, cancellationToken);
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        ConfigPath = path;
        SaveConfigPath = path;
        LegacyCsvPath = string.Empty;
        DraftConfigName = result.Value.Name;
        AdoptCurrentConfigDisplayName(preferredDisplayName);
        UpsertCurrentSessionIntoConfigQueue();
        StatusMessage = $"Loaded config '{DraftConfigName}'.";
        RefreshWorkspaceState();
        ApplyAdaptiveEditorZoom(force: true);
        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();

        if (navigateToEditor)
        {
            NavigateToSection(ShellSectionKind.Editor);
        }

        await TryBuildPreviewAsync(userInitiated: false, cancellationToken);

        if (persistSettings)
        {
            await PersistWorkspaceSettingsAsync(cancellationToken);
        }
    }

    private async Task ImportLegacyConfigFromPathAsync(string path, bool navigateToEditor, bool persistSettings, CancellationToken cancellationToken, string? preferredDisplayName = null)
    {
        var result = await _importLegacyCsvConfigUseCase.ExecuteAsync(path, cancellationToken);
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        LegacyCsvPath = path;
        ConfigPath = string.Empty;
        DraftConfigName = result.Value.Name;
        SaveConfigPath = string.Empty;
        AdoptCurrentConfigDisplayName(preferredDisplayName);
        UpsertCurrentSessionIntoConfigQueue();
        StatusMessage = $"Imported legacy CSV '{Path.GetFileName(path)}' as config '{DraftConfigName}'.";
        RefreshWorkspaceState();
        ApplyAdaptiveEditorZoom(force: true);
        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();

        if (navigateToEditor)
        {
            NavigateToSection(ShellSectionKind.Editor);
        }

        await TryBuildPreviewAsync(userInitiated: false, cancellationToken);

        if (persistSettings)
        {
            await PersistWorkspaceSettingsAsync(cancellationToken);
        }
    }
}
