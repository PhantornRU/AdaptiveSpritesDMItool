using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.Input;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class WorkspaceShellViewModel
{
    private bool CanContinueToEditor() => HasEditorWorkflow && !IsBusy;

    private bool CanResumeLastWorkspace() =>
        !IsBusy &&
        (HasEditorWorkflow ||
         !string.IsNullOrWhiteSpace(DmiPath) ||
         !string.IsNullOrWhiteSpace(ConfigPath) ||
         !string.IsNullOrWhiteSpace(LegacyCsvPath));

    private bool CanOpenRecentDmi() => !IsBusy && !string.IsNullOrWhiteSpace(DmiPath);

    private bool CanOpenRecentConfig() => !IsBusy && !string.IsNullOrWhiteSpace(ConfigPath);

    private bool CanImportRecentLegacyCsv() => !IsBusy && !string.IsNullOrWhiteSpace(LegacyCsvPath);

    [RelayCommand(CanExecute = nameof(CanContinueToEditor))]
    private void ContinueToEditor()
    {
        NavigateToTab(ShellTabKind.Editor);
        StatusMessage = "Returned to the editor workspace.";
    }

    [RelayCommand(CanExecute = nameof(CanResumeLastWorkspace))]
    private async Task ResumeLastWorkspaceAsync()
    {
        if (HasEditorWorkflow)
        {
            NavigateToTab(ShellTabKind.Editor);
            StatusMessage = "Returned to the active workspace.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var recentDmiPath = DmiPath;
                var recentConfigPath = ConfigPath;
                var recentLegacyCsvPath = LegacyCsvPath;

                if (!string.IsNullOrWhiteSpace(recentDmiPath))
                {
                    await OpenDmiFromPathAsync(recentDmiPath, navigateToEditor: false, persistSettings: false, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(recentConfigPath))
                {
                    await LoadConfigFromPathAsync(recentConfigPath, navigateToEditor: false, persistSettings: false, cancellationToken);
                }
                else if (!string.IsNullOrWhiteSpace(recentLegacyCsvPath))
                {
                    await ImportLegacyConfigFromPathAsync(recentLegacyCsvPath, navigateToEditor: false, persistSettings: false, cancellationToken);
                }

                NavigateToTab(HasEditorWorkflow ? ShellTabKind.Editor : ShellTabKind.Start);
                StatusMessage = HasEditorWorkflow
                    ? "Last workspace restored."
                    : "No recent workspace could be restored.";
                await PersistWorkspaceSettingsAsync();
            });
    }

    [RelayCommand(CanExecute = nameof(CanOpenRecentDmi))]
    private async Task OpenRecentDmiAsync()
    {
        await RunBusyOperationAsync(
            cancellationToken => OpenDmiFromPathAsync(DmiPath, navigateToEditor: true, persistSettings: true, cancellationToken));
    }

    [RelayCommand(CanExecute = nameof(CanOpenRecentConfig))]
    private async Task OpenRecentConfigAsync()
    {
        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                if (_editorSession.LoadedAsset is null && !string.IsNullOrWhiteSpace(DmiPath))
                {
                    await OpenDmiFromPathAsync(DmiPath, navigateToEditor: false, persistSettings: false, cancellationToken);
                }

                await LoadConfigFromPathAsync(ConfigPath, navigateToEditor: true, persistSettings: true, cancellationToken);
            });
    }

    [RelayCommand(CanExecute = nameof(CanImportRecentLegacyCsv))]
    private async Task ImportRecentLegacyCsvAsync()
    {
        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                if (_editorSession.LoadedAsset is null && !string.IsNullOrWhiteSpace(DmiPath))
                {
                    await OpenDmiFromPathAsync(DmiPath, navigateToEditor: false, persistSettings: false, cancellationToken);
                }

                await ImportLegacyConfigFromPathAsync(LegacyCsvPath, navigateToEditor: true, persistSettings: true, cancellationToken);
            });
    }

    [RelayCommand(CanExecute = nameof(CanBuildPreview))]
    private async Task BuildPreviewAsync()
    {
        if (IsBusy)
        {
            StatusMessage = "Wait for the active operation to finish before refreshing preview.";
            return;
        }

        await RefreshPreviewNowAsync(userInitiated: true, CancellationToken.None);
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
                NavigateToTab(ShellTabKind.Batch);
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
        RequestAutoPreviewRefresh();
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
        RequestAutoPreviewRefresh();
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
        RequestAutoPreviewRefresh();
        PersistWorkspaceSettingsInBackground();
    }

    [RelayCommand]
    private void UseStateAsLandmark(string stateName)
    {
        LandmarkStateName = stateName;
        RefreshPreviewSelectionSummary();
        RequestAutoPreviewRefresh();
        PersistWorkspaceSettingsInBackground();
    }

    [RelayCommand]
    private void UseStateAsOverlay(string stateName)
    {
        OverlayStateName = stateName;
        RefreshPreviewSelectionSummary();
        RequestAutoPreviewRefresh();
        PersistWorkspaceSettingsInBackground();
    }

    [RelayCommand]
    private void ClearOptionalPreviewLayers()
    {
        LandmarkStateName = string.Empty;
        OverlayStateName = string.Empty;
        RefreshPreviewSelectionSummary();
        RequestAutoPreviewRefresh();
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
                NavigateToTab(ShellTabKind.Start);
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

        if (TryApplySelectedDirection(value, refreshUi: true))
        {
            RequestAutoPreviewRefresh();
            PersistWorkspaceSettingsInBackground();
        }
    }

    partial void OnSelectedPreviewDisplayModeChanged(PreviewDisplayMode value) => RefreshActivePreviewPresentation();

    partial void OnAutoPreviewModeChanged(AutoPreviewMode value)
    {
        if (value == AutoPreviewMode.Enabled)
        {
            RequestAutoPreviewRefresh();
        }
    }

    partial void OnShowGridChanged(bool value) => RefreshEditorSurface();

    partial void OnGridAboveImageChanged(bool value) => RefreshEditorSurface();

    partial void OnShowOverlayChanged(bool value)
    {
        RefreshEditorSurface();
        RefreshActivePreviewPresentation();
    }

    partial void OnShowTextGridChanged(bool value) => RefreshActivePreviewPresentation();
}
