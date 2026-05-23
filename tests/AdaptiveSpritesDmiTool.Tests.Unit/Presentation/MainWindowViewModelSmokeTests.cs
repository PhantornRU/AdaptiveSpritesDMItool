using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Presentation.Wpf;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
        viewModel.NavigationRail.Items.Should().HaveCount(4);
        viewModel.EditorWorkspace.IsAvailable.Should().BeFalse();
        viewModel.BatchWorkspace.IsAvailable.Should().BeFalse();
        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();
        viewModel.StartTab.WelcomeTitle.Should().Contain("Open or import");
        viewModel.PreviewPanel.IsAutoPreviewEnabled.Should().BeTrue();
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

        viewModel.ShowAllDirectionDisplayPicker.Should().BeTrue();
        viewModel.DirectionDisplaySelectorItems.Select(item => item.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.East, SpriteDirection.North, SpriteDirection.West);
        viewModel.EditorSurfaceGridRows.Should().Be(2);
        viewModel.EditorSurfaceGridColumns.Should().Be(2);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.East, SpriteDirection.North, SpriteDirection.West);

        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.East);

        viewModel.EditorSurfaceGridRows.Should().Be(1);
        viewModel.EditorSurfaceGridColumns.Should().Be(1);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should().Equal(SpriteDirection.East);
        viewModel.DirectionNavigatorItems.Single(item => item.Direction == SpriteDirection.East).IsDisplaySelected.Should().BeTrue();

        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.West);

        viewModel.EditorSurfaceGridRows.Should().Be(2);
        viewModel.EditorSurfaceGridColumns.Should().Be(1);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.East, SpriteDirection.West);

        viewModel.ToggleDisplayedDirectionCommand.Execute(SpriteDirection.South);

        viewModel.EditorSurfaceGridRows.Should().Be(2);
        viewModel.EditorSurfaceGridColumns.Should().Be(2);
        viewModel.SourceViewportSurfaces.Select(surface => surface.Direction).Should()
            .Equal(SpriteDirection.South, SpriteDirection.East, SpriteDirection.North, SpriteDirection.West);

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

        viewModel.SelectedEditorTool = EditorTool.UndoArea;

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
    public async Task ParallelScopeShouldMirrorEditableCoordinatesUsingLegacyGeometry()
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
        viewModel.UseCentralizedPropagation = true;
        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;

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
            (new PixelCoordinate(2, 1), new PixelCoordinate(2, 2)));
        AssertDirectionMappings(viewModel, SpriteDirection.East);
        AssertDirectionMappings(viewModel, SpriteDirection.West);
    }

    [Fact]
    public async Task AllScopeShouldMatchLegacySouthPropagation()
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
        viewModel.UseCentralizedPropagation = true;
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
            (new PixelCoordinate(2, 1), new PixelCoordinate(2, 2)));
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.East,
            (new PixelCoordinate(0, 1), new PixelCoordinate(2, 2)));
        AssertDirectionMappings(
            viewModel,
            SpriteDirection.West,
            (new PixelCoordinate(2, 1), new PixelCoordinate(2, 2)));
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
        viewModel.UseCentralizedPropagation = true;
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
            (new PixelCoordinate(2, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.SouthWest);
        AssertDirectionMappings(viewModel, SpriteDirection.NorthEast);
        AssertDirectionMappings(viewModel, SpriteDirection.South);
        AssertDirectionMappings(viewModel, SpriteDirection.North);
        AssertDirectionMappings(viewModel, SpriteDirection.East);
        AssertDirectionMappings(viewModel, SpriteDirection.West);
    }

    [Fact]
    public async Task EightDirectionAllScopeShouldPreserveCardinalCoordinatesAndMirrorDiagonalFamily()
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
        viewModel.UseCentralizedPropagation = true;
        viewModel.SelectedDirection = SpriteDirection.SouthEast;
        viewModel.SelectedDirectionScope = DirectionScope.All;

        viewModel.HandleSourceCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 3, 1));
        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 2));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 2));

        AssertDirectionMappings(viewModel, SpriteDirection.SouthEast, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.NorthWest, (new PixelCoordinate(2, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.SouthWest, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.NorthEast, (new PixelCoordinate(2, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.South, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.North, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.East, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
        AssertDirectionMappings(viewModel, SpriteDirection.West, (new PixelCoordinate(0, 2), new PixelCoordinate(3, 1)));
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
        viewModel.UseCentralizedPropagation = true;

        ApplySingleMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 1), new PixelCoordinate(0, 0));
        ApplySingleMapping(viewModel, SpriteDirection.North, new PixelCoordinate(3, 2), new PixelCoordinate(2, 0));

        viewModel.SelectedDirection = SpriteDirection.South;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;
        viewModel.SelectedEditorTool = EditorTool.Move;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.South, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.South, 1, 0));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.South, 1, 0));

        AssertDirectionHasMapping(viewModel, SpriteDirection.South, new PixelCoordinate(1, 0), new PixelCoordinate(0, 1));
        AssertDirectionHasMapping(viewModel, SpriteDirection.North, new PixelCoordinate(1, 0), new PixelCoordinate(3, 2));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.South, new PixelCoordinate(0, 0));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.North, new PixelCoordinate(2, 0));
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
        viewModel.UseCentralizedPropagation = true;

        ApplySingleMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(0, 1), new PixelCoordinate(0, 0));
        ApplySingleMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(3, 2), new PixelCoordinate(2, 0));

        viewModel.SelectedDirection = SpriteDirection.SouthEast;
        viewModel.SelectedDirectionScope = DirectionScope.Parallel;
        viewModel.SelectedEditorTool = EditorTool.Move;

        viewModel.HandleTargetCellPointerDown(new PixelCellViewModel(SpriteDirection.SouthEast, 0, 0));
        viewModel.HandleTargetCellPointerEnter(new PixelCellViewModel(SpriteDirection.SouthEast, 1, 0));
        viewModel.HandleTargetCellPointerUp(new PixelCellViewModel(SpriteDirection.SouthEast, 1, 0));

        AssertDirectionHasMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(1, 0), new PixelCoordinate(0, 1));
        AssertDirectionHasMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(1, 0), new PixelCoordinate(3, 2));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(0, 0));
        AssertDirectionDoesNotHaveMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(2, 0));
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
        viewModel.UseCentralizedPropagation = true;

        ApplySingleMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(0, 0), new PixelCoordinate(0, 1));
        ApplySingleMapping(viewModel, SpriteDirection.SouthEast, new PixelCoordinate(1, 0), new PixelCoordinate(0, 2));
        ApplySingleMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(3, 0), new PixelCoordinate(2, 1));
        ApplySingleMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(2, 0), new PixelCoordinate(2, 2));

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
        AssertDirectionHasMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(1, 1), new PixelCoordinate(3, 0));
        AssertDirectionHasMapping(viewModel, SpriteDirection.NorthWest, new PixelCoordinate(1, 2), new PixelCoordinate(2, 0));
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
    public async Task EditableSurfaceShouldNotApplyConfigTwiceWhenCompositePreviewExists()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var previewBuilder = new ConfigApplyingPreviewBuilder();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(SupportedDirectionSet.Four),
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

    private static MainWindowViewModel CreateViewModel(
        InMemorySettingsRepository settingsRepository,
        IConfigRepository? configRepository = null,
        ILegacyCsvConfigImporter? legacyImporter = null,
        IDmiReader? dmiReader = null,
        IPreviewBuilder? previewBuilder = null,
        IBatchProcessingService? batchProcessingService = null,
        IFileDialogService? fileDialogService = null,
        EditorSession? editorSession = null)
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
            new ReadStateFrameUseCase(new NullStateFrameReader()),
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
            NullLogger<WorkspaceShellViewModel>.Instance);
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

    private sealed class SuccessfulDmiReader(SupportedDirectionSet directions) : IDmiReader
    {
        public Task<Result<DmiAssetInfo>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(
                Result.Success(
                    new DmiAssetInfo(
                        "sprite",
                        path,
                        new SpriteResolution(4, 4),
                        directions,
                        [new DmiStateInfo("idle", 1), new DmiStateInfo("blink", 1)])));
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

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? DmiPath { get; init; }

        public string? ConfigPath { get; init; }

        public string? LegacyCsvPath { get; init; }

        public string? BatchDirectory { get; init; }

        public string? OpenDmiFile(string? initialPath) => DmiPath ?? initialPath;

        public string? OpenConfigFile(string? initialPath) => ConfigPath ?? initialPath;

        public string? SaveConfigFile(string? initialPath, string? configName) => initialPath;

        public string? OpenLegacyCsvFile(string? initialPath) => LegacyCsvPath ?? initialPath;

        public string? SelectDirectory(string description, string? initialPath) => BatchDirectory ?? initialPath;
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
}
