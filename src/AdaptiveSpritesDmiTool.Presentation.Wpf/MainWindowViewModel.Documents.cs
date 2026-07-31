using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using System.IO;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class WorkspaceShellViewModel
{
    internal async Task<Result> AddAuxiliaryDocumentFrameAsync(
        SpriteDocument document,
        string stateName,
        SpriteDirection direction,
        int frameIndex,
        CancellationToken cancellationToken)
    {
        if (_auxiliaryLayerFrameReader is null)
        {
            return Result.Failure(Errors.Conflict("Auxiliary layer reader is unavailable."));
        }

        var resolution = ResolveEditorResolution();
        if (resolution is null)
        {
            return Result.Failure(Errors.Conflict("Open an editor asset or config before adding an auxiliary layer."));
        }

        if (document.Resolution != resolution.Value)
        {
            return Result.Failure(Errors.Validation(
                $"Auxiliary document resolution {document.Resolution} does not match editor {resolution.Value}."));
        }

        var state = document.States.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, stateName, StringComparison.Ordinal));
        if (state is null || frameIndex < 0 || frameIndex >= state.FramesPerDirection)
        {
            return Result.Failure(Errors.Validation("Selected auxiliary state or frame is invalid."));
        }

        var resolvedDirection = state.DirectionDepth.GetDirections().Contains(direction)
            ? direction
            : SpriteDirection.South;
        var documentFrame = state.Frames.First(frame =>
            frame.Direction == resolvedDirection && frame.FrameIndex == frameIndex);
        var source = document.Sources.First(candidate => candidate.Id == documentFrame.Source.SourceId);
        var sourceStateName = documentFrame.Source.SourceStateName ?? state.Name;
        var readResult = await _auxiliaryLayerFrameReader.ReadAsync(
            new AuxiliaryLayerFrameRequest(
                source.AbsolutePathFallback,
                source.Format,
                sourceStateName,
                resolvedDirection,
                documentFrame.Source.SourceFrameIndex,
                resolution.Value),
            cancellationToken);
        if (readResult.IsFailure)
        {
            return Result.Failure(readResult.Error);
        }

        var duplicate = ImportedDmiStateItems.FirstOrDefault(item =>
            item.SourceId == source.Id &&
            item.FrameIndex == documentFrame.Source.SourceFrameIndex &&
            string.Equals(item.StateName, sourceStateName, StringComparison.Ordinal));
        if (duplicate is not null)
        {
            SelectedImportedDmiStateItem = duplicate;
            return Result.Success();
        }

        var order = ImportedDmiStateItems.Count == 0
            ? 0
            : ImportedDmiStateItems.Max(static item => item.Order) + 1;
        var item = new ImportedDmiStateItemViewModel(
            sourceStateName,
            source.AbsolutePathFallback,
            Path.GetFileName(source.AbsolutePathFallback),
            _bitmapSourceFactory.Create(readResult.Value),
            isSourceAssigned: ImportedDmiStateItems.Count == 0,
            isEditableAssigned: false,
            ImportedStatePlacementMode.Overlay,
            order,
            opacityPercent: 100,
            source.Id,
            source.Format,
            documentFrame.Source.SourceFrameIndex);
        AttachImportedStateItem(item);
        ImportedDmiStateItems.Add(item);
        SelectedImportedDmiStateItem = item;
        _importedStateFrameCache[(item.SourcePath, item.StateName, resolvedDirection, item.FrameIndex, item.SourceFormat)] = readResult.Value;
        RefreshImportedStateComposition();
        OnPropertyChanged(nameof(ImportedDmiStateItems));
        await PersistWorkspaceSettingsAsync(cancellationToken);
        return Result.Success();
    }

    internal Task OpenNativeDmiInEditorAsync(string path, CancellationToken cancellationToken) =>
        OpenDmiFromPathAsync(path, navigateToEditor: false, persistSettings: false, cancellationToken);

    private async Task RestoreAuxiliaryLayerItemsAsync(
        IReadOnlyList<WorkspaceAuxiliaryLayerSettings> settings,
        CancellationToken cancellationToken)
    {
        if (_auxiliaryLayerFrameReader is null || settings.Count == 0)
        {
            return;
        }

        _isUpdatingImportedStateItems = true;
        try
        {
            ClearImportedStateItems();
            foreach (var layer in settings.OrderBy(static item => item.Order))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var label = string.IsNullOrWhiteSpace(layer.SourceFileLabel)
                    ? Path.GetFileName(layer.SourcePath)
                    : layer.SourceFileLabel!;
                var placement = ParseImportedStatePlacementMode(layer.PlacementMode);
                SpriteImage? preview = null;
                string validationMessage;
                var isValid = false;
                var resolution = ResolveEditorResolution();
                if (string.IsNullOrWhiteSpace(layer.SourcePath) || !File.Exists(layer.SourcePath))
                {
                    validationMessage = $"Auxiliary source was not found: {layer.SourcePath}";
                }
                else if (resolution is null)
                {
                    validationMessage = "Open an editor asset or config to validate this auxiliary layer.";
                }
                else
                {
                    var readResult = await _auxiliaryLayerFrameReader.ReadAsync(
                        new AuxiliaryLayerFrameRequest(
                            layer.SourcePath,
                            layer.Format,
                            layer.StateName,
                            SpriteDirection.South,
                            layer.FrameIndex,
                            resolution.Value),
                        cancellationToken);
                    if (readResult.IsSuccess)
                    {
                        preview = readResult.Value;
                        isValid = true;
                        validationMessage = string.Empty;
                        _importedStateFrameCache[(
                            layer.SourcePath,
                            layer.StateName,
                            SpriteDirection.South,
                            layer.FrameIndex,
                            layer.Format)] = preview;
                    }
                    else
                    {
                        validationMessage = readResult.Error.Message;
                    }
                }

                var item = new ImportedDmiStateItemViewModel(
                    layer.StateName,
                    layer.SourcePath,
                    string.IsNullOrWhiteSpace(label) ? "Missing source" : label,
                    _bitmapSourceFactory.Create(preview),
                    layer.IsSourceAssigned,
                    layer.IsEditableAssigned,
                    placement,
                    Math.Max(0, layer.Order),
                    Math.Clamp(layer.OpacityPercent, 0, 100),
                    layer.SourceId,
                    layer.Format,
                    layer.FrameIndex)
                {
                    IsValid = isValid,
                    ValidationMessage = validationMessage
                };
                AttachImportedStateItem(item);
                ImportedDmiStateItems.Add(item);
            }
        }
        finally
        {
            _isUpdatingImportedStateItems = false;
        }

        SelectedImportedDmiStateItem = ImportedDmiStateItems.FirstOrDefault();
        if (ImportedDmiStateItems.Any(static item => item.IsAssignedToAnySurface))
        {
            await PreloadImportedStateFramesAsync(cancellationToken);
        }

        RefreshImportedStateComposition();
        OnPropertyChanged(nameof(ImportedDmiStateItems));
    }
}
