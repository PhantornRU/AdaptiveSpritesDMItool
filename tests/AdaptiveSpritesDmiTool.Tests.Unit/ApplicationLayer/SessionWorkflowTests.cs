using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Application;

public sealed class SessionWorkflowTests
{
    [Fact]
    public void StartEmptyWorkspaceUseCaseShouldResetSessionAndWorkspace()
    {
        var session = new EditorSession();
        var workspace = new EditorWorkspaceService();
        var asset = new DmiAssetInfo("asset", "sample.dmi", new SpriteResolution(32, 32), SupportedDirectionSet.Eight, []);

        session.LoadAsset(asset);
        session.CreateConfig("config", ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test"));
        session.SetPreviewSelection(new PreviewSelection("base", "landmark", "overlay"));
        session.SetSelectedDirection(SpriteDirection.NorthEast);
        session.UpsertMapping(SpriteDirection.South, new PixelCoordinate(1, 1), new PixelCoordinate(2, 2));

        var useCase = new StartEmptyWorkspaceUseCase(workspace, session);

        var result = useCase.Execute();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsEmpty.Should().BeTrue();
        workspace.Current.IsEmpty.Should().BeTrue();
        session.Workspace.IsEmpty.Should().BeTrue();
        session.LoadedAsset.Should().BeNull();
        session.CurrentConfig.Should().BeNull();
        session.CurrentConfigPath.Should().BeNull();
        session.PreviewSelection.Should().Be(new PreviewSelection(string.Empty, null, null));
        session.SelectedDirection.Should().Be(SpriteDirection.South);
        session.CanUndo.Should().BeFalse();
        session.CanRedo.Should().BeFalse();
    }

    [Fact]
    public async Task LoadConfigUseCaseShouldRejectConfigWithIncompatibleResolution()
    {
        var session = CreateLoadedSession(new SpriteResolution(32, 32), SupportedDirectionSet.Four);
        var repository = new InMemoryConfigRepository(
            SpriteConfig.CreateEmpty(
                "config",
                new SpriteResolution(64, 32),
                SupportedDirectionSet.Four,
                ConfigMetadata.CreateNew(ConfigSource.Json, "config.json")));

        var useCase = new LoadConfigUseCase(repository, session);

        var result = await useCase.ExecuteAsync("config.json", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Config resolution");
    }

    [Fact]
    public async Task ImportLegacyCsvConfigUseCaseShouldRejectConfigWithIncompatibleDirectionSet()
    {
        var session = CreateLoadedSession(new SpriteResolution(32, 32), SupportedDirectionSet.Four);
        var importer = new StubLegacyImporter(
            SpriteConfig.CreateEmpty(
                "config",
                new SpriteResolution(32, 32),
                SupportedDirectionSet.Eight,
                ConfigMetadata.CreateNew(ConfigSource.ImportedLegacyCsv, "legacy.csv", importedFromLegacy: "legacy.csv")));

        var useCase = new ImportLegacyCsvConfigUseCase(importer, session);

        var result = await useCase.ExecuteAsync("legacy.csv", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("direction set");
    }

    [Fact]
    public async Task ApplyConfigToDmiBatchUseCaseShouldPropagateOverwritePolicyToBatchService()
    {
        var session = CreateLoadedSession(new SpriteResolution(32, 32), SupportedDirectionSet.Four);
        session.CreateConfig("config", ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test"));

        var batchService = new RecordingBatchProcessingService();
        var useCase = new ApplyConfigToDmiBatchUseCase(batchService, session);
        BatchProgress? lastProgress = null;
        var progress = new Progress<BatchProgress>(value => lastProgress = value);

        var result = await useCase.ExecuteAsync(
            "input",
            "output",
            OverwritePolicy.FailIfExists,
            progress,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        batchService.Request.Should().NotBeNull();
        batchService.Request!.OverwritePolicy.Should().Be(OverwritePolicy.FailIfExists);
        batchService.Request.InputDirectory.Should().Be("input");
        batchService.Request.OutputDirectory.Should().Be("output");
        lastProgress.Should().Be(new BatchProgress(1, 1, "file1.dmi"));
    }

    [Fact]
    public async Task ApplyConfigToDmiBatchUseCaseShouldRequireActiveConfig()
    {
        var batchService = new RecordingBatchProcessingService();
        var useCase = new ApplyConfigToDmiBatchUseCase(batchService, new EditorSession());

        var result = await useCase.ExecuteAsync(
            "input",
            "output",
            OverwritePolicy.OverwriteExisting,
            progress: null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        batchService.Request.Should().BeNull();
    }

    private static EditorSession CreateLoadedSession(SpriteResolution resolution, SupportedDirectionSet directions)
    {
        var session = new EditorSession();
        session.LoadAsset(new DmiAssetInfo("asset", "sample.dmi", resolution, directions, []));
        return session;
    }

    private sealed class InMemoryConfigRepository(SpriteConfig config) : IConfigRepository
    {
        public Task<Result<SpriteConfig>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(config));

        public Task<Result> SaveAsync(string path, SpriteConfig config, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class StubLegacyImporter(SpriteConfig config) : ILegacyCsvConfigImporter
    {
        public Task<Result<SpriteConfig>> ImportAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(config));
    }

    private sealed class RecordingBatchProcessingService : IBatchProcessingService
    {
        public BatchJobRequest? Request { get; private set; }

        public Task<Result<BatchJobResult>> RunAsync(
            BatchJobRequest request,
            IProgress<BatchProgress>? progress,
            CancellationToken cancellationToken)
        {
            Request = request;
            progress?.Report(new BatchProgress(1, 1, "file1.dmi"));
            return Task.FromResult(
                Result.Success(
                    new BatchJobResult(
                    [
                        new BatchFileResult("file1.dmi", "output\\file1.dmi", BatchFileStatus.Processed, "ok")
                    ])));
        }
    }
}