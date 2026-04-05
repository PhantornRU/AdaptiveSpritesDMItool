using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class MainWindowViewModel
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
        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();
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

    public void HandleSourceCellPointerDown(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (!CanEditConfig())
        {
            return;
        }

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
                RefreshEditorSurface();
                break;
            case EditorTool.Single:
                _selectedSourceCoordinate = cell.Coordinate;
                SelectedSourceSummary = $"Source pixel {cell.Coordinate} selected.";
                EditorStatus = "Pick a target pixel in the editable pane to apply the mapping.";
                RefreshEditorSurface();
                break;
            case EditorTool.Undo:
                ApplyRestoreOperation(new PixelAreaSelection(cell.Coordinate, cell.Coordinate), "Restored source pixel.");
                break;
        }
    }

    public void HandleSourceCellPointerEnter(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        HoverSummary = $"Hovering {cell.Coordinate}.";

        if (!_isDraggingSourceArea || _dragAnchor is null)
        {
            return;
        }

        _selectedArea = new PixelAreaSelection(_dragAnchor.Value, cell.Coordinate);
        SelectedAreaSummary = DescribeArea(_selectedArea.Value);
        RefreshEditorSurface();
    }

    public void HandleSourceCellPointerUp(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

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

        RefreshEditorSurface();
    }

    public void HandleTargetCellPointerUp(PixelCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (!CanEditConfig())
        {
            return;
        }

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
        if (string.IsNullOrWhiteSpace(DmiPath))
        {
            BrowseDmiPath();
        }

        if (string.IsNullOrWhiteSpace(DmiPath))
        {
            StatusMessage = "Choose a DMI file to load.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var result = await _loadDmiFileUseCase.ExecuteAsync(DmiPath, cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                BaseStateName = result.Value.States.FirstOrDefault()?.Name ?? string.Empty;
                LandmarkStateName = string.Empty;
                OverlayStateName = string.Empty;
                DraftConfigName = Path.GetFileNameWithoutExtension(result.Value.DisplayName);
                BatchInputDirectory = Path.GetDirectoryName(result.Value.SourcePath ?? DmiPath) ?? string.Empty;
                BatchOutputDirectory = string.IsNullOrWhiteSpace(BatchOutputDirectory)
                    ? Path.Combine(BatchInputDirectory, "processed")
                    : BatchOutputDirectory;
                NormalizeSelectedDirection();
                StatusMessage = $"Loaded DMI '{result.Value.DisplayName}' with {result.Value.States.Count} states.";
                ClearPreviewArtifacts();
                RefreshWorkspaceState();
                RefreshPreviewSelectionSummary();
                RefreshEditorSurface();
                await PersistWorkspaceSettingsAsync();
            });
    }

    [RelayCommand(CanExecute = nameof(CanCreateConfig))]
    private void CreateConfig()
    {
        var metadata = ConfigMetadata.CreateNew(ConfigSource.UserCreated, sourceIdentifier: "presentation-shell");
        var result = _createConfigUseCase.Execute(
            string.IsNullOrWhiteSpace(DraftConfigName) ? "New Config" : DraftConfigName.Trim(),
            metadata);

        if (result.IsFailure)
        {
            StatusMessage = result.Error.Message;
            return;
        }

        if (string.IsNullOrWhiteSpace(SaveConfigPath) && !string.IsNullOrWhiteSpace(DmiPath))
        {
            SaveConfigPath = Path.ChangeExtension(DmiPath, ".json") ?? string.Empty;
        }

        StatusMessage = $"Created config '{result.Value.Name}'.";
        RefreshWorkspaceState();
        RefreshPreviewSelectionSummary();
        RefreshEditorSurface();
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
                StatusMessage = $"Saved config to '{SaveConfigPath}'.";
                RefreshWorkspaceState();
                await PersistWorkspaceSettingsAsync();
            });
    }

    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            BrowseLoadConfigPath();
        }

        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            StatusMessage = "Choose a JSON config file to load.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var result = await _loadConfigUseCase.ExecuteAsync(ConfigPath, cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                SaveConfigPath = ConfigPath;
                DraftConfigName = result.Value.Name;
                StatusMessage = $"Loaded config '{result.Value.Name}'.";
                RefreshWorkspaceState();
                RefreshPreviewSelectionSummary();
                RefreshEditorSurface();
                await TryBuildPreviewAsync(cancellationToken);
                await PersistWorkspaceSettingsAsync();
            });
    }

    [RelayCommand]
    private async Task ImportLegacyConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(LegacyCsvPath))
        {
            BrowseLegacyCsvPath();
        }

        if (string.IsNullOrWhiteSpace(LegacyCsvPath))
        {
            StatusMessage = "Choose a legacy CSV config to import.";
            return;
        }

        await RunBusyOperationAsync(
            async cancellationToken =>
            {
                var result = await _importLegacyCsvConfigUseCase.ExecuteAsync(LegacyCsvPath, cancellationToken);
                if (result.IsFailure)
                {
                    StatusMessage = result.Error.Message;
                    return;
                }

                DraftConfigName = result.Value.Name;
                StatusMessage = $"Imported legacy CSV '{Path.GetFileName(LegacyCsvPath)}' as config '{result.Value.Name}'.";
                RefreshWorkspaceState();
                RefreshPreviewSelectionSummary();
                RefreshEditorSurface();
                await TryBuildPreviewAsync(cancellationToken);
                await PersistWorkspaceSettingsAsync();
            });
    }
}
