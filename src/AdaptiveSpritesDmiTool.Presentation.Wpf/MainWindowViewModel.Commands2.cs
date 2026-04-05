using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.Input;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task BuildPreviewAsync()
    {
        await RunBusyOperationAsync(TryBuildPreviewAsync);
    }

    [RelayCommand(CanExecute = nameof(CanRunBatch))]
    private async Task RunBatchAsync()
    {
        if (string.IsNullOrWhiteSpace(BatchInputDirectory) || string.IsNullOrWhiteSpace(BatchOutputDirectory))
        {
            StatusMessage = "Select both input and output folders before running batch processing.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                BatchResults.Clear();
                BatchProcessedFiles = 0;
                BatchTotalFiles = 0;
                BatchCurrentFile = string.Empty;
                OperationProgressValue = 0;
                OperationProgressMaximum = 1;
                IsProgressIndeterminate = false;

                var progress = new Progress<BatchProgress>(value =>
                {
                    BatchProcessedFiles = value.ProcessedFiles;
                    BatchTotalFiles = value.TotalFiles;
                    BatchCurrentFile = value.CurrentFile ?? string.Empty;
                    OperationProgressMaximum = Math.Max(1, value.TotalFiles);
                    OperationProgressValue = Math.Min(value.ProcessedFiles, OperationProgressMaximum);
                });

                var result = await _applyConfigToDmiBatchUseCase.ExecuteAsync(
                    BatchInputDirectory,
                    BatchOutputDirectory,
                    SelectedOverwritePolicy,
                    progress,
                    cancellationToken);

                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    BatchSummary = "Batch processing failed.";
                    return;
                }

                foreach (var file in result.Value.Files)
                {
                    BatchResults.Add(new BatchResultRowViewModel(file));
                }

                var processedCount = result.Value.Files.Count(file => file.Status == BatchFileStatus.Processed);
                var skippedCount = result.Value.Files.Count(file => file.Status == BatchFileStatus.Skipped);
                var failedCount = result.Value.Files.Count(file => file.Status == BatchFileStatus.Failed);
                var cancelledCount = result.Value.Files.Count(file => file.Status == BatchFileStatus.Cancelled);
                var summaryPrefix = result.Value.WasCancelled ? "Batch cancelled with" : "Batch finished with";
                BatchSummary =
                    $"{summaryPrefix} {processedCount} processed, " +
                    $"{skippedCount} skipped, " +
                    $"{failedCount} failed, " +
                    $"{cancelledCount} cancelled.";
                StatusMessage = BatchSummary;
                await PersistWorkspaceSettingsAsync();
            });
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        var result = _undoChangeUseCase.Execute();
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        StatusMessage = "Undo applied.";
        RefreshWorkspaceState();
        RefreshEditorSurface();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        var result = _redoChangeUseCase.Execute();
        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        StatusMessage = "Redo applied.";
        RefreshWorkspaceState();
        RefreshEditorSurface();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        _selectedSourceCoordinate = null;
        _selectedArea = null;
        _dragAnchor = null;
        _isDraggingSourceArea = false;
        SelectedSourceSummary = "No source pixel selected.";
        SelectedAreaSummary = "No area selected.";
        EditorStatus = "Selection cleared.";
        RefreshEditorSurface();
    }

    [RelayCommand]
    private void ResetActiveConfig()
    {
        if (!CanEditConfig())
        {
            StatusMessage = "There is no active config to clear.";
            return;
        }

        var result = _applyConfigTransformUseCase.Execute(config =>
            SpriteConfig.CreateEmpty(
                config.Name,
                config.Resolution,
                config.SupportedDirections,
                config.Metadata.Touch(DateTimeOffset.UtcNow)));

        ApplyMutationResult(result, "Active config reset to an empty mapping workspace.");
    }

    [RelayCommand]
    private void UseStateAsBase(string stateName)
    {
        BaseStateName = stateName;
        RefreshPreviewSelectionSummary();
        PersistWorkspaceSettingsInBackground();
    }

    [RelayCommand]
    private void UseStateAsLandmark(string stateName)
    {
        LandmarkStateName = stateName;
        RefreshPreviewSelectionSummary();
        PersistWorkspaceSettingsInBackground();
    }

    [RelayCommand]
    private void UseStateAsOverlay(string stateName)
    {
        OverlayStateName = stateName;
        RefreshPreviewSelectionSummary();
        PersistWorkspaceSettingsInBackground();
    }

    [RelayCommand]
    private void RemoveMapping(MappingRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        var result = _applyConfigTransformUseCase.Execute(config => config.RemoveMapping(GetSafeSelectedDirection(), row.Source));
        ApplyMutationResult(result, $"Removed mapping for {row.Source}.");
    }

    [RelayCommand]
    private void UseSelectedStateAsBase()
    {
        if (!string.IsNullOrWhiteSpace(SelectedExplorerState))
        {
            UseStateAsBase(SelectedExplorerState);
        }
    }

    [RelayCommand]
    private void UseSelectedStateAsLandmark()
    {
        if (!string.IsNullOrWhiteSpace(SelectedExplorerState))
        {
            UseStateAsLandmark(SelectedExplorerState);
        }
    }

    [RelayCommand]
    private void UseSelectedStateAsOverlay()
    {
        if (!string.IsNullOrWhiteSpace(SelectedExplorerState))
        {
            UseStateAsOverlay(SelectedExplorerState);
        }
    }

    [RelayCommand]
    private void RemoveSelectedMapping()
    {
        if (SelectedMapping is not null)
        {
            RemoveMapping(SelectedMapping);
        }
    }

    [RelayCommand]
    private async Task ResetWorkspaceAsync()
    {
        await RunBusyOperationAsync(
            async _ =>
            {
                ResetWorkspaceCore();
                RefreshWorkspaceState();
                RefreshPreviewSelectionSummary();
                RefreshEditorSurface();
                await PersistWorkspaceSettingsAsync();
            });
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        if (_activeOperationCts is null)
        {
            StatusMessage = "No cancellable operation is active.";
            return;
        }

        _activeOperationCts.Cancel();
        StatusMessage = "Cancellation requested.";
    }

    partial void OnSelectedDirectionChanged(SpriteDirection value)
    {
        if (_isSynchronizingSelectedDirection)
        {
            return;
        }

        TryApplySelectedDirection(value, refreshUi: true);
    }

    partial void OnSelectedPreviewDisplayModeChanged(PreviewDisplayMode value) => RefreshActivePreviewPresentation();

    partial void OnShowGridChanged(bool value) => RefreshEditorSurface();

    partial void OnGridAboveImageChanged(bool value) => RefreshEditorSurface();

    partial void OnShowOverlayChanged(bool value)
    {
        RefreshEditorSurface();
        RefreshActivePreviewPresentation();
    }

    partial void OnShowTextGridChanged(bool value) => RefreshActivePreviewPresentation();
}
