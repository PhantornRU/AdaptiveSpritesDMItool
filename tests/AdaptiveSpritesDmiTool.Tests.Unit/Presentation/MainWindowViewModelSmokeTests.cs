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
        viewModel.SelectedBottomWorkspaceTab.Should().Be(BottomWorkspaceTab.Assets);
        viewModel.StatusMessage.Should().Be("Ready.");
        viewModel.NavigationRail.Items.Should().HaveCount(4);
        viewModel.EditorWorkspace.IsAvailable.Should().BeFalse();
        viewModel.BatchWorkspace.IsAvailable.Should().BeFalse();
        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();
        viewModel.StartTab.WelcomeTitle.Should().Contain("Open or import");
        viewModel.PreviewPanel.IsAutoPreviewEnabled.Should().BeTrue();
        viewModel.IsBottomWorkspaceExpanded.Should().BeTrue();
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
        viewModel.ConfigSummary.Should().Contain("sprite");
        viewModel.ConfigSummary.Should().Contain("unsaved draft");
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

        await viewModel.PersistWorkspaceSettingsAsync();

        settingsRepository.Saved.Should().NotBeNull();
        settingsRepository.Saved!.LastEditorViewportMode.Should().Be(nameof(EditorViewportMode.Focused));
        settingsRepository.Saved.LastBottomWorkspaceTab.Should().Be(nameof(BottomWorkspaceTab.Mappings));
        settingsRepository.Saved.IsPreviewInspectorExpanded.Should().BeFalse();
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
                nameof(EditorViewportMode.Focused),
                nameof(BottomWorkspaceTab.Configs),
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
        viewModel.SelectedBottomWorkspaceTab.Should().Be(BottomWorkspaceTab.Configs);
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
            NullLogger<MainWindowViewModel>.Instance);
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
        viewModel.SelectedBottomWorkspaceTab.Should().Be(BottomWorkspaceTab.Assets);
        viewModel.EditorWorkspace.LeftRailSummary.Should().Contain("config");
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
}
