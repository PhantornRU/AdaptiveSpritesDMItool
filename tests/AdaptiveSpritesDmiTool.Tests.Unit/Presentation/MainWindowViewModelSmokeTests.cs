using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Presentation.Wpf;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

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
        viewModel.IsPreviewInspectorExpanded = false;
        viewModel.IsBottomWorkspaceExpanded = false;

        await viewModel.PersistWorkspaceSettingsAsync();

        settingsRepository.Saved.Should().NotBeNull();
        settingsRepository.Saved!.LastEditorViewportMode.Should().Be(nameof(EditorViewportMode.Focused));
        settingsRepository.Saved.LastBottomWorkspaceTab.Should().Be(nameof(BottomWorkspaceTab.Mappings));
        settingsRepository.Saved.IsPreviewInspectorExpanded.Should().BeFalse();
        settingsRepository.Saved.IsBottomWorkspaceExpanded.Should().BeFalse();
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
    public async Task RestoreWorkspaceShouldForceMatrixViewportWhileFocusedPathIsDisabled()
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

        viewModel.SelectedEditorViewportMode.Should().Be(EditorViewportMode.Matrix);
        viewModel.IsBottomWorkspaceExpanded.Should().BeFalse();
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
        viewModel.SelectedAreaBounds.Should().Be(new PixelAreaBounds(2, 1, 2, 2));
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
        var sourceColor = sourceSurface.FillColors[sourceSurface.GetIndex(0, 0)];
        var originalEditableColor = sourceSurface.FillColors[sourceSurface.GetIndex(2, 2)];
        var remappedEditableColor = editableSurface.FillColors[editableSurface.GetIndex(2, 2)];

        remappedEditableColor.Should().Be(sourceColor);
        remappedEditableColor.Should().NotBe(originalEditableColor);
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
        viewModel.SelectedEditorViewportMode.Should().Be(EditorViewportMode.Matrix);
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
        IFileDialogService? fileDialogService = null)
    {
        var session = new EditorSession();
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
                        CompositeImage: image)));
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
                        CompositeImage: CreateImage())));
        }

        private static SpriteImage CreateImage() => new(4, 4, new byte[4 * 4 * 4]);
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

        public Task<Result<BatchJobResult>> RunAsync(BatchJobRequest request, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
        {
            CallCount++;
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
}
