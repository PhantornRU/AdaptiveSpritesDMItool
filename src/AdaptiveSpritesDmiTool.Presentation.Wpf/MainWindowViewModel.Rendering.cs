using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class WorkspaceShellViewModel
{
    private static readonly Brush MappedBrush = CreateBrush(Color.FromRgb(198, 224, 206));
    private static readonly Brush TransparentBrush = CreateBrush(Color.FromRgb(245, 210, 197));
    private static readonly Brush AreaBrush = CreateBrush(Color.FromRgb(214, 224, 238));
    private static readonly Brush SelectedBrush = CreateBrush(Color.FromRgb(30, 92, 84));
    private static readonly Brush NeutralBrush = CreateBrush(Color.FromRgb(244, 239, 231));
    private static readonly Brush GridBrush = CreateBrush(Color.FromRgb(217, 207, 192));
    private static readonly Color MappedColor = Color.FromRgb(198, 224, 206);
    private static readonly Color TransparentColor = Color.FromRgb(245, 210, 197);
    private static readonly Color NeutralColor = Color.FromRgb(244, 239, 231);

    private bool CanCreateConfig() => _editorSession.LoadedAsset is not null && !IsBusy;

    private bool CanEditConfig() => _editorSession.CurrentConfig is not null && !IsBusy;

    private bool CanSaveConfig() => CanEditConfig();

    private bool CanBuildPreview() => HasEditorWorkflow && !IsBusy;

    private bool CanRunBatch() => CanEditConfig() && !IsBusy;

    private bool CanUndo() => _editorSession.CanUndo && !IsBusy;

    private bool CanRedo() => _editorSession.CanRedo && !IsBusy;

    private bool CanCancel() => IsBusy;

    private SpriteResolution? ResolveEditorResolution() => _editorSession.CurrentConfig?.Resolution ?? _editorSession.LoadedAsset?.Resolution;

    private double DetermineEditorZoomBaseline(SpriteResolution? resolution = null)
    {
        var resolved = resolution ?? ResolveEditorResolution();
        if (resolved is null)
        {
            return 2.0;
        }

        var maxDimension = Math.Max(resolved.Value.Width, resolved.Value.Height);
        var zoom = maxDimension switch
        {
            <= 32 => 3.0,
            <= 64 => 2.0,
            <= 128 => 1.5,
            _ => 1.0
        };

        return Math.Clamp(zoom, _minEditorZoom, MaxEditorZoom);
    }

    private double DetermineAdaptiveEditorZoom(SpriteResolution? resolution = null)
        => DetermineEditorZoomBaseline(resolution);

    private void ApplyAdaptiveEditorZoom(bool force)
    {
        var nextZoom = DetermineAdaptiveEditorZoom();
        if (!force && Math.Abs(nextZoom - ActiveEditorZoom) < 0.001d)
        {
            return;
        }

        ActiveEditorZoom = nextZoom;
    }

    private void ApplyWorkspaceSettings(WorkspaceSettings settings)
    {
        DmiPath = settings.LastOpenedDmiPath ?? string.Empty;
        ConfigPath = settings.LastOpenedConfigPath ?? string.Empty;
        SaveConfigPath = settings.LastOpenedConfigPath ?? string.Empty;
        LegacyCsvPath = settings.LastImportedLegacyCsvPath ?? string.Empty;
        BatchInputDirectory = settings.LastInputDirectory ?? string.Empty;
        BatchOutputDirectory = settings.LastOutputDirectory ?? string.Empty;
        DraftConfigName = settings.LastDraftConfigName ?? DraftConfigName;
        BaseStateName = settings.LastBaseState ?? string.Empty;
        LandmarkStateName = settings.LastLandmarkState ?? string.Empty;
        OverlayStateName = settings.LastOverlayState ?? string.Empty;
        SelectedDirection = settings.LastSelectedDirection ?? SelectedDirection;
        SelectedOverwritePolicy = settings.LastOverwritePolicy;
        SelectedThemeMode = ParseThemeMode(settings.LastThemeMode);
        App.ApplyThemeMode(SelectedThemeMode);
        SelectedEditorViewportMode = ParseEditorViewportMode(settings.LastEditorViewportMode);
        EditorViewMode = EditorViewMode.CompareSplit;
        SelectedBottomWorkspaceTab = ParseBottomWorkspaceTab(settings.LastBottomWorkspaceTab);
        IsPreviewInspectorExpanded = settings.IsPreviewInspectorExpanded;
        IsBottomWorkspaceExpanded = settings.IsBottomWorkspaceExpanded;
        IsFocusMode = false;
    }

    private async Task RestoreWorkspaceAsync()
    {
        if (!string.IsNullOrWhiteSpace(DmiPath) && File.Exists(DmiPath))
        {
            var dmiResult = await _loadDmiFileUseCase.ExecuteAsync(DmiPath, CancellationToken.None);
            if (dmiResult.IsSuccess)
            {
                BaseStateName = ResolveStateOrFallback(dmiResult.Value, BaseStateName);
                LandmarkStateName = ResolveOptionalState(dmiResult.Value, LandmarkStateName);
                OverlayStateName = ResolveOptionalState(dmiResult.Value, OverlayStateName);
                NormalizeSelectedDirection();
            }
            else
            {
                _logger.LogWarning("Startup DMI restore failed: {Message}", dmiResult.Error.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(ConfigPath) && File.Exists(ConfigPath))
        {
            var configResult = await _loadConfigUseCase.ExecuteAsync(ConfigPath, CancellationToken.None);
            if (configResult.IsFailure)
            {
                _logger.LogWarning("Startup config restore failed: {Message}", configResult.Error.Message);
            }
        }
        else if (!string.IsNullOrWhiteSpace(LegacyCsvPath) && File.Exists(LegacyCsvPath))
        {
            var legacyResult = await _importLegacyCsvConfigUseCase.ExecuteAsync(LegacyCsvPath, CancellationToken.None);
            if (legacyResult.IsFailure)
            {
                _logger.LogWarning("Startup legacy config restore failed: {Message}", legacyResult.Error.Message);
            }
        }

        if (_editorSession.LoadedAsset is not null && _editorSession.CurrentConfig is null)
        {
            CreateImplicitDraftConfig(forceNewQueueItem: true);
        }

        if (_editorSession.LoadedAsset is not null && _editorSession.CurrentConfig is not null && !string.IsNullOrWhiteSpace(BaseStateName))
        {
            await TryBuildPreviewAsync(userInitiated: false, CancellationToken.None);
        }
    }

    private WorkspaceSettings BuildWorkspaceSettings() =>
        new(
            NormalizeOptionalPath(DmiPath) ?? _editorSession.LoadedAsset?.SourcePath,
            NormalizeOptionalPath(ConfigPath) ?? _editorSession.CurrentConfigPath,
            NormalizeOptionalPath(LegacyCsvPath),
            NormalizeOptionalPath(BatchInputDirectory),
            NormalizeOptionalPath(BatchOutputDirectory),
            string.IsNullOrWhiteSpace(DraftConfigName) ? null : DraftConfigName.Trim(),
            string.IsNullOrWhiteSpace(BaseStateName) ? null : BaseStateName.Trim(),
            string.IsNullOrWhiteSpace(LandmarkStateName) ? null : LandmarkStateName.Trim(),
            string.IsNullOrWhiteSpace(OverlayStateName) ? null : OverlayStateName.Trim(),
            GetSafeSelectedDirection(),
            SelectedOverwritePolicy,
            SelectedThemeMode.ToString(),
            SelectedEditorViewportMode.ToString(),
            SelectedBottomWorkspaceTab.ToString(),
            IsPreviewInspectorExpanded,
            IsBottomWorkspaceExpanded);

    private void ResetWorkspaceCore()
    {
        _previewRefreshCoordinator.Cancel();
        var result = _startEmptyWorkspaceUseCase.Execute();
        StatusMessage = result.IsSuccess
            ? "Ready."
            : result.Error.Message;

        _selectedSourceCoordinate = null;
        _selectedEditableCoordinate = null;
        _selectedArea = null;
        ResetEditableDragState();
        _baseImage = null;
        _landmarkImage = null;
        _overlayImage = null;
        _compositeImage = null;
        ClearImportedStateItems();
        DmiPath = string.Empty;
        ConfigPath = string.Empty;
        SaveConfigPath = string.Empty;
        LegacyCsvPath = string.Empty;
        DraftConfigName = "Unsaved Draft";
        BaseStateName = string.Empty;
        LandmarkStateName = string.Empty;
        OverlayStateName = string.Empty;
        CurrentStateSummary = "No DMI state selected yet.";
        EditorStatus = "Edit mappings directly in the matrix.";
        SelectedSourceSummary = "Source: none selected.";
        SelectedAreaSummary = "Area: none selected.";
        HoverSummary = "Hover to inspect coordinates.";
        BatchSummary = "Batch processing is idle.";
        BatchCurrentFile = string.Empty;
        BatchProcessedFiles = 0;
        BatchTotalFiles = 0;
        OperationProgressValue = 0;
        OperationProgressMaximum = 1;
        IsProgressIndeterminate = false;
        SelectedEditorViewportMode = EditorViewportMode.Matrix;
        EditorViewMode = EditorViewMode.CompareSplit;
        SelectedBottomWorkspaceTab = BottomWorkspaceTab.Mappings;
        SelectedEditorLeftDockTab = EditorLeftDockTab.AssetsDmi;
        SelectedEditorAssetTargetSurface = EditorAssetTargetSurface.Source;
        SelectedEditorAssetTargetLayer = EditorAssetTargetLayer.Base;
        IsBottomWorkspaceExpanded = true;
        IsPreviewInspectorExpanded = false;
        IsFocusMode = false;
        MirrorAcrossDirections = true;
        UseCentralizedPropagation = true;
        SelectedBatchSourceItem = null;
        _selectedBatchPreviewAsset = null;
        FocusedDirectionTile = null;
        DirectionMatrixColumns = 2;
        ActiveEditorZoom = 2.0;
        SourceHoveredCoordinate = null;
        EditableHoveredCoordinate = null;
        SourceLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();
        EditableLinkedHoverCoordinates = Array.Empty<PixelCoordinate>();
        HoveredCanvasKind = null;
        HoverMappingSummary = "No hover mapping.";
        SelectedSourceCoordinateView = null;
        SelectedTargetCoordinate = null;
        SelectedAreaBounds = null;
        ActiveSourceSurface = null;
        ActiveTargetSurface = null;
        InvalidateNavigatorSnapshotCache();
        BatchResults.Clear();
        ConfigQueueItems.Clear();
        SampleConfigItems.Clear();
        EditorAssetItems.Clear();
        BatchStateStripItems.Clear();
        BatchSourceTreeItems.Clear();
        SelectedBatchStateStripItem = null;
        DirectionNavigatorItems.Clear();
        DirectionTiles.Clear();
        ClearPreviewArtifacts();
        SelectedShellSection = ShellSectionKind.Start;
    }

    private async Task RunBusyOperationAsync(Func<CancellationToken, Task> operation)
    {
        if (IsBusy)
        {
            StatusMessage = "Wait for the current operation to finish or cancel it first.";
            return;
        }

        using var cancellationSource = new CancellationTokenSource();
        _previewRefreshCoordinator.Cancel();
        _activeOperationCts = cancellationSource;
        IsBusy = true;
        IsProgressIndeterminate = true;

        try
        {
            await operation(cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
            BatchSummary = "Batch processing was cancelled.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Presentation workflow failed.");
            StatusMessage = $"Unexpected error: {exception.Message}";
        }
        finally
        {
            _activeOperationCts = null;
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private async Task RefreshPreviewNowAsync(bool userInitiated, CancellationToken cancellationToken)
    {
        await _previewRefreshCoordinator.RefreshNowAsync(token => TryBuildPreviewAsync(userInitiated, token));
    }

    private void RequestAutoPreviewRefresh()
    {
        if (AutoPreviewMode != AutoPreviewMode.Enabled || !CanAttemptPreviewRefresh())
        {
            RequestBatchQuickPreviewRefresh();
            return;
        }

        _previewRefreshCoordinator.Request(token => TryBuildPreviewAsync(userInitiated: false, token));
        RequestBatchQuickPreviewRefresh();
    }

    private bool CanAttemptPreviewRefresh() =>
        !IsBusy &&
        _editorSession.LoadedAsset is not null &&
        _editorSession.CurrentConfig is not null &&
        !string.IsNullOrWhiteSpace(BaseStateName);

    private async Task TryBuildPreviewAsync(bool userInitiated, CancellationToken cancellationToken)
    {
        if (_editorSession.LoadedAsset is null || _editorSession.CurrentConfig is null)
        {
            PreviewSummary = "Load both a DMI and a config before building preview.";
            RefreshActivePreviewPresentation();
            return;
        }

        IsPreviewRefreshing = true;

        try
        {
            var selectionResult = _setPreviewSelectionUseCase.Execute(BaseStateName, LandmarkStateName, OverlayStateName);
            if (selectionResult.IsFailure)
            {
                ClearPreviewArtifacts();
                PreviewSummary = selectionResult.Error.Message;
                if (userInitiated)
                {
                    StatusMessage = selectionResult.Error.Message;
                }
                return;
            }

            var direction = GetSafeSelectedDirection();
            var selection = new PreviewSelection(
                BaseStateName,
                string.IsNullOrWhiteSpace(LandmarkStateName) ? null : LandmarkStateName.Trim(),
                string.IsNullOrWhiteSpace(OverlayStateName) ? null : OverlayStateName.Trim());

            var result = await _buildPreviewUseCase.ExecuteAsync(selection, direction, cancellationToken);
            if (result.IsFailure)
            {
                ClearPreviewArtifacts();
                PreviewSummary = result.Error.Message;
                if (userInitiated)
                {
                    StatusMessage = result.Error.Message;
                }
                RefreshActivePreviewPresentation();
                return;
            }

            _baseImage = result.Value.BaseImage;
            _landmarkImage = result.Value.LandmarkImage;
            _overlayImage = result.Value.OverlayImage;
            _compositeImage = result.Value.CompositeImage;
            _navigatorBaseImages.Clear();
            _navigatorCompositeImages.Clear();
            _navigatorBaseImages[direction] = result.Value.BaseImage;
            _navigatorCompositeImages[direction] = result.Value.CompositeImage;

            foreach (var previewDirection in AvailableDirections.Where(candidate => candidate != direction))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var previewResult = await _buildPreviewUseCase.ExecuteAsync(selection, previewDirection, cancellationToken);
                if (previewResult.IsFailure)
                {
                    continue;
                }

                _navigatorBaseImages[previewDirection] = previewResult.Value.BaseImage;
                _navigatorCompositeImages[previewDirection] = previewResult.Value.CompositeImage;
            }

            InvalidateNavigatorSnapshotCache();
            PreviewSummary =
                $"Preview built for direction {direction}. " +
                $"Landmark {(result.Value.LandmarkImage is null ? "missing or not selected" : "available")}, " +
                $"overlay {(result.Value.OverlayImage is null ? "missing or not selected" : "available")}.";
            if (userInitiated)
            {
                StatusMessage = "Preview refreshed.";
            }
            RefreshPreviewSelectionSummary();
            RefreshActivePreviewPresentation();
            RefreshEditorSurface();
        }
        catch (OperationCanceledException)
        {
            if (userInitiated)
            {
                PreviewSummary = "Preview refresh cancelled.";
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Preview refresh failed.");
            PreviewSummary = "Preview refresh failed.";
            if (userInitiated)
            {
                StatusMessage = $"Preview refresh failed: {exception.Message}";
            }
        }
        finally
        {
            IsPreviewRefreshing = false;
        }
    }

    private void ApplySourcePixelToEditable(PixelCoordinate editableCoordinate, PixelCoordinate sourceCoordinate, string successMessage)
    {
        var result = _applyConfigTransformUseCase.Execute(config => ApplyScopedMapping(config, editableCoordinate, sourceCoordinate));
        ApplyMutationResult(result, successMessage);
    }

    private void ApplySourcePixelToEditableArea(PixelAreaSelection editableArea, PixelCoordinate sourceCoordinate, string successMessage)
    {
        var result = _applyConfigTransformUseCase.Execute(config =>
        {
            var next = config;
            foreach (var editableCoordinate in editableArea.Enumerate())
            {
                next = ApplyScopedMapping(next, editableCoordinate, sourceCoordinate);
            }

            return next;
        });

        ApplyMutationResult(result, successMessage);
    }

    private void ApplyMovedEditableArea(
        IReadOnlyDictionary<SpriteDirection, Dictionary<PixelCoordinate, PixelCoordinate?>> payload,
        PixelAreaSelection originArea,
        PixelAreaSelection destinationArea,
        string successMessage)
    {
        var deltaX = destinationArea.Left - originArea.Left;
        var deltaY = destinationArea.Top - originArea.Top;

        var result = _applyConfigTransformUseCase.Execute(config =>
        {
            var next = config;
            var selectedDirection = GetSafeSelectedDirection();
            foreach (var (direction, directionPayload) in payload)
            {
                if (!config.SupportedDirections.Supports(direction))
                {
                    continue;
                }

                var moves = directionPayload
                    .Select(entry =>
                    {
                        var destinationEditableCoordinate = ClampCoordinate(
                            new PixelCoordinate(entry.Key.X + deltaX, entry.Key.Y + deltaY),
                            config.Resolution);

                        return (
                            Origin: TransformEditableCoordinate(entry.Key, selectedDirection, direction, config.Resolution),
                            Destination: TransformEditableCoordinate(destinationEditableCoordinate, selectedDirection, direction, config.Resolution),
                            Source: entry.Value);
                    })
                    .ToArray();

                foreach (var move in moves)
                {
                    next = next.RemoveMapping(direction, move.Origin);
                }

                foreach (var move in moves)
                {
                    next = next.SetMapping(direction, move.Destination, move.Source);
                }
            }

            return next;
        });

        ApplyMutationResult(result, successMessage);
    }

    private void ApplyTransparentOperation(PixelAreaSelection area, string successMessage)
    {
        var result = _applyConfigTransformUseCase.Execute(config =>
        {
            var next = config;
            foreach (var editableCoordinate in area.Enumerate())
            {
                next = ApplyScopedMapping(next, editableCoordinate, null);
            }

            return next;
        });

        ApplyMutationResult(result, successMessage);
    }

    private void ApplyRestoreOperation(PixelAreaSelection area, string successMessage)
    {
        var result = _applyConfigTransformUseCase.Execute(config =>
        {
            var next = config;
            foreach (var editableCoordinate in area.Enumerate())
            {
                next = ApplyScopedRestore(next, editableCoordinate);
            }

            return next;
        });

        ApplyMutationResult(result, successMessage);
    }

    private void ApplyRestoreOperations(
        IReadOnlyCollection<PixelCoordinate> editableCoordinates,
        string successMessage,
        bool refreshWorkspace,
        bool rebuildNavigator,
        bool refreshPreview)
    {
        if (editableCoordinates.Count == 0)
        {
            return;
        }

        var result = _applyConfigTransformUseCase.Execute(config =>
        {
            var next = config;
            foreach (var editableCoordinate in editableCoordinates)
            {
                next = ApplyScopedRestore(next, editableCoordinate);
            }

            return next;
        });

        ApplyMutationResult(
            result,
            successMessage,
            refreshWorkspace: refreshWorkspace,
            rebuildNavigator: rebuildNavigator,
            refreshPreview: refreshPreview);
    }

    private SpriteConfig ApplyScopedMapping(SpriteConfig config, PixelCoordinate editableCoordinate, PixelCoordinate? sourceCoordinate)
    {
        var next = config;
        var selectedDirection = GetSafeSelectedDirection();
        foreach (var direction in ResolveDirections(config.SupportedDirections))
        {
            var transformedEditable = TransformEditableCoordinate(editableCoordinate, selectedDirection, direction, config.Resolution);
            next = next.SetMapping(direction, transformedEditable, sourceCoordinate);
        }

        return next;
    }

    private SpriteConfig ApplyScopedRestore(SpriteConfig config, PixelCoordinate editableCoordinate)
    {
        var next = config;
        var selectedDirection = GetSafeSelectedDirection();
        foreach (var direction in ResolveDirections(config.SupportedDirections))
        {
            next = next.RemoveMapping(direction, TransformEditableCoordinate(editableCoordinate, selectedDirection, direction, config.Resolution));
        }

        return next;
    }

    private IReadOnlyList<SpriteDirection> ResolveDirections(SupportedDirectionSet supportedDirections)
    {
        var available = supportedDirections.GetDirections().ToArray();
        var selectedDirection = GetSafeSelectedDirection();
        return SelectedDirectionScope switch
        {
            DirectionScope.Single => [selectedDirection],
            DirectionScope.Parallel => ResolveParallelDirections(available, selectedDirection),
            DirectionScope.All => available,
            _ => [selectedDirection]
        };
    }

    private PixelCoordinate TransformEditableCoordinate(
        PixelCoordinate coordinate,
        SpriteDirection selectedDirection,
        SpriteDirection targetDirection,
        SpriteResolution resolution)
    {
        if (!ShouldMirrorAcrossDirections(selectedDirection, targetDirection))
        {
            return coordinate;
        }

        var mirroredX = (resolution.Width - coordinate.X - 1) + (UseCentralizedPropagation ? -1 : 0);
        return ClampCoordinate(new PixelCoordinate(mirroredX, coordinate.Y), resolution);
    }

    private static IReadOnlyList<SpriteDirection> ResolveParallelDirections(
        IReadOnlyCollection<SpriteDirection> available,
        SpriteDirection selectedDirection)
    {
        var resolved = new List<SpriteDirection> { selectedDirection };
        var opposite = GetHorizontalOppositeDirection(selectedDirection);
        if (opposite is { } horizontalOpposite && available.Contains(horizontalOpposite))
        {
            resolved.Add(horizontalOpposite);
        }

        return resolved;
    }

    private bool ShouldMirrorAcrossDirections(SpriteDirection selectedDirection, SpriteDirection targetDirection)
    {
        if (!MirrorAcrossDirections || selectedDirection == targetDirection)
        {
            return false;
        }

        if (!BelongsToSameDirectionFamily(selectedDirection, targetDirection))
        {
            return false;
        }

        return !IsVerticalOppositeDirection(selectedDirection, targetDirection);
    }

    private static bool BelongsToSameDirectionFamily(SpriteDirection left, SpriteDirection right) =>
        IsCardinalDirection(left) == IsCardinalDirection(right);

    private static bool IsCardinalDirection(SpriteDirection direction) =>
        direction is SpriteDirection.South or SpriteDirection.North or SpriteDirection.East or SpriteDirection.West;

    private static bool IsVerticalOppositeDirection(SpriteDirection selectedDirection, SpriteDirection targetDirection) =>
        GetVerticalOppositeDirection(selectedDirection) is { } opposite && opposite == targetDirection;

    private static SpriteDirection? GetHorizontalOppositeDirection(SpriteDirection direction) =>
        direction switch
        {
            SpriteDirection.South => SpriteDirection.North,
            SpriteDirection.North => SpriteDirection.South,
            SpriteDirection.East => SpriteDirection.West,
            SpriteDirection.West => SpriteDirection.East,
            SpriteDirection.SouthEast => SpriteDirection.NorthWest,
            SpriteDirection.NorthWest => SpriteDirection.SouthEast,
            SpriteDirection.SouthWest => SpriteDirection.NorthEast,
            SpriteDirection.NorthEast => SpriteDirection.SouthWest,
            _ => null
        };

    private static SpriteDirection? GetVerticalOppositeDirection(SpriteDirection direction) =>
        direction switch
        {
            SpriteDirection.South => SpriteDirection.East,
            SpriteDirection.North => SpriteDirection.West,
            SpriteDirection.East => SpriteDirection.South,
            SpriteDirection.West => SpriteDirection.North,
            SpriteDirection.SouthEast => SpriteDirection.SouthWest,
            SpriteDirection.NorthWest => SpriteDirection.NorthEast,
            SpriteDirection.SouthWest => SpriteDirection.SouthEast,
            SpriteDirection.NorthEast => SpriteDirection.NorthWest,
            _ => null
        };

    private static PixelCoordinate ClampCoordinate(PixelCoordinate coordinate, SpriteResolution resolution) =>
        new(Math.Clamp(coordinate.X, 0, resolution.Width - 1), Math.Clamp(coordinate.Y, 0, resolution.Height - 1));

    private void ApplyMutationResult(
        Result<SpriteConfig> result,
        string successMessage,
        bool refreshWorkspace = true,
        bool rebuildNavigator = true,
        bool refreshPreview = true)
    {
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        StatusMessage = successMessage;
        NormalizeSelectedDirection();
        if (rebuildNavigator)
        {
            InvalidateNavigatorSnapshotCache();
        }

        if (refreshWorkspace)
        {
            RefreshWorkspaceState();
        }

        RefreshEditorSurface(
            rebuildNavigator: rebuildNavigator,
            rebuildPreviewGrid: refreshPreview,
            rebuildActivePreview: refreshPreview);

        if (refreshPreview)
        {
            RequestAutoPreviewRefresh();
        }

        PersistWorkspaceSettingsInBackground();
    }

    private void RefreshWorkspaceState()
    {
        var workspace = _editorSession.Workspace;
        WorkspaceTitle = workspace.IsEmpty ? "Empty workspace" : workspace.DisplayName ?? "Sprite workspace";
        CurrentDirectionText = _editorSession.SelectedDirection.ToString();

        AvailableDirections.Clear();
        foreach (var direction in GetPresentationDirectionOrder(_editorSession.LoadedAsset?.SupportedDirections ?? _editorSession.CurrentConfig?.SupportedDirections ?? SupportedDirectionSet.Four))
        {
            AvailableDirections.Add(direction);
        }
        DirectionNavigatorColumns = AvailableDirections.Count > 4 ? 4 : 2;

        AvailableStates.Clear();
        foreach (var state in (_editorSession.LoadedAsset?.States ?? Array.Empty<DmiStateInfo>())
                     .OrderBy(static state => state.Name, StringComparer.Ordinal))
        {
            AvailableStates.Add(state.Name);
        }

        DirectionMatrixColumns = AvailableDirections.Count > 4 ? 4 : 2;

        if (!string.IsNullOrWhiteSpace(SelectedExplorerState) && !AvailableStates.Contains(SelectedExplorerState))
        {
            SelectedExplorerState = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(SelectedExplorerState))
        {
            SelectedExplorerState = AvailableStates.FirstOrDefault(static state =>
                                        string.Equals(state, "human32x", StringComparison.OrdinalIgnoreCase))
                                    ?? AvailableStates.FirstOrDefault()
                                    ?? string.Empty;
        }

        if (_editorSession.LoadedAsset is { } asset)
        {
            SpriteContractSummary = $"{asset.Resolution} | {asset.SupportedDirections} directions | {asset.States.Count} states";
            CurrentStateSummary = string.IsNullOrWhiteSpace(BaseStateName)
                ? "Base state not selected."
                : $"Base {BaseStateName} | Landmark {NormalizeOptionalText(LandmarkStateName)} | Overlay {NormalizeOptionalText(OverlayStateName)}";
        }
        else
        {
            SpriteContractSummary = "No sprite loaded yet. Open a DMI to populate the workspace.";
            CurrentStateSummary = "No DMI state selected yet.";
        }

        if (_editorSession.CurrentConfig is { } config)
        {
            var mappingCount = config.Directions.Sum(direction => config.GetMappings(direction).Count);
            var storageLabel = string.IsNullOrWhiteSpace(_editorSession.CurrentConfigPath)
                ? "unsaved draft"
                : Path.GetFileName(_editorSession.CurrentConfigPath);
            ConfigSummary = $"{config.Name} | {mappingCount} mappings | {storageLabel}";
            WorkspaceNotes = "Editor workflow is active. Edit one direction in the center and use the right navigator to switch directions.";
        }
        else
        {
            ConfigSummary = "No config loaded";
            WorkspaceNotes = "Open a DMI, create or load a config, then move into the editor.";
        }

        RefreshConfigQueueItems();
        RefreshSampleConfigItems();
        RefreshEditorAssetItems();
        RefreshBatchPipelineState();

        OnPropertyChanged(nameof(HasLoadedAsset));
        OnPropertyChanged(nameof(HasActiveConfig));
        OnPropertyChanged(nameof(HasEditorWorkflow));
        OnPropertyChanged(string.Empty);
        RefreshCommandStates();
    }

    private void RefreshPreviewSelectionSummary()
    {
        var direction = GetSafeSelectedDirection();
        PreviewSelectionSummary = string.IsNullOrWhiteSpace(BaseStateName)
            ? $"Dir {direction}. Base state not selected."
            : $"Dir {direction}. Base {BaseStateName} | Landmark {NormalizeOptionalText(LandmarkStateName)} | Overlay {NormalizeOptionalText(OverlayStateName)}";
        OnPropertyChanged(string.Empty);
    }

    private void RefreshEditorSurface(
        bool rebuildNavigator = true,
        bool rebuildPreviewGrid = true,
        bool rebuildActivePreview = true)
    {
        NormalizeSelectedDirection();
        RefreshMappingRows();
        RefreshInteractionState();
        RebuildActiveSurfaceRenderStates();
        if (rebuildNavigator)
        {
            RebuildDirectionNavigatorItems();
        }

        if (rebuildPreviewGrid)
        {
            RebuildPreviewGridRows();
        }

        if (rebuildActivePreview)
        {
            RefreshActivePreviewPresentation();
        }
    }

    private void RebuildDirectionNavigatorItems()
    {
        DirectionNavigatorItems.Clear();
        var activeDirection = GetSafeSelectedDirection();
        var scopeDirections = SelectedDirectionScope switch
        {
            DirectionScope.Single => new HashSet<SpriteDirection>([activeDirection]),
            DirectionScope.Parallel => new HashSet<SpriteDirection>(ResolveParallelDirections(AvailableDirections.ToArray(), activeDirection)),
            DirectionScope.All => new HashSet<SpriteDirection>(AvailableDirections),
            _ => new HashSet<SpriteDirection>([activeDirection])
        };

        foreach (var direction in AvailableDirections)
        {
            var item = new DirectionNavigatorItemViewModel(direction)
            {
                IsActive = direction == activeDirection,
                IsScopeAffected = scopeDirections.Contains(direction),
                PreviewImage = BuildNavigatorPreviewImage(direction)
            };

            DirectionNavigatorItems.Add(item);
        }

        DirectionTiles.Clear();
        FocusedDirectionTile = null;

        OnPropertyChanged(nameof(DirectionNavigatorItems));
    }

    private BitmapSource? BuildNavigatorPreviewImage(SpriteDirection direction)
    {
        var cacheKey = (direction, ShowOverlay, NavigatorSnapshotVersion);
        if (NavigatorSnapshotCache.TryGetValue(cacheKey, out var cached))
        {
#if DEBUG
            Debug.WriteLine($"[NavigatorSnapshotCache] HIT direction={direction} overlay={ShowOverlay} version={NavigatorSnapshotVersion}");
#endif
            return cached;
        }

#if DEBUG
        Debug.WriteLine($"[NavigatorSnapshotCache] MISS direction={direction} overlay={ShowOverlay} version={NavigatorSnapshotVersion}");
#endif

        var surface = BuildEditableSurfaceRenderState(direction, useCompositeImage: ShowOverlay);
        if (surface is null)
        {
            return null;
        }

        var pixels = new byte[surface.Width * surface.Height * 4];
        for (var y = 0; y < surface.Height; y++)
        {
            for (var x = 0; x < surface.Width; x++)
            {
                var color = surface.FillColors[surface.GetIndex(x, y)];
                var offset = ((y * surface.Width) + x) * 4;
                pixels[offset] = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = color.A;
            }
        }

        var bitmap = BitmapSource.Create(surface.Width, surface.Height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, surface.Width * 4);
        bitmap.Freeze();
        NavigatorSnapshotCache[cacheKey] = bitmap;
        return bitmap;
    }

    private void InvalidateNavigatorSnapshotCache()
    {
        NavigatorSnapshotVersion++;
        NavigatorSnapshotCache.Clear();

#if DEBUG
        Debug.WriteLine($"[NavigatorSnapshotCache] INVALIDATE version={NavigatorSnapshotVersion}");
#endif
    }

    public void NavigateToSection(ShellSectionKind section)
    {
        if (section == ShellSectionKind.Editor && !EditorWorkspace.IsAvailable)
        {
            SelectedShellSection = ShellSectionKind.Start;
            return;
        }

        if (section == ShellSectionKind.Batch && !BatchWorkspace.IsAvailable)
        {
            SelectedShellSection = HasEditorWorkflow ? ShellSectionKind.Editor : ShellSectionKind.Start;
            return;
        }

        SelectedShellSection = section;
    }

    private SpriteDirection GetSafeSelectedDirection()
    {
        var supportedDirections = ResolveSelectedDirectionSupport();
        var preferredDirection = supportedDirections is not null && !supportedDirections.Supports(SelectedDirection)
            ? supportedDirections.GetDirections().First()
            : SelectedDirection;

        if (TryApplySelectedDirection(preferredDirection, refreshUi: false))
        {
            return _editorSession.SelectedDirection;
        }

        return supportedDirections?.Supports(_editorSession.SelectedDirection) == true
            ? _editorSession.SelectedDirection
            : supportedDirections?.GetDirections().First() ?? _editorSession.SelectedDirection;
    }

    private void NormalizeSelectedDirection() => GetSafeSelectedDirection();

    private SupportedDirectionSet? ResolveSelectedDirectionSupport() =>
        _editorSession.CurrentConfig?.SupportedDirections ?? _editorSession.LoadedAsset?.SupportedDirections;

    private bool TryApplySelectedDirection(SpriteDirection direction, bool refreshUi)
    {
        var result = _setSelectedDirectionUseCase.Execute(direction);
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            SynchronizeSelectedDirectionProperty(_editorSession.SelectedDirection);
            return false;
        }

        SynchronizeSelectedDirectionProperty(_editorSession.SelectedDirection);
        CurrentDirectionText = _editorSession.SelectedDirection.ToString();

        if (refreshUi)
        {
            RefreshWorkspaceState();
            RefreshPreviewSelectionSummary();
            RefreshEditorSurface();
        }

        return true;
    }

    private void SynchronizeSelectedDirectionProperty(SpriteDirection direction)
    {
        if (SelectedDirection == direction)
        {
            return;
        }

        _isSynchronizingSelectedDirection = true;
        try
        {
            SelectedDirection = direction;
        }
        finally
        {
            _isSynchronizingSelectedDirection = false;
        }
    }

    private static string ResolveStateOrFallback(DmiAssetInfo asset, string? preferredState)
    {
        if (!string.IsNullOrWhiteSpace(preferredState) &&
            asset.States.Any(state => string.Equals(state.Name, preferredState, StringComparison.OrdinalIgnoreCase)))
        {
            return preferredState.Trim();
        }

        return asset.States.FirstOrDefault()?.Name ?? string.Empty;
    }

    private static string ResolveOptionalState(DmiAssetInfo asset, string? preferredState)
    {
        if (!string.IsNullOrWhiteSpace(preferredState) &&
            asset.States.Any(state => string.Equals(state.Name, preferredState, StringComparison.OrdinalIgnoreCase)))
        {
            return preferredState.Trim();
        }

        return string.Empty;
    }

    private static EditorViewportMode ParseEditorViewportMode(string? value) =>
        WorkspaceEnumParsing.ParseDefinedEnumOrDefault(value, EditorViewportMode.Matrix);

    private static BottomWorkspaceTab ParseBottomWorkspaceTab(string? value) =>
        WorkspaceEnumParsing.ParseDefinedEnumOrDefault(value, BottomWorkspaceTab.Mappings);

    private static WorkspaceThemeMode ParseThemeMode(string? value) =>
        WorkspaceEnumParsing.ParseDefinedEnumOrDefault(value, WorkspaceThemeMode.Dark);

    private static IReadOnlyList<SpriteDirection> GetPresentationDirectionOrder(SupportedDirectionSet supportedDirections)
    {
        var orderedDirections = new[]
        {
            SpriteDirection.South,
            SpriteDirection.North,
            SpriteDirection.East,
            SpriteDirection.West,
            SpriteDirection.SouthEast,
            SpriteDirection.NorthWest,
            SpriteDirection.SouthWest,
            SpriteDirection.NorthEast
        };

        return orderedDirections.Where(supportedDirections.Supports).ToArray();
    }
}
