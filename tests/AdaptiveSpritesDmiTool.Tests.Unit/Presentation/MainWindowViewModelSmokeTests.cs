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
    public async Task InitializeAsyncShouldStartWithEmptyWorkspaceWithoutDemoAssets()
    {
        var settingsRepository = new InMemorySettingsRepository(WorkspaceSettings.Empty);
        var viewModel = CreateViewModel(settingsRepository);

        await viewModel.InitializeAsync();

        viewModel.WorkspaceTitle.Should().Be("Empty workspace");
        viewModel.ConfigSummary.Should().Be("No config loaded");
        viewModel.StatusMessage.Should().Contain("Empty workspace");
        viewModel.SpriteContractSummary.Should().Contain("No sprite loaded");
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

    private static MainWindowViewModel CreateViewModel(InMemorySettingsRepository settingsRepository)
    {
        var session = new EditorSession();
        var workspace = new EditorWorkspaceService();

        return new MainWindowViewModel(
            new StartEmptyWorkspaceUseCase(workspace, session),
            new CreateConfigUseCase(session),
            new SaveConfigUseCase(new NullConfigRepository(), session),
            new LoadConfigUseCase(new NullConfigRepository(), session),
            new ImportLegacyCsvConfigUseCase(new NullLegacyCsvImporter(), session),
            new LoadDmiFileUseCase(new NullDmiReader(), session, workspace),
            new BuildPreviewUseCase(new NullPreviewBuilder(), session),
            new ApplyConfigToDmiBatchUseCase(new NullBatchProcessingService(), session),
            new UndoChangeUseCase(session),
            new RedoChangeUseCase(session),
            new SetPreviewSelectionUseCase(session),
            new SetSelectedDirectionUseCase(session),
            new ApplyConfigTransformUseCase(session),
            new LoadWorkspaceSettingsUseCase(settingsRepository),
            new SaveWorkspaceSettingsUseCase(settingsRepository),
            new SpriteImageBitmapSourceFactory(),
            new StubFileDialogService(),
            session,
            NullLogger<MainWindowViewModel>.Instance);
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

    private sealed class NullLegacyCsvImporter : ILegacyCsvConfigImporter
    {
        public Task<Result<SpriteConfig>> ImportAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<SpriteConfig>(Errors.NotFound(path)));
    }

    private sealed class NullDmiReader : IDmiReader
    {
        public Task<Result<DmiAssetInfo>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<DmiAssetInfo>(Errors.NotFound(path)));
    }

    private sealed class NullPreviewBuilder : IPreviewBuilder
    {
        public Task<Result<PreviewBuildResult>> BuildAsync(PreviewBuildRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<PreviewBuildResult>(Errors.Conflict("preview unavailable")));
    }

    private sealed class NullBatchProcessingService : IBatchProcessingService
    {
        public Task<Result<BatchJobResult>> RunAsync(BatchJobRequest request, IProgress<BatchProgress>? progress, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(new BatchJobResult([])));
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? OpenDmiFile(string? initialPath) => initialPath;

        public string? OpenConfigFile(string? initialPath) => initialPath;

        public string? SaveConfigFile(string? initialPath, string? configName) => initialPath;

        public string? OpenLegacyCsvFile(string? initialPath) => initialPath;

        public string? SelectDirectory(string description, string? initialPath) => initialPath;
    }
}
