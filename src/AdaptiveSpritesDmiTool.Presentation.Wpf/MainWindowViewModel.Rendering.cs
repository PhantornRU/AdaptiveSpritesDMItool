using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
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

    private bool CanCreateConfig() => _editorSession.LoadedAsset is not null && !IsBusy;

    private bool CanEditConfig() => _editorSession.CurrentConfig is not null && !IsBusy;

    private bool CanSaveConfig() => CanEditConfig();

    private bool CanBuildPreview() => HasEditorWorkflow && !IsBusy;

    private bool CanRunBatch() => CanEditConfig() && !IsBusy;

    private bool CanUndo() => _editorSession.CanUndo && !IsBusy;

    private bool CanRedo() => _editorSession.CanRedo && !IsBusy;

    private bool CanCancel() => IsBusy;

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
            SelectedOverwritePolicy);

    private void ResetWorkspaceCore()
    {
        _previewRefreshCoordinator.Cancel();
        var result = _startEmptyWorkspaceUseCase.Execute();
        StatusMessage = result.IsSuccess
            ? "Ready. Empty workspace created. No demo assets were loaded."
            : result.Error.Message;

        _selectedSourceCoordinate = null;
        _dragAnchor = null;
        _selectedArea = null;
        _isDraggingSourceArea = false;
        _baseImage = null;
        _landmarkImage = null;
        _overlayImage = null;
        _compositeImage = null;
        DmiPath = string.Empty;
        ConfigPath = string.Empty;
        SaveConfigPath = string.Empty;
        LegacyCsvPath = string.Empty;
        DraftConfigName = "New Config";
        BaseStateName = string.Empty;
        LandmarkStateName = string.Empty;
        OverlayStateName = string.Empty;
        CurrentStateSummary = "No DMI state selected yet.";
        EditorStatus = "Select a source pixel or drag an area to begin editing.";
        SelectedSourceSummary = "No source pixel selected.";
        SelectedAreaSummary = "No area selected.";
        HoverSummary = "Hover a cell to inspect coordinates.";
        BatchSummary = "Batch processing is idle.";
        BatchCurrentFile = string.Empty;
        BatchProcessedFiles = 0;
        BatchTotalFiles = 0;
        OperationProgressValue = 0;
        OperationProgressMaximum = 1;
        IsProgressIndeterminate = false;
        BatchResults.Clear();
        ClearPreviewArtifacts();
        SelectedShellTab = ShellTabKind.Start;
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
            return;
        }

        _previewRefreshCoordinator.Request(token => TryBuildPreviewAsync(userInitiated: false, token));
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

        var result = await _buildPreviewUseCase.ExecuteAsync(cancellationToken);
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

    private void ApplyMappedArea(PixelAreaSelection area, PixelCoordinate targetAnchor, string successMessage)
    {
        var result = _applyConfigTransformUseCase.Execute(config =>
        {
            var next = config;
            var offsetX = targetAnchor.X - area.Left;
            var offsetY = targetAnchor.Y - area.Top;

            foreach (var source in area.Enumerate())
            {
                var target = ClampCoordinate(
                    new PixelCoordinate(
                        Math.Clamp(source.X + offsetX, 0, config.Resolution.Width - 1),
                        Math.Clamp(source.Y + offsetY, 0, config.Resolution.Height - 1)),
                    config.Resolution);

                next = ApplyScopedMapping(next, source, target);
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
            foreach (var source in area.Enumerate())
            {
                next = ApplyScopedMapping(next, source, null);
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
            foreach (var source in area.Enumerate())
            {
                next = ApplyScopedRestore(next, source);
            }

            return next;
        });

        ApplyMutationResult(result, successMessage);
    }

    private SpriteConfig ApplyScopedMapping(SpriteConfig config, PixelCoordinate source, PixelCoordinate? target)
    {
        var next = config;
        var selectedDirection = GetSafeSelectedDirection();
        foreach (var direction in ResolveDirections(config.SupportedDirections))
        {
            var transformedSource = TransformCoordinate(source, selectedDirection, direction, config.Resolution);
            PixelCoordinate? transformedTarget = target is null
                ? null
                : TransformTarget(source, target.Value, transformedSource, selectedDirection, direction, config.Resolution);
            next = next.SetMapping(direction, transformedSource, transformedTarget);
        }

        return next;
    }

    private SpriteConfig ApplyScopedRestore(SpriteConfig config, PixelCoordinate source)
    {
        var next = config;
        var selectedDirection = GetSafeSelectedDirection();
        foreach (var direction in ResolveDirections(config.SupportedDirections))
        {
            next = next.RemoveMapping(direction, TransformCoordinate(source, selectedDirection, direction, config.Resolution));
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
            DirectionScope.Parallel => available.Where(direction => GetParallelGroup(direction) == GetParallelGroup(selectedDirection)).ToArray(),
            DirectionScope.All => available,
            _ => [selectedDirection]
        };
    }

    private PixelCoordinate TransformCoordinate(
        PixelCoordinate coordinate,
        SpriteDirection selectedDirection,
        SpriteDirection targetDirection,
        SpriteResolution resolution)
    {
        if (!MirrorAcrossDirections || !ShouldMirror(selectedDirection, targetDirection))
        {
            return coordinate;
        }

        return ClampCoordinate(new PixelCoordinate((resolution.Width - 1) - coordinate.X, coordinate.Y), resolution);
    }

    private PixelCoordinate TransformTarget(
        PixelCoordinate source,
        PixelCoordinate target,
        PixelCoordinate transformedSource,
        SpriteDirection selectedDirection,
        SpriteDirection targetDirection,
        SpriteResolution resolution)
    {
        if (!UseCentralizedPropagation || selectedDirection == targetDirection)
        {
            return TransformCoordinate(target, selectedDirection, targetDirection, resolution);
        }

        var deltaX = target.X - source.X;
        var deltaY = target.Y - source.Y;
        if (MirrorAcrossDirections && ShouldMirror(selectedDirection, targetDirection))
        {
            deltaX *= -1;
        }

        return ClampCoordinate(
            new PixelCoordinate(
                Math.Clamp(transformedSource.X + deltaX, 0, resolution.Width - 1),
                Math.Clamp(transformedSource.Y + deltaY, 0, resolution.Height - 1)),
            resolution);
    }

    private static bool ShouldMirror(SpriteDirection selectedDirection, SpriteDirection targetDirection)
    {
        if (selectedDirection == targetDirection)
        {
            return false;
        }

        return (IsLeftFacing(selectedDirection), IsLeftFacing(targetDirection), IsRightFacing(selectedDirection), IsRightFacing(targetDirection)) switch
        {
            (true, false, false, true) => true,
            (false, true, true, false) => true,
            _ => false
        };
    }

    private static bool IsLeftFacing(SpriteDirection direction) =>
        direction is SpriteDirection.West or SpriteDirection.SouthWest or SpriteDirection.NorthWest;

    private static bool IsRightFacing(SpriteDirection direction) =>
        direction is SpriteDirection.East or SpriteDirection.SouthEast or SpriteDirection.NorthEast;

    private static int GetParallelGroup(SpriteDirection direction) =>
        direction switch
        {
            SpriteDirection.South or SpriteDirection.North => 0,
            SpriteDirection.East or SpriteDirection.West => 1,
            SpriteDirection.SouthEast or SpriteDirection.NorthWest => 2,
            SpriteDirection.SouthWest or SpriteDirection.NorthEast => 3,
            _ => 4
        };

    private static PixelCoordinate ClampCoordinate(PixelCoordinate coordinate, SpriteResolution resolution) =>
        new(Math.Clamp(coordinate.X, 0, resolution.Width - 1), Math.Clamp(coordinate.Y, 0, resolution.Height - 1));

    private void ApplyMutationResult(Result<SpriteConfig> result, string successMessage)
    {
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        StatusMessage = successMessage;
        NormalizeSelectedDirection();
        RefreshWorkspaceState();
        RefreshEditorSurface();
        RequestAutoPreviewRefresh();
        PersistWorkspaceSettingsInBackground();
    }

    private void RefreshWorkspaceState()
    {
        var workspace = _editorSession.Workspace;
        WorkspaceTitle = workspace.IsEmpty ? "Empty workspace" : workspace.DisplayName ?? "Sprite workspace";
        CurrentDirectionText = _editorSession.SelectedDirection.ToString();

        AvailableDirections.Clear();
        foreach (var direction in (_editorSession.LoadedAsset?.SupportedDirections ?? _editorSession.CurrentConfig?.SupportedDirections ?? SupportedDirectionSet.Four).GetDirections())
        {
            AvailableDirections.Add(direction);
        }

        AvailableStates.Clear();
        foreach (var state in (_editorSession.LoadedAsset?.States ?? Array.Empty<DmiStateInfo>())
                     .OrderBy(static state => state.Name, StringComparer.Ordinal))
        {
            AvailableStates.Add(state.Name);
        }

        if (_editorSession.LoadedAsset is { } asset)
        {
            SpriteContractSummary = $"{asset.Resolution} | {asset.SupportedDirections} directions | {asset.States.Count} states";
            CurrentStateSummary = string.IsNullOrWhiteSpace(BaseStateName)
                ? "Choose a base state from the explorer or type a name."
                : $"Base '{BaseStateName}', landmark '{NormalizeOptionalText(LandmarkStateName)}', overlay '{NormalizeOptionalText(OverlayStateName)}'.";
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
            WorkspaceNotes = "Editor workflow is active. Use the source pane, editable pane, direction scope, and batch tools without touching legacy static controllers.";
        }
        else
        {
            ConfigSummary = "No config loaded";
            WorkspaceNotes = "Empty workspace first. Open a DMI, create or load a JSON config, then edit mappings through the new MVVM shell.";
        }

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
            ? $"Direction: {direction}. Base state is not selected yet."
            : $"Direction: {direction}. Base '{BaseStateName}', landmark '{NormalizeOptionalText(LandmarkStateName)}', overlay '{NormalizeOptionalText(OverlayStateName)}'.";
        OnPropertyChanged(string.Empty);
    }

    private void RefreshEditorSurface()
    {
        NormalizeSelectedDirection();
        RefreshMappingRows();
        RebuildPixelRows(SourceRows, false);
        RebuildPixelRows(TargetRows, ShowOverlay);
        RebuildPreviewGridRows();
        RefreshActivePreviewPresentation();
        OnPropertyChanged(string.Empty);
    }

    private void NavigateToTab(ShellTabKind tab) => SelectedShellTab = tab;

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
}
