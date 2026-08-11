using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using AdaptiveSpritesDmiTool.Presentation.Wpf;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections;
using System.Reflection;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Presentation;

public sealed class MainWindowViewModelSmokeTests
{
    [Fact]
    public async Task InitializeAsyncShouldStartOnStartSectionWithoutDemoAssets()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.SelectedShellSection.Should().Be(ShellSectionKind.Start);
        viewModel.NavigationRail.SelectedSection.Should().Be(ShellSectionKind.Start);
        viewModel.SelectedEditorViewportMode.Should().Be(EditorViewportMode.Matrix);
        viewModel.SelectedBottomWorkspaceTab.Should().Be(BottomWorkspaceTab.Mappings);
        viewModel.StatusMessage.Should().Be("Ready.");
        viewModel.NavigationRail.Items.Should().HaveCount(5);
        viewModel.NavigationRail.Items.Should().ContainSingle(item => item.Section == ShellSectionKind.Documents);
        viewModel.EditorWorkspace.IsAvailable.Should().BeFalse();
        viewModel.BatchWorkspace.IsAvailable.Should().BeFalse();
        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();
        viewModel.StartTab.WelcomeTitle.Should().Contain("Open or import");
        viewModel.PreviewPanel.IsAutoPreviewEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task LoadedJsonMirrorAxisShouldOverrideWorkspaceAndBecomeNewWorkspaceValue()
    {
        var tempRoot = CreateTempDirectory();
        var dmiPath = Path.Combine(tempRoot, "sprite.dmi");
        var configPath = Path.Combine(tempRoot, "config.json");
        await File.WriteAllTextAsync(dmiPath, "placeholder");
        await File.WriteAllTextAsync(configPath, "placeholder");
        var settingsRepository = new InMemorySettingsRepository(
            WorkspaceSettings.Empty with
            {
                LastOpenedDmiPath = dmiPath,
                LastOpenedConfigPath = configPath,
                MirrorAxisOffsetPixels = -1
            });
        var config = SpriteConfig.CreateEmpty(
            "json-wins",
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.Json, configPath),
            new SpriteEditorSettings(1));
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            configRepository: new SuccessfulConfigRepository(config),
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.PersistWorkspaceSettingsAsync();

        viewModel.MirrorAxisOffsetPixels.Should().Be(1);
        session.CurrentConfig!.EditorSettings.MirrorAxisOffsetPixels.Should().Be(1);
        settingsRepository.Saved!.MirrorAxisOffsetPixels.Should().Be(1);
    }

    [Fact]
    public async Task NewConfigShouldInheritWorkspaceMirrorAxisUntilUserChangesIt()
    {
        var settingsRepository = new InMemorySettingsRepository(
            WorkspaceSettings.Empty with { MirrorAxisOffsetPixels = -1 });
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService,
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        session.CurrentConfig!.EditorSettings.MirrorAxisOffsetPixels.Should().Be(-1);
        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task ManualMirrorAxisChangeShouldUpdateConfigDirtyStateAndWorkspace()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: new StubFileDialogService { DmiPath = "sprite.dmi" },
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        session.SetCurrentConfigPath("saved.json").IsSuccess.Should().BeTrue();

        viewModel.MirrorAxisOffsetPixels = 1;
        await viewModel.PersistWorkspaceSettingsAsync();

        session.CurrentConfig!.EditorSettings.MirrorAxisOffsetPixels.Should().Be(1);
        session.IsDirty.Should().BeTrue();
        settingsRepository.Saved!.MirrorAxisOffsetPixels.Should().Be(1);
    }

    [Fact]
    public async Task EditorWorkspaceShouldExposeMirrorGuideValuesForCanvasBindings()
    {
        var settingsRepository = new InMemorySettingsRepository(
            WorkspaceSettings.Empty with
            {
                MirrorAxisOffsetPixels = -1,
                ShowMirrorAxisGuide = true
            });
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.EditorWorkspace.MirrorAxisOffsetPixels.Should().Be(-1);
        viewModel.EditorWorkspace.ShowMirrorAxisGuide.Should().BeTrue();

        viewModel.MirrorAxisOffsetPixels = 1;
        viewModel.ShowMirrorAxisGuide = false;

        viewModel.EditorWorkspace.MirrorAxisOffsetPixels.Should().Be(1);
        viewModel.EditorWorkspace.ShowMirrorAxisGuide.Should().BeFalse();
    }

    [Fact]
    public async Task UndoRedoShouldSynchronizeMirrorAxisWithConfigAndWorkspace()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: new StubFileDialogService { DmiPath = "sprite.dmi" },
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.MirrorAxisOffsetPixels = 1;

        viewModel.UndoCommand.Execute(null);
        await viewModel.PersistWorkspaceSettingsAsync();

        viewModel.MirrorAxisOffsetPixels.Should().Be(0);
        session.CurrentConfig!.EditorSettings.MirrorAxisOffsetPixels.Should().Be(0);
        settingsRepository.Saved!.MirrorAxisOffsetPixels.Should().Be(0);

        viewModel.RedoCommand.Execute(null);
        await viewModel.PersistWorkspaceSettingsAsync();

        viewModel.MirrorAxisOffsetPixels.Should().Be(1);
        session.CurrentConfig.EditorSettings.MirrorAxisOffsetPixels.Should().Be(1);
        settingsRepository.Saved!.MirrorAxisOffsetPixels.Should().Be(1);
    }

    [Fact]
    public async Task ResetActiveConfigShouldClearMappingsWithoutResettingMirrorAxis()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: new StubFileDialogService { DmiPath = "sprite.dmi" },
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.MirrorAxisOffsetPixels = 1;
        ApplySingleMapping(
            viewModel,
            SpriteDirection.South,
            new PixelCoordinate(2, 2),
            new PixelCoordinate(1, 1));

        viewModel.ResetActiveConfigCommand.Execute(null);
        await viewModel.PersistWorkspaceSettingsAsync();

        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().BeEmpty();
        session.CurrentConfig.EditorSettings.MirrorAxisOffsetPixels.Should().Be(1);
        viewModel.MirrorAxisOffsetPixels.Should().Be(1);
        settingsRepository.Saved!.MirrorAxisOffsetPixels.Should().Be(1);
    }

    [Fact]
    public async Task NavigationRailShouldSwitchTheSelectedSection()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();

        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.NavigationRail.SelectedSection = ShellSectionKind.Batch;

        viewModel.SelectedShellSection.Should().Be(ShellSectionKind.Batch);
        viewModel.NavigationRail.SelectedSection.Should().Be(ShellSectionKind.Batch);

        viewModel.SelectedShellSection = ShellSectionKind.Editor;

        viewModel.NavigationRail.SelectedSection.Should().Be(ShellSectionKind.Editor);
        viewModel.SelectedShellSectionIndex.Should().Be((int)ShellSectionKind.Editor);
    }

    [Fact]
    public async Task OpenDmiShouldEnterEditorWithATwoByTwoMatrixForFourDirections()
    {
        await AssertOpenDmiMatrixLayoutAsync(SupportedDirectionSet.Four, 2);
    }

    [Fact]
    public async Task OpenDmiShouldEnterEditorWithAFourByTwoMatrixForEightDirections()
    {
        await AssertOpenDmiMatrixLayoutAsync(SupportedDirectionSet.Eight, 4);
    }

    [Fact]
    public async Task ParallelScopeShouldSplitLargeCanvasesByCanonicalDirectionPair()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.ShowAllDirectionDisplayPicker.Should().BeFalse();
        viewModel.SelectedDirection = SpriteDirection.North;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;

        viewModel.ShowAllDirectionDisplayPicker.Should().BeFalse();
        viewModel.EditorSurfaceGridRows.Should().Be(2);
        viewModel.EditorSurfaceGridColumns.Should().Be(1);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.North);
        viewModel.TargetViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.North);
        viewModel.TargetViewportSurfaces.Single(surface => surface.Direction == SpriteDirection.North).IsActive.Should().BeTrue();

        viewModel.SelectedDirection = SpriteDirection.West;

        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.East, SpriteDirection.West);
        viewModel.TargetViewportSurfaces.Single(surface => surface.Direction == SpriteDirection.West).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AllScopeShouldToggleDisplayedLargeCanvasDirections()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.SelectedDirectionScope = DirectionScope.All;

        // Initial state: all 4 directions are display-selected
        viewModel.ShowAllDirectionDisplayPicker.Should().BeTrue();
        viewModel.DirectionDisplaySelectorItems.Select(item => item.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.East, SpriteDirection.North, SpriteDirection.West);
        viewModel.DirectionNavigatorItems.Should().OnlyContain(item => item.IsDisplaySelected);
        viewModel.EditorSurfaceGridRows.Should().Be(2);
        viewModel.EditorSurfaceGridColumns.Should().Be(2);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.East, SpriteDirection.North, SpriteDirection.West);

        // Toggle off East → 3 selected → all 4 shown (3+ falls back to all)
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.East);

        viewModel.EditorSurfaceGridRows.Should().Be(2);
        viewModel.EditorSurfaceGridColumns.Should().Be(2);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.East, SpriteDirection.North, SpriteDirection.West);
        viewModel.DirectionNavigatorItems.Single(item => item.Direction == SpriteDirection.East).IsDisplaySelected.Should().BeFalse();

        // Toggle off West → 2 selected → filtered to {South, North}
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.West);

        viewModel.EditorSurfaceGridRows.Should().Be(2);
        viewModel.EditorSurfaceGridColumns.Should().Be(1);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.North);
        viewModel.DirectionNavigatorItems.Where(item => item.IsDisplaySelected).Select(item => item.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.North);

        // Toggle off South → 1 selected → filtered to {North}
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.South);

        viewModel.EditorSurfaceGridRows.Should().Be(1);
        viewModel.EditorSurfaceGridColumns.Should().Be(1);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should().Equal(SpriteDirection.North);
        viewModel.DirectionNavigatorItems.Single(item => item.Direction == SpriteDirection.North).IsDisplaySelected.Should().BeTrue();

        // Re-add removed directions one by one
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.East);
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.West);
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.South);

        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.East, SpriteDirection.North, SpriteDirection.West);
        viewModel.DirectionNavigatorItems.Should().OnlyContain(item => item.IsDisplaySelected);

        // Toggle all off: remove North, East, West, South → 0 selected → show all but !IsDisplaySelected
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.North);
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.East);
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.West);
        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.South);

        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.East, SpriteDirection.North, SpriteDirection.West);
        viewModel.DirectionNavigatorItems.Should().OnlyContain(item => !item.IsDisplaySelected);
    }

    [Fact]
    public async Task MultiDirectionInactiveSourceCanvasesShouldHideByDefault()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.SelectedDirectionScope = DirectionScope.Parallel;

        viewModel.HasMultipleDirectionViewportSurfaces.Should().BeTrue();
        viewModel.ShowSourceViewportPane.Should().BeTrue();

        viewModel.SelectedEditorTool = EditorTool.Move;

        viewModel.IsSourceReferenceDimmed.Should().BeTrue();
        viewModel.ShowSourceViewportPane.Should().BeFalse();

        viewModel.SelectedEditorTool = EditorTool.Select;

        viewModel.ShowSourceViewportPane.Should().BeFalse();

        viewModel.SelectedEditorTool = EditorTool.RestoreArea;

        viewModel.ShowSourceViewportPane.Should().BeFalse();

        viewModel.SelectedEditorTool = EditorTool.Fill;

        viewModel.IsSourceReferenceDimmed.Should().BeFalse();
        viewModel.ShowSourceViewportPane.Should().BeTrue();
    }

    [Fact]
    public async Task HideInactiveSourceCanvasesSettingShouldKeepSourceVisible()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.SelectedDirectionScope = DirectionScope.All;
        viewModel.SelectedEditorTool = EditorTool.Move;

        viewModel.ShowSourceViewportPane.Should().BeFalse();

        viewModel.HideInactiveSourceCanvases = false;

        viewModel.ShowSourceViewportPane.Should().BeTrue();
    }

    [Fact]
    public async Task FitMultipleDirectionCanvasesSettingShouldToggleViewportLayout()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.SelectedDirectionScope = DirectionScope.All;

        viewModel.HasMultipleDirectionViewportSurfaces.Should().BeTrue();
        viewModel.UseFittedDirectionViewportLayout.Should().BeTrue();
        viewModel.UseScrollableDirectionViewportLayout.Should().BeFalse();

        viewModel.FitMultipleDirectionCanvasesToViewport = false;

        viewModel.UseFittedDirectionViewportLayout.Should().BeFalse();
        viewModel.UseScrollableDirectionViewportLayout.Should().BeTrue();

        viewModel.SelectedDirectionScope = DirectionScope.Single;

        viewModel.HasMultipleDirectionViewportSurfaces.Should().BeFalse();
        viewModel.UseFittedDirectionViewportLayout.Should().BeFalse();
        viewModel.UseScrollableDirectionViewportLayout.Should().BeTrue();
    }

    [Fact]
    public async Task CreateConfigShouldEnableBatchWorkflowAndStayOnEditorSection()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.CreateConfigCommand.Execute(null);

        viewModel.SelectedShellSection.Should().Be(ShellSectionKind.Editor);
        viewModel.BatchWorkspace.IsAvailable.Should().BeTrue();
        viewModel.ConfigSummary.Should().Contain("mappings");
        viewModel.ConfigSummary.Should().Contain("draft");
        viewModel.EditorWorkspace.RolesSummary.Should().Contain("Base:");
        viewModel.EditorWorkspace.RolesSummary.Should().Contain("Landmark:");
        viewModel.EditorWorkspace.RolesSummary.Should().Contain("Overlay:");
        viewModel.EditorWorkspace.SelectionSummary.Should().Contain("No source pixel selected");
    }

    [Fact]
    public async Task BottomWorkspaceAndPreviewPreferencesShouldPersist()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.SelectedEditorViewportMode = EditorViewportMode.Focused;
        viewModel.SelectedBottomWorkspaceTab = BottomWorkspaceTab.Mappings;
        viewModel.SelectedLanguage = WorkspaceLanguage.Russian;
        viewModel.IsPreviewInspectorExpanded = false;
        viewModel.IsBottomWorkspaceExpanded = false;
        viewModel.HideInactiveSourceCanvases = false;
        viewModel.FitMultipleDirectionCanvasesToViewport = false;

        await viewModel.PersistWorkspaceSettingsAsync();

        settingsRepository.Saved.Should().NotBeNull();
        settingsRepository.Saved!.LastEditorViewportMode.Should().Be(nameof(EditorViewportMode.Focused));
        settingsRepository.Saved.LastBottomWorkspaceTab.Should().Be(nameof(BottomWorkspaceTab.Mappings));
        settingsRepository.Saved.LastUiLanguage.Should().Be(nameof(WorkspaceLanguage.Russian));
        settingsRepository.Saved.IsPreviewInspectorExpanded.Should().BeFalse();
        settingsRepository.Saved.IsBottomWorkspaceExpanded.Should().BeFalse();
        settingsRepository.Saved.HideInactiveSourceCanvases.Should().BeFalse();
        settingsRepository.Saved.FitMultipleDirectionCanvasesToViewport.Should().BeFalse();
    }

    [Fact]
    public async Task EditableSelectionDragShouldNotRebuildActiveEditorSurfacesOrNavigatorItems()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.SelectedEditorTool = EditorTool.Select;
        var sourceSurface = viewModel.ActiveSourceSurface;
        var targetSurface = viewModel.ActiveTargetSurface;
        var navigatorPreview = viewModel.DirectionNavigatorItems[0].PreviewImage;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 3, 3));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 3, 3));

        viewModel.ActiveSourceSurface.Should().BeSameAs(sourceSurface);
        viewModel.ActiveTargetSurface.Should().BeSameAs(targetSurface);
        viewModel.DirectionNavigatorItems[0].PreviewImage.Should().BeSameAs(navigatorPreview);
        viewModel.SelectedAreaBounds.Should().NotBeNull();
        viewModel.HoverSummary.Should().Contain("Editable");
    }

    [Fact]
    public async Task RestoreWorkspaceShouldApplyLastEditorViewportMode()
    {
        var settingsRepository = new InMemorySettingsRepository(
            new WorkspaceSettings(
                "sprite.dmi",
                "config.json",
                "legacy.csv",
                "input",
                "output",
                "draft",
                "base",
                "landmark",
                "overlay",
                SpriteDirection.East,
                OverwritePolicy.FailIfExists,
                null,
                nameof(EditorViewportMode.Focused),
                nameof(BottomWorkspaceTab.Mappings),
            false,
            false,
            nameof(WorkspaceLanguage.Russian)));

        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            configRepository: new SuccessfulConfigRepository(CreateConfig("Restored Config")),
            legacyImporter: new SuccessfulLegacyImporter(CreateConfig("Imported Config")));

        await viewModel.InitializeAsync();

        viewModel.SelectedEditorViewportMode.Should().Be(EditorViewportMode.Focused);
        viewModel.SelectedLanguage.Should().Be(WorkspaceLanguage.Russian);
        viewModel.IsBottomWorkspaceExpanded.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown")]
    [InlineData("1")]
    public async Task RestoreWorkspaceShouldFallbackToMatrixForMissingOrInvalidViewportMode(string? viewportMode)
    {
        var settingsRepository = new InMemorySettingsRepository(
            WorkspaceSettings.Empty with { LastEditorViewportMode = viewportMode });
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.SelectedEditorViewportMode.Should().Be(EditorViewportMode.Matrix);
    }

    [Fact]
    public async Task PersistWorkspaceSettingsAsyncShouldHonorCancellation()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);
        using var cancellationSource = new CancellationTokenSource();

        await viewModel.InitializeAsync();
        cancellationSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => viewModel.PersistWorkspaceSettingsAsync(cancellationSource.Token));
    }

    [Fact]
    public async Task EditorCommandBarShouldSupportDirectToolbarSelection()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.EditorWorkspace.CommandBar.SelectEditorToolCommand.Execute(EditorTool.Move);
        viewModel.EditorWorkspace.CommandBar.SelectDirectionScopeCommand.Execute(DirectionScope.All);

        viewModel.SelectedEditorTool.Should().Be(EditorTool.Move);
        viewModel.SelectedDirectionScope.Should().Be(DirectionScope.All);
        viewModel.EditorWorkspace.CommandBar.IsMoveToolSelected.Should().BeTrue();
        viewModel.EditorWorkspace.CommandBar.IsAllScopeSelected.Should().BeTrue();
    }

    [Fact]
    public async Task EditorViewModeCommandsShouldSelectRequestedMode()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.SetEditableOnlyModeCommand.Execute(null);

        viewModel.EditorViewMode.Should().Be(EditorViewMode.EditableOnly);
        viewModel.IsEditableOnlyMode.Should().BeTrue();
        viewModel.IsCompareSplitMode.Should().BeFalse();

        viewModel.SetCompareSplitModeCommand.Execute(null);

        viewModel.EditorViewMode.Should().Be(EditorViewMode.CompareSplit);
        viewModel.IsCompareSplitMode.Should().BeTrue();
    }

    [Fact]
    public async Task SingleToolShouldMapEditableCellFromSelectedSourcePixel()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 3, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 3, 2));

        viewModel.MappingRows.Should().ContainSingle();
        viewModel.MappingRows[0].Editable.Should().Be(new PixelCoordinate(3, 2));
        viewModel.MappingRows[0].Source.Should().Be(new PixelCoordinate(1, 1));
    }

    [Fact]
    public async Task SourceSelectionShouldNotAutoSelectEditableCoordinate()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));

        viewModel.SelectedSourceCoordinateView.Should().Be(new PixelCoordinate(1, 1));
        viewModel.SelectedTargetCoordinate.Should().BeNull();
    }

    [Fact]
    public async Task SourceHoverShouldHighlightAllLinkedEditablePixels()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(3, 3));
        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(3, 3), new PixelCoordinate(1, 1));
        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(3, 3), new PixelCoordinate(2, 2));

        viewModel.HandleSourceSurfaceHover(new PixelCellViewModel(SpriteDirection.South, 3, 3));

        viewModel.SourceHoveredCoordinate.Should().Be(new PixelCoordinate(3, 3));
        viewModel.EditableHoveredCoordinate.Should().BeNull();
        viewModel.EditableLinkedHoverCoordinates.Should().BeEquivalentTo(
            [new PixelCoordinate(1, 1), new PixelCoordinate(2, 2)]);
        viewModel.HoverMappingSummary.Should().Contain("2 editable pixel");
    }

    [Fact]
    public async Task EditableHoverShouldHighlightMappedSourcePixel()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 3), new PixelCoordinate(2, 1));

        viewModel.HandleTargetSurfaceHover(new PixelCellViewModel(SpriteDirection.South, 2, 1));

        viewModel.EditableHoveredCoordinate.Should().Be(new PixelCoordinate(2, 1));
        viewModel.SourceHoveredCoordinate.Should().BeNull();
        viewModel.SourceLinkedHoverCoordinates.Should().BeEquivalentTo([new PixelCoordinate(0, 3)]);
        viewModel.HoverMappingSummary.Should().Contain("Editable 2,1 <- Source 0,3");
    }

    [Fact]
    public async Task FillToolShouldApplySelectedSourcePixelAcrossEditableArea()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.SelectedEditorTool = EditorTool.Fill;
        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 2, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 2));

        viewModel.MappingRows.Should().HaveCount(4);
        viewModel.MappingRows.Should().OnlyContain(row => row.Source == new PixelCoordinate(0, 1));
        viewModel.SelectedAreaBounds.Should().BeNull();
        viewModel.SelectedAreaSummary.Should().Be("No area selected.");
    }

    [Fact]
    public async Task MirroredFillShouldProjectSelectedSourceForOppositeDirection()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;
        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;
        viewModel.SelectedEditorTool = EditorTool.Fill;
        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 2, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 2));

        AssertDirectionMappings(
            viewModel,
            SpriteDirection.South,
            (new PixelCoordinate(1, 1), new PixelCoordinate(0, 1)),
            (new PixelCoordinate(2, 1), new PixelCoordinate(0, 1)),
            (new PixelCoordinate(1, 2), new PixelCoordinate(0, 1)),
            (new PixelCoordinate(2, 2), new PixelCoordinate(0, 1)));
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.North,
            (new PixelCoordinate(2, 1), new PixelCoordinate(3, 1)),
            (new PixelCoordinate(1, 1), new PixelCoordinate(3, 1)),
            (new PixelCoordinate(2, 2), new PixelCoordinate(3, 1)),
            (new PixelCoordinate(1, 2), new PixelCoordinate(3, 1)));
    }

    [Fact]
    public async Task PaintStrokeShouldInterpolateSparsePointerSamplesAndCommitOneUndoStep()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService,
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Single;
        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 3, 0));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 3, 0));

        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().HaveCount(4);
        session.CanUndo.Should().BeTrue();
        session.Undo().IsSuccess.Should().BeTrue();
        session.CurrentConfig.GetMappings(SpriteDirection.South).Should().BeEmpty();
        session.CanUndo.Should().BeFalse();
    }

    [Fact]
    public async Task CancelledPaintStrokeShouldNotChangeConfigOrHistory()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService,
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Single;
        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 3, 0));
        viewModel.CancelActiveEditorGesture();

        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().BeEmpty();
        session.CanUndo.Should().BeFalse();
        session.CanRedo.Should().BeFalse();
    }

    [Fact]
    public async Task ChangingStateDuringPaintStrokeShouldCancelGestureWithoutHistory()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: new StubFileDialogService { DmiPath = "sprite.dmi" },
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Single;
        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 3, 0));

        viewModel.BaseStateName = "replacement-state";
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 3, 0));

        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().BeEmpty();
        session.CanUndo.Should().BeFalse();
        session.CanRedo.Should().BeFalse();
    }

    [Fact]
    public async Task ChangingToolDuringPaintStrokeShouldCancelGestureWithoutHistory()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: new StubFileDialogService { DmiPath = "sprite.dmi" },
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Single;
        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 3, 0));

        viewModel.SelectedEditorTool = EditorTool.Erase;
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 3, 0));

        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().BeEmpty();
        session.CanUndo.Should().BeFalse();
        session.CanRedo.Should().BeFalse();
    }

    [Fact]
    public async Task FailedEditorGestureShouldLogCompleteOperationContext()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var logger = new RecordingLogger<WorkspaceShellViewModel>();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: new StubFileDialogService { DmiPath = "sprite.dmi" },
            editorSession: session,
            logger: logger);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Erase;
        var invalidCell = new PixelCellViewModel(SpriteDirection.South, 99, 99);

        viewModel.HandleTargetCellPointerDown(invalidCell);
        viewModel.HandleTargetCellPointerUp(invalidCell);

        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().BeEmpty();
        session.CanUndo.Should().BeFalse();
        var entry = logger.Entries.Should().ContainSingle(item => item.Message.Contains("result=failed", StringComparison.Ordinal)).Subject;
        entry.Properties.Should().ContainKey("OperationId");
        entry.Properties.Should().ContainKey("Tool");
        entry.Properties.Should().ContainKey("State");
        entry.Properties.Should().ContainKey("Direction");
        entry.Properties.Should().Contain("Frame", 0);
        entry.Properties.Should().Contain("Scope", DirectionScope.Single);
        entry.Properties.Should().Contain("Applied", 0);
        entry.Properties.Should().ContainKey("Skipped");
        entry.Properties.Should().ContainKey("Message");
    }

    [Fact]
    public async Task EraseAndRestoreShouldHaveSeparateTransparentAndOriginalPixelSemantics()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService,
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        var cell = new PixelCellViewModel(SpriteDirection.South, 1, 1);

        viewModel.SelectedEditorTool = EditorTool.Erase;
        viewModel.HandleTargetCellPointerDown(cell);
        viewModel.HandleTargetCellPointerUp(cell);
        session.CurrentConfig!.IsTransparent(SpriteDirection.South, cell.Coordinate).Should().BeTrue();

        viewModel.SelectedEditorTool = EditorTool.Restore;
        viewModel.HandleTargetCellPointerDown(cell);
        viewModel.HandleTargetCellPointerUp(cell);
        session.CurrentConfig.GetMappings(SpriteDirection.South).Should().NotContain(
            mapping => mapping.Source == cell.Coordinate);
        session.CurrentConfig.GetEffectiveTarget(SpriteDirection.South, cell.Coordinate).Should().Be(cell.Coordinate);
    }

    [Fact]
    public async Task ParallelScopeShouldMirrorEditableAndSourceCoordinatesUsingExactCenter()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var session = new EditorSession();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService,
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.HideInactiveSourceCanvases = false;
        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;
        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 2, 2));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 0, 1));

        viewModel.SourceViewportSurfaces
            .Single(surface => surface.Direction == SpriteDirection.North)
            .TransformedSelectedSourceCoordinate
            .Should()
            .Be(new PixelCoordinate(1, 2));

        AssertDirectionMappings(
            viewModel,
            SpriteDirection.South,
            (new PixelCoordinate(0, 1), new PixelCoordinate(2, 2)));
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.North,
            (new PixelCoordinate(3, 1), new PixelCoordinate(1, 2)));
        AssertDirectionMappings(viewModel, SpriteDirection.East);
        AssertDirectionMappings(viewModel, SpriteDirection.West);

        session.Undo().IsSuccess.Should().BeTrue();
        AssertDirectionMappings(viewModel, SpriteDirection.South);
        AssertDirectionMappings(viewModel, SpriteDirection.North);
        session.Redo().IsSuccess.Should().BeTrue();
        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().ContainSingle();
        session.CurrentConfig.GetMappings(SpriteDirection.North).Should().ContainSingle();
    }

    [Fact]
    public async Task MirroredSetSourceShouldSkipDirectionWhenSourceProjectionIsOutOfBounds()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.HideInactiveSourceCanvases = false;
        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 1;
        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 2));

        viewModel.SourceViewportSurfaces
            .Single(surface => surface.Direction == SpriteDirection.North)
            .TransformedSelectedSourceCoordinate
            .Should()
            .BeNull();

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 2, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 1));

        viewModel.StatusMessage.Should().Contain("Skipped 1 out-of-bounds projection");
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.South,
            (new PixelCoordinate(2, 1), new PixelCoordinate(0, 2)));
        AssertDirectionMappings(viewModel, SpriteDirection.North);
    }

    [Fact]
    public async Task AllScopeShouldUseOrientationParity()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;
        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.All;

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 2, 2));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 0, 1));

        AssertDirectionMappings(
            viewModel,
            SpriteDirection.South,
            (new PixelCoordinate(0, 1), new PixelCoordinate(2, 2)));
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.North,
            (new PixelCoordinate(3, 1), new PixelCoordinate(1, 2)));
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.East,
            (new PixelCoordinate(0, 1), new PixelCoordinate(2, 2)));
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.West,
            (new PixelCoordinate(3, 1), new PixelCoordinate(1, 2)));
    }

    [Fact]
    public async Task EightDirectionParallelScopeShouldMirrorDiagonalPairOnly()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Eight),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;
        viewModel.SelectedDirection = SpriteDirection.SouthEast;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 3, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 2));

        AssertDirectionMappings(
            viewModel,
            SpriteDirection.SouthEast,
            (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.NorthWest,
            (new PixelCoordinate(3, 2), new PixelCoordinate(0, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.SouthWest);
        AssertDirectionMappings(viewModel, SpriteDirection.NorthEast);
        AssertDirectionMappings(viewModel, SpriteDirection.South);
        AssertDirectionMappings(viewModel, SpriteDirection.North);
        AssertDirectionMappings(viewModel, SpriteDirection.East);
        AssertDirectionMappings(viewModel, SpriteDirection.West);
    }

    [Fact]
    public async Task EightDirectionAllScopeShouldUseOrientationParityAcrossFamilies()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Eight),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;
        viewModel.SelectedDirection = SpriteDirection.SouthEast;
        viewModel.SelectedDirectionScope = DirectionScope.All;

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 3, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 2));

        AssertDirectionMappings(viewModel, SpriteDirection.SouthEast, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.NorthWest, (new PixelCoordinate(3, 2), new PixelCoordinate(0, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.SouthWest, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.NorthEast, (new PixelCoordinate(3, 2), new PixelCoordinate(0, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.South, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.North, (new PixelCoordinate(3, 2), new PixelCoordinate(0, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.East, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.West, (new PixelCoordinate(3, 2), new PixelCoordinate(0, 1)));
    }

    [Fact]
    public async Task SelectToolShouldMoveEditableSelectionToNewLocation()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 2));

        viewModel.SelectedEditorTool = EditorTool.Select;
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 1, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 2));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 2, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 1));

        viewModel.MappingRows.Should().Contain(row => row.Editable == new PixelCoordinate(2, 1) && row.Source == new PixelCoordinate(0, 0));
        viewModel.MappingRows.Should().Contain(row => row.Editable == new PixelCoordinate(2, 2) && row.Source == new PixelCoordinate(0, 1));
        viewModel.MappingRows.Should().NotContain(row => row.Editable == new PixelCoordinate(1, 1));
        viewModel.MappingRows.Should().NotContain(row => row.Editable == new PixelCoordinate(1, 2));
        viewModel.SelectedAreaBounds.Should().BeNull();
    }

    [Fact]
    public async Task SelectToolShouldKeepDragAliveWhenPointerLeavesSurface()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Select;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 3, 3));
        viewModel.HandleTargetSurfacePointerLeave();
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 3, 3));

        viewModel.SelectedAreaBounds.Should().Be(new PixelAreaBounds(0, 0, 3, 3));
        viewModel.SelectedAreaSummary.Should().Contain("4x4");
    }

    [Fact]
    public async Task SelectToolShouldMoveOverlappingEditableSelectionUsingExplicitMappingsOnly()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(3, 3), new PixelCoordinate(0, 1));
        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(2, 2), new PixelCoordinate(1, 1));
        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 3), new PixelCoordinate(2, 1));

        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Select;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 2, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 1));

        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(1, 1), new PixelCoordinate(3, 3));
        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(2, 1), new PixelCoordinate(2, 2));
        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(3, 1), new PixelCoordinate(0, 3));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 1));
    }

    [Fact]
    public async Task SelectToolShouldRestoreSourceBackgroundForMovedSelectionOrigins()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(1, 1));
        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 2), new PixelCoordinate(3, 2));

        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Select;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 2, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 2));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 2, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 1));

        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(2, 1), new PixelCoordinate(0, 0));
        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(3, 1), new PixelCoordinate(2, 1));
        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(2, 2), new PixelCoordinate(1, 2));
        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(3, 2), new PixelCoordinate(2, 2));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.South, new PixelCoordinate(1, 1));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.South, new PixelCoordinate(1, 2));
    }

    [Fact]
    public async Task SelectToolShouldKeepFullOverlappingSelectionMaterialized()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(3, 3), new PixelCoordinate(0, 1));
        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(2, 2), new PixelCoordinate(1, 1));
        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 3), new PixelCoordinate(2, 1));

        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Select;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 2, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 1, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 1));

        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.MappingRows.Should().HaveCount(3);
        viewModel.MappingRows.Should().ContainSingle(row =>
            row.Editable == new PixelCoordinate(1, 1) &&
            row.Source == new PixelCoordinate(3, 3));
        viewModel.MappingRows.Should().ContainSingle(row =>
            row.Editable == new PixelCoordinate(2, 1) &&
            row.Source == new PixelCoordinate(2, 2));
        viewModel.MappingRows.Should().ContainSingle(row =>
            row.Editable == new PixelCoordinate(3, 1) &&
            row.Source == new PixelCoordinate(0, 3));
    }

    [Fact]
    public async Task SelectToolShouldMoveUnmappedPixelsWithoutTransparentOrigins()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Select;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 1, 0));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 0));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 1, 0));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 0));

        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 0));
        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(1, 0), new PixelCoordinate(0, 0));
        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(2, 0), new PixelCoordinate(1, 0));

        var sourceSurface = viewModel.ActiveSourceSurface!;
        var editableSurface = viewModel.ActiveTargetSurface!;
        GetSurfaceColor(editableSurface, 0, 0).Should()
            .Be(GetSurfaceColor(sourceSurface, 0, 0));
    }

    [Fact]
    public async Task ScopedMoveShouldUseDirectionSpecificPayload()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 1), new PixelCoordinate(0, 0));
        ApplySingleMapping(viewModel, SpriteDirection.North, new PixelCoordinate(3, 2), new PixelCoordinate(3, 0));

        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;
        viewModel.SelectedEditorTool = EditorTool.Move;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 1, 0));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 0));

        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(1, 0), new PixelCoordinate(0, 1));
        AssertDirectionHasMapping(viewModel, SpriteDirection.North, new PixelCoordinate(2, 0), new PixelCoordinate(3, 2));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 0));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.North, new PixelCoordinate(3, 0));
    }

    [Fact]
    public async Task ScopedMoveShouldProjectExactMirrorAtRightEdge()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 1), new PixelCoordinate(3, 0));
        ApplySingleMapping(viewModel, SpriteDirection.North, new PixelCoordinate(2, 1), new PixelCoordinate(0, 0));

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;
        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;
        viewModel.SelectedEditorTool = EditorTool.Move;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 3, 0));

        viewModel.TargetViewportSurfaces
            .Single(surface => surface.Direction == SpriteDirection.North)
            .TransformedSelectedTargetCoordinate
            .Should()
            .Be(new PixelCoordinate(0, 0));

        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 2, 0));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 0));

        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(2, 0), new PixelCoordinate(0, 1));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.South, new PixelCoordinate(3, 0));
        AssertDirectionHasMapping(viewModel, SpriteDirection.North, new PixelCoordinate(1, 0), new PixelCoordinate(2, 1));
    }

    [Fact]
    public async Task SelectToolShouldProjectCompleteRightEdgeSelectionBounds()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;
        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.All;
        viewModel.SelectedEditorTool = EditorTool.Select;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 3, 0));

        viewModel.SelectedAreaBounds.Should().Be(new PixelAreaBounds(0, 0, 3, 0));
        viewModel.TargetViewportSurfaces
            .Single(surface => surface.Direction == SpriteDirection.North)
            .TransformedSelectedAreaBounds
            .Should()
            .Be(new PixelAreaBounds(0, 0, 3, 0));
        viewModel.TargetViewportSurfaces
            .Single(surface => surface.Direction == SpriteDirection.West)
            .TransformedSelectedAreaBounds
            .Should()
            .Be(new PixelAreaBounds(0, 0, 3, 0));
    }

    [Fact]
    public async Task EightDirectionScopedMoveShouldUseDirectionSpecificPayloadForDiagonalPair()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Eight),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;

        ApplySingleMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(0, 1), new PixelCoordinate(0, 0));
        ApplySingleMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(3, 2), new PixelCoordinate(3, 0));

        viewModel.SelectedDirection = SpriteDirection.SouthEast;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;
        viewModel.SelectedEditorTool = EditorTool.Move;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.SouthEast, 1, 0));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.SouthEast, 1, 0));

        AssertDirectionHasMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(1, 0), new PixelCoordinate(0, 1));
        AssertDirectionHasMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(2, 0), new PixelCoordinate(3, 2));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(0, 0));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(3, 0));
    }

    [Fact]
    public async Task EightDirectionScopedSelectShouldUseDirectionSpecificPayloadForDiagonalPair()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Eight),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.MirrorAcrossDirections = true;
        viewModel.MirrorAxisOffsetPixels = 0;

        ApplySingleMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(0, 0), new PixelCoordinate(0, 1));
        ApplySingleMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(1, 0), new PixelCoordinate(0, 2));
        ApplySingleMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(3, 0), new PixelCoordinate(3, 1));
        ApplySingleMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(2, 0), new PixelCoordinate(3, 2));

        viewModel.SelectedDirection = SpriteDirection.SouthEast;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;
        viewModel.SelectedEditorTool = EditorTool.Select;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 2));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 1));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.SouthEast, 1, 1));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.SouthEast, 1, 1));

        AssertDirectionHasMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(1, 1), new PixelCoordinate(0, 0));
        AssertDirectionHasMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(1, 2), new PixelCoordinate(1, 0));
        AssertDirectionHasMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(2, 1), new PixelCoordinate(3, 0));
        AssertDirectionHasMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(2, 2), new PixelCoordinate(2, 0));
    }

    [Fact]
    public async Task EditableSurfaceShouldRenderAppliedResultColors()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var previewBuilder = new FixedPreviewBuilder();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            stateFrameReader: new CoordinateStateFrameReader(),
            previewBuilder: previewBuilder,
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedExplorerState = "idle";
        viewModel.UseSelectedStateAsBaseCommand.Execute(null);
        await viewModel.BuildPreviewCommand.ExecuteAsync(null);

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 2, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 2));

        viewModel.ActiveSourceSurface.Should().NotBeNull();
        viewModel.ActiveTargetSurface.Should().NotBeNull();
        var sourceSurface = viewModel.ActiveSourceSurface!;
        var editableSurface = viewModel.ActiveTargetSurface!;
        var sourceColor = GetSurfaceColor(sourceSurface, 0, 0);
        var originalEditableColor = GetSurfaceColor(sourceSurface, 2, 2);
        var remappedEditableColor = GetSurfaceColor(editableSurface, 2, 2);

        remappedEditableColor.Should().Be(sourceColor);
        remappedEditableColor.Should().NotBe(originalEditableColor);
    }

    [Fact]
    public async Task ImportedStateDefaultsShouldAssignFirstToSourceAndSecondToEditableOnly()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four, "c", "a", "b"),
            stateFrameReader: new SolidStateFrameReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.ImportedDmiStateItems.Select(item => item.StateName).Should().Equal("c", "a", "b");
        viewModel.AvailableStates.Should().Equal("c", "a", "b");
        viewModel.ImportedDmiStateItems[0].IsSourceAssigned.Should().BeTrue();
        viewModel.ImportedDmiStateItems[0].IsEditableAssigned.Should().BeFalse();
        viewModel.ImportedDmiStateItems[0].PlacementMode.Should().Be(ImportedStatePlacementMode.Overlay);
        viewModel.ImportedDmiStateItems[0].Order.Should().Be(0);
        viewModel.ImportedDmiStateItems[0].OpacityPercent.Should().Be(100);
        viewModel.ImportedDmiStateItems[1].IsSourceAssigned.Should().BeFalse();
        viewModel.ImportedDmiStateItems[1].IsEditableAssigned.Should().BeFalse();
        viewModel.ImportedDmiStateItems[1].PlacementMode.Should().Be(ImportedStatePlacementMode.Overlay);
        viewModel.ImportedDmiStateItems[1].Order.Should().Be(1);
        viewModel.ImportedDmiStateItems[1].OpacityPercent.Should().Be(100);
        viewModel.ImportedDmiStateItems[2].IsSourceAssigned.Should().BeFalse();
        viewModel.ImportedDmiStateItems[2].IsEditableAssigned.Should().BeFalse();
        viewModel.ImportedDmiStateItems[2].PlacementMode.Should().Be(ImportedStatePlacementMode.Overlay);
        viewModel.ImportedDmiStateItems[2].Order.Should().Be(2);
        viewModel.ImportedDmiStateItems[2].OpacityPercent.Should().Be(100);
    }

    [Fact]
    public async Task ImportedStateSurfaceAssignmentsShouldAffectOnlySelectedCanvas()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four, "a", "b"),
            stateFrameReader: new SolidStateFrameReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        // По умолчанию Editable выключен; включаем для "b" вручную
        viewModel.ToggleImportedStateEditableCommand.Execute(viewModel.ImportedDmiStateItems[1]);

        GetSurfaceColor(viewModel.ActiveSourceSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("a"));
        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("b"));

        viewModel.ToggleImportedStateSourceCommand.Execute(viewModel.ImportedDmiStateItems[0]);

        GetSurfaceColor(viewModel.ActiveSourceSurface!, 0, 0).Should().Be(System.Windows.Media.Color.FromRgb(244, 239, 231));
        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("b"));

        viewModel.ToggleImportedStateEditableCommand.Execute(viewModel.ImportedDmiStateItems[1]);

        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(System.Windows.Media.Color.FromRgb(244, 239, 231));
    }

    [Fact]
    public async Task ClearImportedStatesShouldLeaveCanvasesEmpty()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four, "a", "b"),
            stateFrameReader: new SolidStateFrameReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.ImportedDmiStateItems.Should().NotBeEmpty();

        await viewModel.ClearImportedStatesCommand.ExecuteAsync(null);

        viewModel.ImportedDmiStateItems.Should().BeEmpty();
        GetSurfaceColor(viewModel.ActiveSourceSurface!, 0, 0).Should().Be(NeutralSurfaceColor());
        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(NeutralSurfaceColor());
    }

    [Fact]
    public async Task SourceBackgroundStateShouldNotCompositeOverPreviewBase()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var previewBuilder = new FixedPreviewBuilder();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four, "a", "b"),
            stateFrameReader: new SparseStateFrameReader(),
            previewBuilder: previewBuilder,
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedExplorerState = "a";
        viewModel.UseSelectedStateAsBaseCommand.Execute(null);
        await viewModel.BuildPreviewCommand.ExecuteAsync(null);

        viewModel.ToggleImportedStateBackgroundCommand.Execute(viewModel.ImportedDmiStateItems[0]);

        GetSurfaceColor(viewModel.ActiveSourceSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("a"));
        GetSurfaceColor(viewModel.ActiveSourceSurface!, 1, 1).Should().Be(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
    }

    [Fact]
    public async Task ImportedStateOpacityShouldScaleOnlyAssignedCanvas()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four, "a", "b"),
            stateFrameReader: new SolidStateFrameReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        // По умолчанию Editable выключен; включаем для "b" вручную
        viewModel.ToggleImportedStateEditableCommand.Execute(viewModel.ImportedDmiStateItems[1]);

        var sourceState = viewModel.ImportedDmiStateItems[0];
        sourceState.OpacityPercent = 50;

        var sourceColor = GetSurfaceColor(viewModel.ActiveSourceSurface!, 0, 0);
        sourceColor.A.Should().Be(128);
        sourceColor.R.Should().Be(SolidStateFrameReader.ColorForState("a").R);
        sourceColor.G.Should().Be(SolidStateFrameReader.ColorForState("a").G);
        sourceColor.B.Should().Be(SolidStateFrameReader.ColorForState("a").B);
        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("b"));

        sourceState.OpacityPercent = 0;

        GetSurfaceColor(viewModel.ActiveSourceSurface!, 0, 0).Should().Be(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("b"));
    }

    [Fact]
    public async Task ImportedStatePlacementAndOrderShouldControlCompositionWithinAssignedCanvas()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four, "a", "b", "c"),
            stateFrameReader: new SolidStateFrameReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        var first = viewModel.ImportedDmiStateItems[0];
        var second = viewModel.ImportedDmiStateItems[1];
        var third = viewModel.ImportedDmiStateItems[2];

        first.IsEditableAssigned = true;
        second.IsEditableAssigned = false;
        third.IsEditableAssigned = true;
        first.PlacementMode = ImportedStatePlacementMode.Overlay;
        third.PlacementMode = ImportedStatePlacementMode.Overlay;
        first.Order = 0;
        third.Order = 5;

        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("c"));

        first.Order = 10;

        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("a"));

        viewModel.ToggleImportedStateBackgroundCommand.Execute(first);

        first.IsSourceAssigned.Should().BeTrue();
        first.IsEditableAssigned.Should().BeTrue();
        first.PlacementMode.Should().Be(ImportedStatePlacementMode.Background);
        GetSurfaceColor(viewModel.ActiveTargetSurface!, 0, 0).Should().Be(SolidStateFrameReader.ColorForState("c"));
    }

    [Fact]
    public async Task EditableSurfaceMappingShouldUseSourceLayersAsColorSource()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four, "a", "b"),
            stateFrameReader: new SparseStateFrameReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.ImportedDmiStateItems.Should().HaveCount(2);
        viewModel.ImportedDmiStateItems[0].StateName.Should().Be("a");
        viewModel.ImportedDmiStateItems[1].StateName.Should().Be("b");

        // По умолчанию Editable выключен; включаем "b" как визуальный оверлей
        viewModel.ToggleImportedStateEditableCommand.Execute(viewModel.ImportedDmiStateItems[1]);

        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedExplorerState = "idle";
        viewModel.UseSelectedStateAsBaseCommand.Execute(null);
        await viewModel.BuildPreviewCommand.ExecuteAsync(null);

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 2, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 2, 2));

        viewModel.ActiveTargetSurface.Should().NotBeNull();
        var editableSurface = viewModel.ActiveTargetSurface!;
        var mappedColor = GetSurfaceColor(editableSurface, 2, 2);

        // sourceReferenceImage строится из Source-слоёв (state "a"),
        // поэтому маппинг читает цвет из state "a", а не из state "b".
        mappedColor.Should().Be(SolidStateFrameReader.ColorForState("a"));
    }

    [Fact]
    public async Task InitializeAsyncShouldRestorePersistedImportedStateAssignments()
    {
        var tempRoot = CreateTempDirectory();
        var dmiPath = Path.Combine(tempRoot, "sprite.dmi");
        var sourcePath = Path.Combine(tempRoot, "states.dmi");
        await File.WriteAllTextAsync(dmiPath, "placeholder");
        await File.WriteAllTextAsync(sourcePath, "placeholder");
        var settingsRepository = new InMemorySettingsRepository(
            WorkspaceSettings.Empty with
            {
                LastOpenedDmiPath = dmiPath,
                ImportedStates =
                [
                    new WorkspaceImportedStateSettings(
                        "a",
                        sourcePath,
                        "states.dmi",
                        IsSourceAssigned: true,
                        IsEditableAssigned: false,
                        PlacementMode: "Background",
                        Order: 4,
                        OpacityPercent: 65),
                    new WorkspaceImportedStateSettings(
                        "missing",
                        Path.Combine(tempRoot, "missing.dmi"),
                        "missing.dmi",
                        IsSourceAssigned: false,
                        IsEditableAssigned: true,
                        PlacementMode: "Overlay",
                        Order: 5)
                ]
            });
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four, "a", "b"),
            stateFrameReader: new SolidStateFrameReader());

        await viewModel.InitializeAsync();

        viewModel.ImportedDmiStateItems.Should().HaveCount(2);
        var restored = viewModel.ImportedDmiStateItems[0];
        restored.StateName.Should().Be("a");
        restored.SourcePath.Should().Be(sourcePath);
        restored.IsSourceAssigned.Should().BeTrue();
        restored.IsEditableAssigned.Should().BeFalse();
        restored.PlacementMode.Should().Be(ImportedStatePlacementMode.Background);
        restored.Order.Should().Be(4);
        restored.OpacityPercent.Should().Be(65);
        restored.IsValid.Should().BeTrue();
        GetSurfaceColor(viewModel.ActiveSourceSurface!, 0, 0).A.Should().Be(166);

        var invalid = viewModel.ImportedDmiStateItems[1];
        invalid.StateName.Should().Be("missing");
        invalid.IsValid.Should().BeFalse();
        invalid.ValidationMessage.Should().Contain("Source DMI file was not found");
    }

    [Fact]
    public async Task MergeImportedStatesShouldWarmUpCacheForAllSupportedDirections()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            stateFrameReader: new SolidStateFrameReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        var cacheField = typeof(WorkspaceShellViewModel)
            .GetField("_importedStateFrameCache", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var cache = (System.Collections.IDictionary)cacheField.GetValue(viewModel)!;

        var sourcePath = "sprite.dmi";
        var expectedDirections = new[]
        {
            SpriteDirection.South,
            SpriteDirection.North,
            SpriteDirection.East,
            SpriteDirection.West
        };

        foreach (var stateName in new[] { "idle", "blink" })
        {
            foreach (var direction in expectedDirections)
            {
                var key = (sourcePath, stateName, direction, 0, SpriteSourceFormat.Dmi);
                cache.Contains(key).Should().BeTrue(
                    $"Cache should contain key for state '{stateName}', direction {direction}");
            }
        }
    }

    [Fact]
    public async Task EditableSurfaceShouldNotApplyConfigTwiceWhenCompositePreviewExists()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var previewBuilder = new ConfigApplyingPreviewBuilder();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            stateFrameReader: new CoordinateStateFrameReader(),
            previewBuilder: previewBuilder,
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(1, 0), new PixelCoordinate(0, 0));
        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(2, 2));

        viewModel.SelectedExplorerState = "idle";
        viewModel.UseSelectedStateAsBaseCommand.Execute(null);
        await viewModel.BuildPreviewCommand.ExecuteAsync(null);

        var sourceSurface = viewModel.ActiveSourceSurface!;
        var editableSurface = viewModel.ActiveTargetSurface!;
        var expectedSourceColor = GetSurfaceColor(sourceSurface, 0, 0);
        var doubleAppliedColor = GetSurfaceColor(sourceSurface, 1, 0);

        GetSurfaceColor(editableSurface, 2, 2).Should().Be(expectedSourceColor);
        GetSurfaceColor(editableSurface, 2, 2).Should().NotBe(doubleAppliedColor);
    }

    [Fact]
    public async Task SourceCoordinateCaptionsShouldShowMappedOriginalOnEditablePixels()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        ApplySingleMapping(
            viewModel,
            SpriteDirection.South,
            source: new PixelCoordinate(2, 1),
            editable: new PixelCoordinate(3, 0));

        var captionIndex = viewModel.ActiveTargetSurface!.GetIndex(3, 0);
        viewModel.ActiveTargetSurface.Captions[captionIndex].Should().BeEmpty();

        viewModel.ShowSourceCoordinateCaptions = true;

        viewModel.ActiveTargetSurface!.Captions[captionIndex].Should().Be("2,1");
        viewModel.EditorWorkspace.ShowGridCaptions.Should().BeTrue();
    }

    [Fact]
    public async Task ResumeLastWorkspaceShouldRestoreRecentPathsAndReturnToEditor()
    {
        var settingsRepository = new InMemorySettingsRepository(
            new WorkspaceSettings(
                "sprite.dmi",
                "config.json",
                "legacy.csv",
                "input",
                "output",
                "draft",
                "base",
                "landmark",
                "overlay",
                SpriteDirection.East,
                OverwritePolicy.FailIfExists,
                null,
                nameof(EditorViewportMode.Focused),
                nameof(BottomWorkspaceTab.Mappings),
            false,
            false));

        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            configRepository: new SuccessfulConfigRepository(CreateConfig("Restored Config")),
            legacyImporter: new SuccessfulLegacyImporter(CreateConfig("Imported Config")));

        await viewModel.InitializeAsync();
        await viewModel.ResumeLastWorkspaceCommand.ExecuteAsync(null);

        viewModel.SelectedShellSection.Should().Be(ShellSectionKind.Editor);
        viewModel.ConfigSummary.Should().Contain("Restored Config");
        viewModel.SelectedEditorViewportMode.Should().Be(EditorViewportMode.Focused);
        viewModel.SelectedBottomWorkspaceTab.Should().Be(BottomWorkspaceTab.Mappings);
        viewModel.IsBottomWorkspaceExpanded.Should().BeFalse();
        viewModel.StartTab.HasRecentWorkspace.Should().BeTrue();
    }

    [Fact]
    public async Task BatchWorkspaceShouldExposePipelineStateAndResults()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var tempRoot = CreateTempDirectory();
        var inputDirectory = Path.Combine(tempRoot, "input");
        var outputDirectory = Path.Combine(tempRoot, "output");
        Directory.CreateDirectory(inputDirectory);
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "sprite.dmi"), "placeholder");

        var dialogService = new StubFileDialogService
        {
            DmiPath = "sprite.dmi",
            BatchDirectory = inputDirectory
        };

        var batchService = new RecordingBatchProcessingService();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            batchProcessingService: batchService,
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.BatchInputDirectory = inputDirectory;
        viewModel.BatchOutputDirectory = outputDirectory;

        await viewModel.RunBatchCommand.ExecuteAsync(null);

        batchService.CallCount.Should().Be(1);
        viewModel.SelectedShellSection.Should().Be(ShellSectionKind.Batch);
        viewModel.BatchWorkspace.IsAvailable.Should().BeTrue();
        viewModel.BatchWorkspace.SourceTreeItems.Should().NotBeEmpty();
        viewModel.BatchWorkspace.StateStripItems.Should().NotBeEmpty();
        viewModel.BatchWorkspace.ConfigQueueItems.Should().NotBeEmpty();
        viewModel.BatchResults.Should().NotBeEmpty();
        viewModel.BatchWorkspace.BatchSummary.Should().Contain("processed");
    }

    [Fact]
    public async Task BatchWorkspaceShouldTogglePreviewDirectionMode()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: new StubFileDialogService { DmiPath = "sprite.dmi" });

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.BatchWorkspace.ShowPreviewDirectionModeSelector.Should().BeTrue();
        viewModel.BatchWorkspace.IsSingleDirectionPreviewSelected.Should().BeTrue();
        viewModel.BatchWorkspace.IsAllDirectionsPreviewSelected.Should().BeFalse();

        viewModel.BatchWorkspace.IsAllDirectionsPreviewSelected = true;

        viewModel.SelectedBatchPreviewDirectionMode.Should().Be(BatchPreviewDirectionMode.All);
        viewModel.BatchWorkspace.IsSingleDirectionPreviewSelected.Should().BeFalse();
        viewModel.BatchWorkspace.IsAllDirectionsPreviewSelected.Should().BeTrue();
    }

    [Fact]
    public async Task BatchWorkspaceRunPlanShouldSelectValidFilesAndExcludeOutputDirectory()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var tempRoot = CreateTempDirectory();
        var inputDirectory = Path.Combine(tempRoot, "input");
        var nestedDirectory = Path.Combine(inputDirectory, "nested");
        var outputDirectory = Path.Combine(inputDirectory, "processed");
        Directory.CreateDirectory(nestedDirectory);
        Directory.CreateDirectory(outputDirectory);
        var rootFile = Path.Combine(inputDirectory, "root.dmi");
        var nestedFile = Path.Combine(nestedDirectory, "nested.dmi");
        var outputFile = Path.Combine(outputDirectory, "already-processed.dmi");
        await File.WriteAllTextAsync(rootFile, "placeholder");
        await File.WriteAllTextAsync(nestedFile, "placeholder");
        await File.WriteAllTextAsync(outputFile, "placeholder");

        var batchService = new RecordingBatchProcessingService();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            batchProcessingService: batchService,
            fileDialogService: new StubFileDialogService { DmiPath = "sprite.dmi" });

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.BatchInputDirectory = inputDirectory;
        viewModel.BatchOutputDirectory = outputDirectory;

        viewModel.BatchWorkspace.CandidateFileCount.Should().Be(3);
        viewModel.BatchWorkspace.ValidCandidateFileCount.Should().Be(2);
        viewModel.BatchWorkspace.SelectedCandidateFileCount.Should().Be(2);
        viewModel.BatchWorkspace.CandidateFiles.Should().Contain(file =>
            file.FullPath == outputFile &&
            !file.IsValid &&
            !file.IsSelected &&
            file.ValidationMessage.Contains("Output folder", StringComparison.OrdinalIgnoreCase));

        await viewModel.BatchWorkspace.RunSelectedBatchCommand.ExecuteAsync(null);

        batchService.Request.Should().NotBeNull();
        batchService.Request!.ExplicitFiles.Should().BeEquivalentTo([rootFile, nestedFile]);
    }

    [Fact]
    public async Task RemovingFinalActiveConfigQueueItemShouldCreateFreshDraftForLoadedAsset()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            fileDialogService: dialogService,
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        ApplySingleMapping(
            viewModel,
            SpriteDirection.South,
            new PixelCoordinate(1, 1),
            new PixelCoordinate(2, 2));
        var removedConfig = session.CurrentConfig;
        var activeItem = viewModel.ConfigQueueItems.Single(static item => item.IsActive);

        viewModel.RemoveConfigQueueItemCommand.Execute(activeItem);

        session.CurrentConfig.Should().NotBeNull();
        session.CurrentConfig.Should().NotBeSameAs(removedConfig);
        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().BeEmpty();
        viewModel.ConfigQueueItems.Should().ContainSingle(item => item.IsActive);
        viewModel.MappingRows.Should().BeEmpty();
    }

    [Fact]
    public async Task RemovingFinalActiveConfigQueueItemWithoutLoadedAssetShouldClearCurrentConfig()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var dialogService = new StubFileDialogService { ConfigPath = "config.json" };
        var viewModel = CreateViewModel(
            settingsRepository,
            configRepository: new SuccessfulConfigRepository(CreateConfig("Loaded Only")),
            fileDialogService: dialogService,
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.LoadConfigCommand.ExecuteAsync(null);
        var activeItem = viewModel.ConfigQueueItems.Single(static item => item.IsActive);

        viewModel.RemoveConfigQueueItemCommand.Execute(activeItem);

        session.CurrentConfig.Should().BeNull();
        session.CurrentConfigPath.Should().BeNull();
        viewModel.ConfigQueueItems.Should().BeEmpty();
        viewModel.HasActiveConfig.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivatingModifiedSavedConfigQueueItemShouldPreserveDirtyBaselineUntilSave()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var session = new EditorSession();
        var dialogService = new StubFileDialogService
        {
            DmiPath = "sprite.dmi",
            ConfigPath = "saved.json"
        };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            configRepository: new SuccessfulConfigRepository(CreateConfig("saved")),
            fileDialogService: dialogService,
            editorSession: session);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        await viewModel.LoadConfigCommand.ExecuteAsync(null);
        var savedItem = viewModel.ConfigQueueItems.Single(item =>
            item.SessionSnapshot.ConfigPath == "saved.json");
        var editable = new PixelCoordinate(1, 1);
        var target = new PixelCoordinate(2, 2);
        ApplySingleMapping(viewModel, SpriteDirection.South, target, editable);

        viewModel.CreateConfigCommand.Execute(null);
        viewModel.ActivateConfigQueueItemCommand.Execute(savedItem);

        session.CurrentConfigPath.Should().Be("saved.json");
        session.CurrentConfig!.GetEffectiveTarget(SpriteDirection.South, editable).Should().Be(target);
        session.IsDirty.Should().BeTrue();
        viewModel.ConfigSummary.Should().Contain("modified");

        await viewModel.SaveConfigCommand.ExecuteAsync(null);

        session.IsDirty.Should().BeFalse();
        viewModel.ConfigSummary.Should().NotContain("modified");
    }

    [Fact]
    public void BuildBatchSourceTreeItemsShouldSkipChildDirectoriesWithEnumerationErrors()
    {
        var root = Path.Combine("batch", "root");
        var deniedDirectory = Path.Combine(root, "denied");
        var validDirectory = Path.Combine(root, "valid");
        var validFile = Path.Combine(validDirectory, "sprite.dmi");
        Func<string, IEnumerable<string>> enumerateDirectories = path =>
            string.Equals(path, root, StringComparison.Ordinal)
                ? [deniedDirectory, validDirectory]
                : string.Equals(path, deniedDirectory, StringComparison.Ordinal)
                    ? throw new UnauthorizedAccessException("Denied for test.")
                    : [];
        Func<string, IEnumerable<string>> enumerateFiles = path =>
            string.Equals(path, validDirectory, StringComparison.Ordinal)
                ? [validFile]
                : [];

        var items = BuildBatchSourceTreeItemsForTest(root, enumerateDirectories, enumerateFiles);

        items.Should().ContainSingle(item => item.FullPath == validDirectory);
        items.Should().NotContain(item => item.FullPath == deniedDirectory);
        items.Single().Children.Should().ContainSingle(item => item.FullPath == validFile);
    }

    [Fact]
    public void BuildBatchSourceTreeItemsShouldSkipChildFilesWithEnumerationErrors()
    {
        var root = Path.Combine("batch", "root");
        var validDirectory = Path.Combine(root, "valid");
        var validFile = Path.Combine(root, "sprite.dmi");
        Func<string, IEnumerable<string>> enumerateDirectories = path =>
            string.Equals(path, root, StringComparison.Ordinal)
                ? [validDirectory]
                : [];
        Func<string, IEnumerable<string>> enumerateFiles = path =>
            string.Equals(path, validDirectory, StringComparison.Ordinal)
                ? throw new IOException("I/O failure for test.")
                : string.Equals(path, root, StringComparison.Ordinal)
                    ? [validFile]
                    : [];

        var items = BuildBatchSourceTreeItemsForTest(root, enumerateDirectories, enumerateFiles);

        items.Should().ContainSingle(item => item.FullPath == validFile);
        items.Should().NotContain(item => item.FullPath == validDirectory);
    }

    [Fact]
    public async Task ManualPreviewRefreshShouldHandleMissingOptionalLayers()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var previewBuilder = new RecordingPreviewBuilder();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
            previewBuilder: previewBuilder,
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedExplorerState = "idle";
        viewModel.UseSelectedStateAsBaseCommand.Execute(null);

        await viewModel.BuildPreviewCommand.ExecuteAsync(null);

        previewBuilder.CallCount.Should().BeGreaterThan(0);
        viewModel.PreviewSummary.Should().Contain("missing or not selected");
    }

    [Fact]
    public async Task DocumentWorkspaceShouldResolveChangedAndMissingSourcesBeforeOpeningAtomically()
    {
        var changedSourceId = Guid.NewGuid();
        var missingSourceId = Guid.NewGuid();
        var document = new SpriteDocument(
            Guid.NewGuid(),
            "resolved-project",
            new SpriteResolution(1, 1),
            [
                new SpriteSourceReference(
                    changedSourceId,
                    "changed.png",
                    @"C:\assets\changed.png",
                    SpriteSourceFormat.Png,
                    1,
                    1,
                    1,
                    new string('a', 64)),
                new SpriteSourceReference(
                    missingSourceId,
                    "missing.png",
                    @"C:\assets\missing.png",
                    SpriteSourceFormat.Png,
                    1,
                    1,
                    1,
                    new string('b', 64))
            ],
            [
                new SpriteDocumentState(
                    "idle",
                    SpriteDirectionDepth.One,
                    1,
                    SpriteAnimationMetadata.Static,
                    [
                        new SpriteDocumentFrame(
                            SpriteDirection.South,
                            0,
                            new SpriteFrameReference(
                                changedSourceId,
                                new SpriteSourceRectangle(0, 0, 1, 1)))
                    ])
            ]);
        var repository = new SourceResolutionDocumentRepository(document, changedSourceId, missingSourceId);
        var unusedServices = new UnusedSpriteDocumentServices();
        var documentSession = new SpriteDocumentSession();
        var workflow = new SpriteDocumentWorkflow(
            unusedServices,
            unusedServices,
            repository,
            unusedServices,
            unusedServices,
            documentSession);
        var dialogs = new StubFileDialogService
        {
            SpriteDocumentPath = @"C:\projects\sprite.adaptive-dmi.json",
            SourceChangeChoice = SpriteSourceChangeChoice.AcceptNewFingerprint,
            RelinkSourcePath = @"C:\replacement\missing.png"
        };
        var viewModel = CreateViewModel(
            new InMemorySettingsRepository(WorkspaceSettings.Empty),
            fileDialogService: dialogs,
            spriteDocumentWorkflow: workflow);

        await viewModel.DocumentWorkspace.OpenDocumentCommand.ExecuteAsync(null);

        repository.Requests.Should().HaveCount(3);
        repository.Requests[1].AcceptedChangedSources.Should().Contain(changedSourceId);
        repository.Requests[2].AcceptedChangedSources.Should().Contain(changedSourceId);
        repository.Requests[2].RelinkedSources.Should().ContainKey(missingSourceId)
            .WhoseValue.Should().Be(dialogs.RelinkSourcePath);
        dialogs.SourceChangePromptCount.Should().Be(1);
        dialogs.RelinkPromptCount.Should().Be(1);
        documentSession.CurrentDocument.Should().BeSameAs(document);
        documentSession.IsDirty.Should().BeTrue();
        viewModel.DocumentWorkspace.IsDocumentDirty.Should().BeTrue();
    }

    private static MainWindowViewModel CreateViewModel(
        InMemorySettingsRepository settingsRepository,
        IConfigRepository? configRepository = null,
        ILegacyCsvConfigImporter? legacyImporter = null,
        IDmiReader? dmiReader = null,
        IStateFrameReader? stateFrameReader = null,
        IPreviewBuilder? previewBuilder = null,
        IBatchProcessingService? batchProcessingService = null,
        IFileDialogService? fileDialogService = null,
        EditorSession? editorSession = null,
        ILogger<WorkspaceShellViewModel>? logger = null,
        SpriteDocumentWorkflow? spriteDocumentWorkflow = null)
    {
        var session = editorSession ?? new EditorSession();
        var workspace = new EditorWorkspaceService();

        return new MainWindowViewModel(
            new StartEmptyWorkspaceUseCase(workspace, session),
            new CreateConfigUseCase(session),
            new SaveConfigUseCase(configRepository ?? new NullConfigRepository(), session),
            new LoadConfigUseCase(configRepository ?? new NullConfigRepository(), session),
            new ImportLegacyCsvConfigUseCase(legacyImporter ?? new NullLegacyCsvImporter(), session),
            new LoadDmiFileUseCase(dmiReader ?? new NullDmiReader(), session, workspace),
            new InspectDmiFileUseCase(dmiReader ?? new NullDmiReader()),
            new ReadStateFrameUseCase(stateFrameReader ?? new NullStateFrameReader()),
            new BuildPreviewUseCase(previewBuilder ?? new NullPreviewBuilder(), session),
            new ApplyConfigToDmiBatchUseCase(batchProcessingService ?? new NullBatchProcessingService(), session),
            new UndoChangeUseCase(session),
            new RedoChangeUseCase(session),
            new SetPreviewSelectionUseCase(session),
            new SetSelectedDirectionUseCase(session),
            new ApplyConfigTransformUseCase(session),
            new LoadWorkspaceSettingsUseCase(settingsRepository),
            new SaveWorkspaceSettingsUseCase(settingsRepository),
            new SpriteImageBitmapSourceFactory(),
            fileDialogService ?? new StubFileDialogService(),
            session,
            logger ?? NullLogger<WorkspaceShellViewModel>.Instance,
            spriteDocumentWorkflow);
    }

    private static BatchSourceTreeItemViewModel[] BuildBatchSourceTreeItemsForTest(
        string rootDirectory,
        Func<string, IEnumerable<string>> enumerateDirectories,
        Func<string, IEnumerable<string>> enumerateFiles)
    {
        var method = typeof(WorkspaceShellViewModel)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(static candidate =>
                candidate.Name == "BuildBatchSourceTreeItems" &&
                candidate.GetParameters().Length == 3);
        var result = method.Invoke(null, [rootDirectory, enumerateDirectories, enumerateFiles]);
        return ((IEnumerable<BatchSourceTreeItemViewModel>)result!).ToArray();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static SpriteConfig CreateConfig(string name) =>
        SpriteConfig.CreateEmpty(
            name,
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "tests"));

    private static async Task AssertOpenDmiMatrixLayoutAsync(SupportedDirectionSet directions, int expectedColumns)
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(directions),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.SelectedShellSection.Should().Be(ShellSectionKind.Editor);
        viewModel.EditorWorkspace.IsAvailable.Should().BeTrue();
        viewModel.DirectionMatrixColumns.Should().Be(expectedColumns);
        viewModel.EditorWorkspace.DirectionMatrix.MatrixColumns.Should().Be(expectedColumns);
        viewModel.SelectedBottomWorkspaceTab.Should().Be(BottomWorkspaceTab.Mappings);
        viewModel.EditorWorkspace.LeftRailSummary.Should().Contain("Config:");
    }

    private static void ApplySingleMapping(
        MainWindowViewModel viewModel,
        SpriteDirection direction,
        PixelCoordinate source,
        PixelCoordinate editable)
    {
        viewModel.SelectedDirection = direction;
        viewModel.SelectedDirectionScope = DirectionScope.Single;
        viewModel.SelectedEditorTool = EditorTool.Single;
        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(direction, source.X, source.Y));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(direction, editable.X, editable.Y));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(direction, editable.X, editable.Y));
    }

    private static void AssertDirectionMappings(
        MainWindowViewModel viewModel,
        SpriteDirection direction,
        params (PixelCoordinate Editable, PixelCoordinate? Source)[] expectedMappings)
    {
        viewModel.SelectedDirection = direction;
        viewModel.MappingRows.Should().HaveCount(expectedMappings.Length);
        foreach (var (editable, source) in expectedMappings)
        {
            viewModel.MappingRows.Should().ContainSingle(row => row.Editable == editable && row.Source == source);
        }
    }

    private static void AssertDirectionHasMapping(
        MainWindowViewModel viewModel,
        SpriteDirection direction,
        PixelCoordinate editable,
        PixelCoordinate? source)
    {
        viewModel.SelectedDirection = direction;
        viewModel.MappingRows.Should().Contain(row => row.Editable == editable && row.Source == source);
    }

    private static void AssertDirectionDoesNotHaveMapping(
        MainWindowViewModel viewModel,
        SpriteDirection direction,
        PixelCoordinate editable)
    {
        viewModel.SelectedDirection = direction;
        viewModel.MappingRows.Should().NotContain(row => row.Editable == editable);
    }

    private sealed record RecordedLogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<RecordedLogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values
                    .Where(static pair => pair.Key != "{OriginalFormat}")
                    .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);
            Entries.Add(new RecordedLogEntry(logLevel, eventId, formatter(state, exception), properties));
        }
    }

    private sealed class InMemorySettingsRepository(WorkspaceSettings settings) : ISettingsRepository
    {
        public WorkspaceSettings Current { get; private set; } = settings;

        public WorkspaceSettings? Saved { get; private set; }

        public Task<Result<WorkspaceSettings>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(Current));

        public Task<Result> SaveAsync(WorkspaceSettings settings, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Current = settings;
            Saved = settings;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class NullConfigRepository : IConfigRepository
    {
        public Task<Result<SpriteConfig>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<SpriteConfig>(Errors.NotFound(path)));

        public Task<Result> SaveAsync(string path, SpriteConfig config, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class SuccessfulConfigRepository(SpriteConfig config) : IConfigRepository
    {
        public Task<Result<SpriteConfig>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(config));

        public Task<Result> SaveAsync(string path, SpriteConfig config, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class NullLegacyCsvImporter : ILegacyCsvConfigImporter
    {
        public Task<Result<SpriteConfig>> ImportAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<SpriteConfig>(Errors.NotFound(path)));
    }

    private sealed class SuccessfulLegacyImporter(SpriteConfig config) : ILegacyCsvConfigImporter
    {
        public Task<Result<SpriteConfig>> ImportAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(config));
    }

    private sealed class NullDmiReader : IDmiReader
    {
        public Task<Result<DmiAssetInfo>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<DmiAssetInfo>(Errors.NotFound(path)));
    }

    private sealed class SuccessfulDmiReader : IDmiReader
    {
        private readonly SupportedDirectionSet _directions;
        private readonly string[] _stateNames;

        public SuccessfulDmiReader(SupportedDirectionSet directions, params string[] stateNames)
        {
            _directions = directions;
            _stateNames = stateNames;
        }

        public Task<Result<DmiAssetInfo>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(
                Result.Success(
                    new DmiAssetInfo(
                        "sprite",
                        path,
                        new SpriteResolution(4, 4),
                        _directions,
                        ResolveStates(_stateNames))));

        private static DmiStateInfo[] ResolveStates(string[] requestedStateNames)
        {
            var names = requestedStateNames.Length == 0
                ? ["idle", "blink"]
                : requestedStateNames;
            return names.Select(static name => new DmiStateInfo(name, 1)).ToArray();
        }
    }

    private sealed class NullPreviewBuilder : IPreviewBuilder
    {
        public Task<Result<PreviewBuildResult>> BuildAsync(PreviewBuildRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<PreviewBuildResult>(Errors.Conflict("preview unavailable")));
    }

    private sealed class FixedPreviewBuilder : IPreviewBuilder
    {
        public Task<Result<PreviewBuildResult>> BuildAsync(PreviewBuildRequest request, CancellationToken cancellationToken)
        {
            var image = CreateCoordinateImage(4, 4);
            return Task.FromResult(
                Result.Success(
                    new PreviewBuildResult(
                        BaseImage: image,
                        LandmarkImage: null,
                        OverlayImage: null,
                        CompositeImage: image,
                        EditableBackingOrigins: BuildBackingOrigins(4, 4))));
        }
    }

    private sealed class ConfigApplyingPreviewBuilder : IPreviewBuilder
    {
        public Task<Result<PreviewBuildResult>> BuildAsync(PreviewBuildRequest request, CancellationToken cancellationToken)
        {
            var image = CreateCoordinateImage(4, 4);
            var composite = ApplyConfigToImage(image, request.Config, request.Direction);
            return Task.FromResult(
                Result.Success(
                    new PreviewBuildResult(
                        BaseImage: image,
                        LandmarkImage: null,
                        OverlayImage: null,
                        CompositeImage: composite,
                        EditableBackingOrigins: BuildBackingOrigins(4, 4))));
        }
    }

    private sealed class RecordingPreviewBuilder : IPreviewBuilder
    {
        public int CallCount { get; private set; }

        public Task<Result<PreviewBuildResult>> BuildAsync(PreviewBuildRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(
                Result.Success(
                    new PreviewBuildResult(
                        BaseImage: CreateImage(),
                        LandmarkImage: null,
                        OverlayImage: null,
                        CompositeImage: CreateImage(),
                        EditableBackingOrigins: BuildBackingOrigins(4, 4))));
        }

        private static SpriteImage CreateImage() => new(4, 4, new byte[4 * 4 * 4]);
    }

    private static Dictionary<PixelCoordinate, PixelCoordinate?> BuildBackingOrigins(int width, int height)
    {
        var result = new Dictionary<PixelCoordinate, PixelCoordinate?>(width * height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var coordinate = new PixelCoordinate(x, y);
                result[coordinate] = coordinate;
            }
        }

        return result;
    }

    private sealed class NullStateFrameReader : IStateFrameReader
    {
        public Task<Result<SpriteImage>> ReadFrameAsync(string dmiPath, string stateName, SpriteDirection direction, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<SpriteImage>(Errors.NotFound(stateName)));
    }

    private sealed class SolidStateFrameReader : IStateFrameReader
    {
        public Task<Result<SpriteImage>> ReadFrameAsync(string dmiPath, string stateName, SpriteDirection direction, CancellationToken cancellationToken)
        {
            var color = ColorForState(stateName);
            var pixels = new byte[4 * 4 * 4];
            for (var index = 0; index < pixels.Length; index += 4)
            {
                pixels[index] = color.R;
                pixels[index + 1] = color.G;
                pixels[index + 2] = color.B;
                pixels[index + 3] = color.A;
            }

            return Task.FromResult(Result.Success(new SpriteImage(4, 4, pixels)));
        }

        public static System.Windows.Media.Color ColorForState(string stateName) =>
            stateName.ToLowerInvariant() switch
            {
                "a" => System.Windows.Media.Color.FromArgb(255, 220, 24, 36),
                "b" => System.Windows.Media.Color.FromArgb(255, 30, 144, 255),
                "c" => System.Windows.Media.Color.FromArgb(255, 32, 180, 80),
                _ => System.Windows.Media.Color.FromArgb(255, 180, 180, 180)
            };
    }

    private sealed class CoordinateStateFrameReader : IStateFrameReader
    {
        public Task<Result<SpriteImage>> ReadFrameAsync(string dmiPath, string stateName, SpriteDirection direction, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(CreateCoordinateImage(4, 4)));
    }

    private sealed class SparseStateFrameReader : IStateFrameReader
    {
        public Task<Result<SpriteImage>> ReadFrameAsync(string dmiPath, string stateName, SpriteDirection direction, CancellationToken cancellationToken)
        {
            var color = SolidStateFrameReader.ColorForState(stateName);
            var pixels = new byte[4 * 4 * 4];
            pixels[0] = color.R;
            pixels[1] = color.G;
            pixels[2] = color.B;
            pixels[3] = color.A;

            return Task.FromResult(Result.Success(new SpriteImage(4, 4, pixels)));
        }
    }

    private sealed class NullBatchProcessingService : IBatchProcessingService
    {
        public Task<Result<BatchJobResult>> RunAsync(BatchJobRequest request, IProgress<BatchProgress>? progress, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(new BatchJobResult([])));
    }

    private sealed class RecordingBatchProcessingService : IBatchProcessingService
    {
        public int CallCount { get; private set; }

        public BatchJobRequest? Request { get; private set; }

        public Task<Result<BatchJobResult>> RunAsync(BatchJobRequest request, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
        {
            CallCount++;
            Request = request;
            progress?.Report(new BatchProgress(1, 1, "sprite.dmi"));
            return Task.FromResult(
                Result.Success(
                    new BatchJobResult(
                        [
                            new BatchFileResult(
                                "input/sprite.dmi",
                                "output/sprite.dmi",
                                BatchFileStatus.Processed,
                                "Processed")
                        ])));
        }
    }

    private sealed class SourceResolutionDocumentRepository(
        SpriteDocument document,
        Guid changedSourceId,
        Guid missingSourceId) : ISpriteDocumentRepository
    {
        public List<SpriteDocumentLoadRequest> Requests { get; } = [];

        public Task<Result<SpriteDocument>> LoadAsync(
            SpriteDocumentLoadRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            var result = Requests.Count switch
            {
                1 => Result.Failure<SpriteDocument>(SpriteDocumentSourceErrors.Changed(changedSourceId, "changed.png")),
                2 => Result.Failure<SpriteDocument>(SpriteDocumentSourceErrors.Missing(missingSourceId, "missing.png")),
                _ => Result.Success(document)
            };
            return Task.FromResult(result);
        }

        public Task<Result> SaveAsync(
            string projectPath,
            SpriteDocument savedDocument,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class UnusedSpriteDocumentServices :
        IAssetProbeService,
        ISpriteDocumentImporter,
        ISpriteFrameSource,
        ISpriteDocumentExporter
    {
        public Task<Result<AssetProbe>> ProbeAsync(
            string path,
            AssetImportLimits limits,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<AssetProbe>(Errors.Unexpected("Probe should not be called.")));

        public Task<Result<SpriteDocument>> ImportAsync(
            SpriteDocumentImportRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<SpriteDocument>(Errors.Unexpected("Import should not be called.")));

        public Task<Result<SpriteImage>> ReadAsync(
            SpriteFrameReadRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<SpriteImage>(Errors.Unexpected("Frame read should not be called.")));

        public Task<Result<SpriteDocumentExportResult>> ExportAsync(
            SpriteDocumentExportRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<SpriteDocumentExportResult>(Errors.Unexpected("Export should not be called.")));
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? DmiPath { get; init; }

        public string? ConfigPath { get; init; }

        public string? LegacyCsvPath { get; init; }

        public string? BatchDirectory { get; init; }

        public string? SpriteDocumentPath { get; init; }

        public SpriteSourceChangeChoice SourceChangeChoice { get; init; } = SpriteSourceChangeChoice.Cancel;

        public string? RelinkSourcePath { get; init; }

        public int SourceChangePromptCount { get; private set; }

        public int RelinkPromptCount { get; private set; }

        public string? OpenDmiFile(string? initialPath) => DmiPath ?? initialPath;

        public string? OpenConfigFile(string? initialPath) => ConfigPath ?? initialPath;

        public string? SaveConfigFile(string? initialPath, string? configName) => initialPath;

        public string? OpenLegacyCsvFile(string? initialPath) => LegacyCsvPath ?? initialPath;

        public string? SelectDirectory(string description, string? initialPath) => BatchDirectory ?? initialPath;

        public string? OpenSpriteDocumentFile(string? initialPath) => SpriteDocumentPath ?? initialPath;

        public SpriteSourceChangeChoice ResolveSpriteSourceChange(SpriteDocumentSourceIssue issue)
        {
            SourceChangePromptCount++;
            return SourceChangeChoice;
        }

        public string? RelinkSpriteSource(SpriteDocumentSourceIssue issue, string? initialPath)
        {
            RelinkPromptCount++;
            return RelinkSourcePath;
        }
    }

    private static SpriteImage CreateCoordinateImage(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = ((y * width) + x) * 4;
                pixels[index] = (byte)(x * 40 + 10);
                pixels[index + 1] = (byte)(y * 50 + 20);
                pixels[index + 2] = (byte)(x * 15 + y * 10 + 30);
                pixels[index + 3] = 255;
            }
        }

        return new SpriteImage(width, height, pixels);
    }

    private static SpriteImage ApplyConfigToImage(SpriteImage image, SpriteConfig config, SpriteDirection direction)
    {
        var pixels = image.RgbaBytes[..];
        foreach (var mapping in config.GetMappings(direction))
        {
            var destinationIndex = ((mapping.Source.Y * image.Width) + mapping.Source.X) * 4;
            if (mapping.Target is not { } target)
            {
                pixels[destinationIndex] = 0;
                pixels[destinationIndex + 1] = 0;
                pixels[destinationIndex + 2] = 0;
                pixels[destinationIndex + 3] = 0;
                continue;
            }

            var sourceIndex = ((target.Y * image.Width) + target.X) * 4;
            pixels[destinationIndex] = image.RgbaBytes[sourceIndex];
            pixels[destinationIndex + 1] = image.RgbaBytes[sourceIndex + 1];
            pixels[destinationIndex + 2] = image.RgbaBytes[sourceIndex + 2];
            pixels[destinationIndex + 3] = image.RgbaBytes[sourceIndex + 3];
        }

        return new SpriteImage(image.Width, image.Height, pixels);
    }

    private static System.Windows.Media.Color GetSurfaceColor(EditorSurfaceRenderState surface, int x, int y)
    {
        var index = surface.GetIndex(x, y) * 4;
        return System.Windows.Media.Color.FromArgb(
            surface.RgbaBytes[index + 3],
            surface.RgbaBytes[index + 2],
            surface.RgbaBytes[index + 1],
            surface.RgbaBytes[index]);
    }

    private static System.Windows.Media.Color NeutralSurfaceColor() =>
        System.Windows.Media.Color.FromRgb(244, 239, 231);
}
