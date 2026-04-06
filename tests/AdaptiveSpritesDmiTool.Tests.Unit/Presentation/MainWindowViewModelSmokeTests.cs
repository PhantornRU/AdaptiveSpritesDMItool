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
    public async Task InitializeAsyncShouldStartOnStartTabWithoutDemoAssets()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.ConfigSummary.Should().Be("No config loaded");
        viewModel.StatusMessage.Should().Contain("Empty workspace");
        viewModel.SpriteContractSummary.Should().Contain("No sprite loaded");
        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Start);
        viewModel.SelectedTabIndex.Should().Be((int)ShellTabKind.Start);
        viewModel.IsStartSelected.Should().BeTrue();
        viewModel.IsEditorSelected.Should().BeFalse();
        viewModel.IsBatchSelected.Should().BeFalse();
        viewModel.StartTab.WelcomeTitle.Should().Contain("Open or import");
        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();
        viewModel.StartTab.ShowResumeEditorHint.Should().BeFalse();
        viewModel.StartTab.ResumeEditorHint.Should().Contain("Start by opening a DMI");
        viewModel.StartTab.RecentSummary.Should().Contain("Last DMI: No DMI selected yet.");
        viewModel.StartTab.RecentSummary.Should().Contain("Last JSON: No JSON config selected yet.");
        viewModel.PreviewPanel.IsAutoPreviewEnabled.Should().BeTrue();
        viewModel.PreviewPanel.AutoPreviewMode.Should().Be(AutoPreviewMode.Enabled);
        viewModel.EditorTab.IsAvailable.Should().BeFalse();
        viewModel.BatchTab.IsAvailable.Should().BeFalse();
        viewModel.EditorTab.LeftRailSummary.Should().Contain("No config yet");
        viewModel.EditorTab.MappingsHeader.Should().Be("Mappings");
        viewModel.EditorTab.HasMappings.Should().BeFalse();
        viewModel.EditorTab.SelectionSummary.Should().Contain("No source pixel selected");
        viewModel.EditorTab.HoverAndStatusSummary.Should().Contain("Hover a cell");
        viewModel.PreviewPanel.PreviewModeSummary.Should().Contain("Build a preview");
    }

    [Fact]
    public async Task TabSelectionFlagsShouldMirrorTheSelectedShellTab()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.IsEditorSelected = true;

        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Editor);
        viewModel.IsStartSelected.Should().BeFalse();
        viewModel.IsEditorSelected.Should().BeTrue();
        viewModel.IsBatchSelected.Should().BeFalse();

        viewModel.SelectedTabIndex = (int)ShellTabKind.Batch;

        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Batch);
        viewModel.IsStartSelected.Should().BeFalse();
        viewModel.IsEditorSelected.Should().BeFalse();
        viewModel.IsBatchSelected.Should().BeTrue();
    }

    [Fact]
    public async Task CreateConfigAvailabilityShouldTrackDmiLoadedWorkspaceState()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();

        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();

        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.StartTab.ShowCreateConfigAction.Should().BeTrue();
        viewModel.StartTab.ResumeEditorHint.Should().Contain("Create a config");
        viewModel.EditorTab.IsAvailable.Should().BeTrue();
        viewModel.BatchTab.IsAvailable.Should().BeFalse();

        viewModel.CreateConfigCommand.Execute(null);

        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();
        viewModel.StartTab.ResumeEditorHint.Should().Contain("Switch to Editor");
    }

    [Fact]
    public async Task OpenDmiAsyncShouldSwitchToEditorTab()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Editor);
        viewModel.SelectedTabIndex.Should().Be((int)ShellTabKind.Editor);
        viewModel.EditorTab.IsAvailable.Should().BeTrue();
        viewModel.AvailableStates.Should().Contain("idle");
        viewModel.EditorTab.LeftRailSummary.Should().Contain("No config yet");
    }

    [Fact]
    public async Task CreateConfigShouldEnableBatchWorkflowAndStayOnEditorTab()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);

        viewModel.CreateConfigCommand.Execute(null);

        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Editor);
        viewModel.BatchTab.IsAvailable.Should().BeTrue();
        viewModel.ConfigSummary.Should().Contain("sprite");
        viewModel.ConfigSummary.Should().Contain("unsaved draft");
        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();
        viewModel.EditorTab.RolesSummary.Should().Contain("Base:");
        viewModel.EditorTab.RolesSummary.Should().Contain("Landmark:");
        viewModel.EditorTab.RolesSummary.Should().Contain("Overlay:");
        viewModel.EditorTab.LeftRailSummary.Should().Contain("sprite");
        viewModel.EditorTab.PreviewSelectionSummary.Should().Contain("Direction: South");
    }

    [Fact]
    public async Task LoadConfigAsyncShouldSwitchToEditorTab()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { ConfigPath = "config.json" };
        var viewModel = CreateViewModel(
            settingsRepository,
            configRepository: new SuccessfulConfigRepository(CreateConfig("Loaded Config")),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.LoadConfigCommand.ExecuteAsync(null);

        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Editor);
        viewModel.ConfigSummary.Should().Contain("Loaded Config");
        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();
    }

    [Fact]
    public async Task ImportLegacyConfigAsyncShouldSwitchToEditorTab()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { LegacyCsvPath = "legacy.csv" };
        var viewModel = CreateViewModel(
            settingsRepository,
            legacyImporter: new SuccessfulLegacyImporter(CreateConfig("Imported Config")),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.ImportLegacyConfigCommand.ExecuteAsync(null);

        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Editor);
        viewModel.ConfigSummary.Should().Contain("Imported Config");
        viewModel.StartTab.ShowCreateConfigAction.Should().BeFalse();
    }

    [Fact]
    public async Task SelectedTabIndexShouldStayInSyncWithSelectedShellTab()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.SelectedTabIndex = (int)ShellTabKind.Editor;
        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Editor);

        viewModel.SelectedShellTab = ShellTabKind.Batch;
        viewModel.SelectedTabIndex.Should().Be((int)ShellTabKind.Batch);
    }

    [Fact]
    public async Task BatchTabShouldBecomeAvailableOnceConfigExistsAndBatchRunShouldNavigateThere()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService
        {
            DmiPath = "sprite.dmi",
            BatchDirectory = "batch"
        };
        var batchService = new RecordingBatchProcessingService();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(),
            batchProcessingService: batchService,
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.BatchTab.IsAvailable.Should().BeTrue();
        viewModel.BatchTab.BatchInputDirectory = "input";
        viewModel.BatchTab.BatchOutputDirectory = "output";

        await viewModel.RunBatchCommand.ExecuteAsync(null);

        batchService.CallCount.Should().Be(1);
        viewModel.SelectedShellTab.Should().Be(ShellTabKind.Batch);
        viewModel.BatchResults.Should().NotBeEmpty();
        viewModel.BatchTab.BatchProcessedFiles.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MappingDrawerStateShouldTrackSelectedMappingAndRemoval()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(),
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);

        viewModel.SourceRows.Should().NotBeEmpty();
        viewModel.TargetRows.Should().NotBeEmpty();

        var sourceCell = viewModel.SourceRows[0].Cells[0];
        var targetCell = viewModel.TargetRows[0].Cells[1];

        viewModel.HandleSourceCellPointerDown(sourceCell);
        viewModel.HandleTargetCellPointerUp(targetCell);

        viewModel.MappingRows.Should().HaveCount(1);
        viewModel.SelectedMapping.Should().BeNull();
        viewModel.EditorTab.HasMappings.Should().BeTrue();
        viewModel.EditorTab.MappingsHeader.Should().Be("Mappings (1)");

        viewModel.SelectedMapping = viewModel.MappingRows[0];
        viewModel.RemoveSelectedMappingCommand.Execute(null);

        viewModel.MappingRows.Should().BeEmpty();
        viewModel.EditorTab.HasMappings.Should().BeFalse();
        viewModel.EditorTab.MappingsHeader.Should().Be("Mappings");
    }

    [Fact]
    public async Task CompactEditorSummariesShouldReflectSelectionAndPreviewMode()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var previewBuilder = new RecordingPreviewBuilder();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(),
            previewBuilder: previewBuilder,
            fileDialogService: dialogService);

        await viewModel.InitializeAsync();
        await viewModel.OpenDmiCommand.ExecuteAsync(null);
        viewModel.CreateConfigCommand.Execute(null);
        viewModel.SelectedExplorerState = "idle";
        viewModel.UseSelectedStateAsBaseCommand.Execute(null);

        viewModel.EditorTab.LeftRailSummary.Should().Contain("sprite");
        viewModel.EditorTab.RolesSummary.Should().Contain("Base: idle");
        viewModel.EditorTab.SelectionSummary.Should().Contain("No source pixel selected");
        viewModel.EditorTab.HoverAndStatusSummary.Should().Contain("Select a source pixel");
        viewModel.PreviewPanel.PreviewModeSummary.Should().Contain("Build a preview");
        viewModel.PreviewPanel.IsAutoPreviewEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task PersistWorkspaceSettingsAsyncShouldStoreShellState()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);
        await viewModel.InitializeAsync();

        viewModel.DmiPath = "sprite.dmi";
        viewModel.ConfigPath = "config.json";
        viewModel.DraftConfigName = "draft";
        viewModel.BaseStateName = "base";
        viewModel.LandmarkStateName = "landmark";
        viewModel.OverlayStateName = "overlay";
        viewModel.SelectedDirection = SpriteDirection.East;
        viewModel.SelectedOverwritePolicy = OverwritePolicy.FailIfExists;

        await viewModel.PersistWorkspaceSettingsAsync();

        settingsRepository.Saved.Should().NotBeNull();
        settingsRepository.Saved!.LastDraftConfigName.Should().Be("draft");
        settingsRepository.Saved.LastBaseState.Should().Be("base");
        settingsRepository.Saved.LastSelectedDirection.Should().Be(SpriteDirection.East);
        settingsRepository.Saved.LastOverwritePolicy.Should().Be(OverwritePolicy.FailIfExists);
    }

    [Fact]
    public async Task ManualPreviewRefreshShouldHandleMissingOptionalLayers()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var dialogService = new StubFileDialogService { DmiPath = "sprite.dmi" };
        var previewBuilder = new RecordingPreviewBuilder();
        var viewModel = CreateViewModel(
            settingsRepository,
            dmiReader: new SuccessfulDmiReader(),
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

    private static SpriteConfig CreateConfig(string name) =>
        SpriteConfig.CreateEmpty(
            name,
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "tests"));

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

    private sealed class SuccessfulDmiReader : IDmiReader
    {
        public Task<Result<DmiAssetInfo>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(
                Result.Success(
                    new DmiAssetInfo(
                        "sprite",
                        path,
                        new SpriteResolution(4, 4),
                        SupportedDirectionSet.Four,
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
