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
        RefreshWorkspaceState();
        if (HasEditorWorkflow)
        {
            ApplyAdaptiveEditorZoom(force: true);
        }

        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();
        NavigateToSection(HasEditorWorkflow ? ShellSectionKind.Editor : ShellSectionKind.Start);
    }

    public async Task PersistWorkspaceSettingsAsync()
    {
        var settings = BuildWorkspaceSettings();
        await _workspaceSettingsPersistenceGate.WaitAsync().ConfigureAwait(false);

        try
        {
            var result = await _saveWorkspaceSettingsUseCase
                .ExecuteAsync(settings, CancellationToken.None)
                .ConfigureAwait(false);
            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to persist workspace settings: {Message}", result.Error.Message);
            }
        }
        finally
        {
            _workspaceSettingsPersistenceGate.Release();
        }
    }

    private void PersistWorkspaceSettingsInBackground() => _ = PersistWorkspaceSettingsAsync();

    private void EnsureActiveDirection(SpriteDirection direction)
    {
        if (SelectedDirection == direction)
        {
            return;
        }

        TryApplySelectedDirection(direction, refreshUi: false);
    }

    public void HandleSourceCellPointerDown(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (!CanEditConfig())
        {
            return;
        }

        EnsureActiveDirection(cell.Direction);

        switch (SelectedEditorTool)
        {
            case EditorTool.Select:
            case EditorTool.Fill:
            case EditorTool.Delete:
            case EditorTool.UndoArea:
            case EditorTool.Move:
                _dragAnchor = cell.Coordinate;
                _selectedArea = new PixelAreaSelection(cell.Coordinate, cell.Coordinate);
                _isDraggingSourceArea = true;
                EditorStatus = $"{SelectedEditorTool} drag started at {cell.Coordinate}.";
                RefreshInteractionState();
                break;
            case EditorTool.Single:
                _selectedSourceCoordinate = cell.Coordinate;
                SelectedSourceSummary = $"Source pixel {cell.Coordinate} selected.";
                EditorStatus = "Pick a target pixel in the editable pane to apply the mapping.";
                RefreshInteractionState();
                break;
            case EditorTool.Undo:
                ApplyRestoreOperation(new PixelAreaSelection(cell.Coordinate, cell.Coordinate), "Restored source pixel.");
                break;
        }
    }

    public void HandleSourceCellPointerEnter(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        EnsureActiveDirection(cell.Direction);
        UpdateSourceHoverState(cell.Coordinate);

        if (!_isDraggingSourceArea || _dragAnchor is null)
        {
            return;
        }

        _selectedArea = new PixelAreaSelection(_dragAnchor.Value, cell.Coordinate);
        SelectedAreaSummary = DescribeArea(_selectedArea.Value);
        RefreshInteractionState();
    }

    public void HandleSourceSurfacePointerLeave()
    {
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

        if (!_isDraggingSourceArea || _dragAnchor is null)
        {
            return;
        }

        _selectedArea = new PixelAreaSelection(_dragAnchor.Value, cell.Coordinate);
        _dragAnchor = null;
        _isDraggingSourceArea = false;
        SelectedAreaSummary = DescribeArea(_selectedArea.Value);

        switch (SelectedEditorTool)
        {
            case EditorTool.Select:
                EditorStatus = "Area selected. Switch to Fill, Move, Delete, or UndoArea to act on it.";
                break;
            case EditorTool.Delete:
                ApplyTransparentOperation(_selectedArea.Value, "Applied transparent delete to the selected area.");
                break;
            case EditorTool.UndoArea:
                ApplyRestoreOperation(_selectedArea.Value, "Restored the selected area to source pixels.");
                break;
            case EditorTool.Fill:
            case EditorTool.Move:
                EditorStatus = "Area selected. Pick a target pixel in the editable pane to apply the offset.";
                break;
        }

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

        switch (SelectedEditorTool)
        {
            case EditorTool.Single when _selectedSourceCoordinate is { } source:
                ApplyMappedArea(new PixelAreaSelection(source, source), cell.Coordinate, "Applied single-pixel mapping.");
                break;
            case EditorTool.Fill:
            case EditorTool.Move:
                if (_selectedArea is { } area)
                {
                    ApplyMappedArea(
                        area,
                        cell.Coordinate,
                        SelectedEditorTool == EditorTool.Move
                            ? "Moved the selected area by applying an offset mapping."
                            : "Filled the selected area with an offset mapping.");
                }
                break;
        }
    }

    private void ClearHoverState()
    {
        HoveredCanvasKind = null;
        SourceHoveredCoordinate = null;
        EditableHoveredCoordinate = null;
        HoverSummary = "Hover to inspect coordinates.";
        HoverMappingSummary = "No hover mapping.";
    }

    private void UpdateSourceHoverState(PixelCoordinate coordinate)
    {
        HoveredCanvasKind = EditorCanvasKind.Source;
        SourceHoveredCoordinate = coordinate;
        HoverSummary = $"Source {coordinate}.";

        if (TryResolveTargetFromSource(coordinate, out var targetCoordinate, out var isTransparent))
        {
            EditableHoveredCoordinate = targetCoordinate;
            HoverMappingSummary = isTransparent
                ? $"Source {coordinate} -> transparent."
                : $"Source {coordinate} -> Editable {targetCoordinate}.";
            return;
        }

        EditableHoveredCoordinate = null;
        HoverMappingSummary = $"Source {coordinate} has no editable mapping.";
    }

    private void UpdateEditableHoverState(PixelCoordinate coordinate)
    {
        HoveredCanvasKind = EditorCanvasKind.Editable;
        EditableHoveredCoordinate = coordinate;
        HoverSummary = $"Editable {coordinate}.";

        if (TryResolveSourceFromTarget(coordinate, out var sourceCoordinate))
        {
            SourceHoveredCoordinate = sourceCoordinate;
            HoverMappingSummary = $"Source {sourceCoordinate} -> Editable {coordinate}.";
            return;
        }

        SourceHoveredCoordinate = null;
        HoverMappingSummary = $"Editable {coordinate} has no source mapping.";
    }

    private bool TryResolveTargetFromSource(PixelCoordinate source, out PixelCoordinate? target, out bool isTransparent)
    {
        target = null;
        isTransparent = false;

        if (_editorSession.CurrentConfig is null)
        {
            return false;
        }

        foreach (var mapping in _editorSession.CurrentConfig.GetMappings(GetSafeSelectedDirection()))
        {
            if (mapping.Source != source)
            {
                continue;
            }

            target = mapping.Target;
            isTransparent = mapping.IsTransparent || mapping.Target is null;
            return true;
        }

        return false;
    }

    private bool TryResolveSourceFromTarget(PixelCoordinate target, out PixelCoordinate source)
    {
        source = default;

        if (_editorSession.CurrentConfig is null)
        {
            return false;
        }

        foreach (var mapping in _editorSession.CurrentConfig.GetMappings(GetSafeSelectedDirection()))
        {
            if (mapping.Target != target)
            {
                continue;
            }

            source = mapping.Source;
            return true;
        }

        return false;
    }

    public void HandleBatchSourceSelection(BatchSourceTreeItemViewModel? item)
    {
        SelectedBatchSourceItem = item;
        BatchCurrentFile = item is null
            ? string.Empty
            : item.IsDirectory
                ? item.FullPath
                : Path.GetFileName(item.FullPath);
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

                await MergeImportedStatesFromAssetAsync(result.Value, cancellationToken);
                StatusMessage = $"Merged {result.Value.States.Count} state(s) from '{result.Value.DisplayName}'.";
                RefreshEditorSurface();
                PersistWorkspaceSettingsInBackground();
            });
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
                await PersistWorkspaceSettingsAsync();
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
            await PersistWorkspaceSettingsAsync();
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
                AttachImportedStateItem(imported);
                ImportedDmiStateItems.Add(imported);
                continue;
            }

            existing.SourceFileLabel = Path.GetFileName(asset.SourcePath ?? asset.DisplayName);
            existing.PreviewImage = previewBitmap;
            existing.SourcePath = asset.SourcePath ?? string.Empty;
        }

        OnPropertyChanged(nameof(ImportedDmiStateItems));
        InvalidateImportedStateFrameCache();
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
            await PersistWorkspaceSettingsAsync();
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
            await PersistWorkspaceSettingsAsync();
        }
    }
}
