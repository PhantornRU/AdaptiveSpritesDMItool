using AdaptiveSpritesDmiTool.Application;
using Microsoft.Extensions.Logging;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public sealed class MainWindowViewModel : WorkspaceShellViewModel
{
    public MainWindowViewModel(
        StartEmptyWorkspaceUseCase startEmptyWorkspaceUseCase,
        CreateConfigUseCase createConfigUseCase,
        SaveConfigUseCase saveConfigUseCase,
        LoadConfigUseCase loadConfigUseCase,
        ImportLegacyCsvConfigUseCase importLegacyCsvConfigUseCase,
        LoadDmiFileUseCase loadDmiFileUseCase,
        BuildPreviewUseCase buildPreviewUseCase,
        ApplyConfigToDmiBatchUseCase applyConfigToDmiBatchUseCase,
        UndoChangeUseCase undoChangeUseCase,
        RedoChangeUseCase redoChangeUseCase,
        SetPreviewSelectionUseCase setPreviewSelectionUseCase,
        SetSelectedDirectionUseCase setSelectedDirectionUseCase,
        ApplyConfigTransformUseCase applyConfigTransformUseCase,
        LoadWorkspaceSettingsUseCase loadWorkspaceSettingsUseCase,
        SaveWorkspaceSettingsUseCase saveWorkspaceSettingsUseCase,
        SpriteImageBitmapSourceFactory bitmapSourceFactory,
        IFileDialogService fileDialogService,
        EditorSession editorSession,
        ILogger<WorkspaceShellViewModel> logger)
        : base(
            startEmptyWorkspaceUseCase,
            createConfigUseCase,
            saveConfigUseCase,
            loadConfigUseCase,
            importLegacyCsvConfigUseCase,
            loadDmiFileUseCase,
            buildPreviewUseCase,
            applyConfigToDmiBatchUseCase,
            undoChangeUseCase,
            redoChangeUseCase,
            setPreviewSelectionUseCase,
            setSelectedDirectionUseCase,
            applyConfigTransformUseCase,
            loadWorkspaceSettingsUseCase,
            saveWorkspaceSettingsUseCase,
            bitmapSourceFactory,
            fileDialogService,
            editorSession,
            logger)
    {
    }
}
