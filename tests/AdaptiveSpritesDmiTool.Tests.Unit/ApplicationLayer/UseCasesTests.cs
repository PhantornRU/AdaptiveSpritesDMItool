using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Application;

public sealed class UseCasesTests
{
    [Fact]
    public async Task SaveConfigUseCaseShouldDelegateToRepository()
    {
        var session = new EditorSession();
        session.LoadAsset(new DmiAssetInfo("asset", null, new SpriteResolution(32, 32), SupportedDirectionSet.Four, []));
        session.CreateConfig("config", ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test"));

        var repository = new InMemoryConfigRepository();
        var useCase = new SaveConfigUseCase(repository, session);

        var result = await useCase.ExecuteAsync("config.json", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Stored.Should().ContainKey("config.json");
    }

    [Fact]
    public async Task LoadDmiFileUseCaseShouldUpdateSessionAndWorkspace()
    {
        var asset = new DmiAssetInfo("asset", "sample.dmi", new SpriteResolution(32, 32), SupportedDirectionSet.Four, []);
        var reader = new StubDmiReader(asset);
        var session = new EditorSession();
        var workspace = new EditorWorkspaceService();
        var useCase = new LoadDmiFileUseCase(reader, session, workspace);

        var result = await useCase.ExecuteAsync("sample.dmi", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.Workspace.LoadedDocumentPath.Should().Be("sample.dmi");
        workspace.Current.LoadedDocumentPath.Should().Be("sample.dmi");
    }

    [Fact]
    public async Task LoadDmiFileUseCaseShouldRejectAssetIncompatibleWithActiveConfig()
    {
        var existingConfig = SpriteConfig.CreateEmpty(
            "config",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test"));
        var asset = new DmiAssetInfo("asset", "sample.dmi", new SpriteResolution(64, 32), SupportedDirectionSet.Four, []);
        var reader = new StubDmiReader(asset);
        var session = new EditorSession();
        session.SetCurrentConfig(existingConfig).IsSuccess.Should().BeTrue();
        var workspace = new EditorWorkspaceService();
        var useCase = new LoadDmiFileUseCase(reader, session, workspace);

        var result = await useCase.ExecuteAsync("sample.dmi", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Config resolution");
        session.LoadedAsset.Should().BeNull();
        session.CurrentConfig.Should().BeSameAs(existingConfig);
        workspace.Current.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void SetPreviewSelectionUseCaseShouldNormalizeOptionalValues()
    {
        var session = new EditorSession();
        var useCase = new SetPreviewSelectionUseCase(session);

        var result = useCase.Execute(" base ", " landmark ", "   ");

        result.IsSuccess.Should().BeTrue();
        session.PreviewSelection.Should().Be(new PreviewSelection("base", "landmark", null));
    }

    [Fact]
    public async Task LoadWorkspaceSettingsUseCaseShouldFallbackToEmptySettingsWhenRepositoryHasNoData()
    {
        var repository = new StubSettingsRepository(
            Result.Failure<WorkspaceSettings>(Errors.NotFound("settings.json")));
        var useCase = new LoadWorkspaceSettingsUseCase(repository);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(WorkspaceSettings.Empty);
    }

    [Fact]
    public async Task SaveWorkspaceSettingsUseCaseShouldDelegateToRepository()
    {
        var repository = new StubSettingsRepository(Result.Success(WorkspaceSettings.Empty));
        var useCase = new SaveWorkspaceSettingsUseCase(repository);
        var settings = new WorkspaceSettings(
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
            "Focused",
            "Mappings",
            false,
            true);

        var result = await useCase.ExecuteAsync(settings, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.SavedSettings.Should().Be(settings);
    }

    private sealed class InMemoryConfigRepository : IConfigRepository
    {
        public Dictionary<string, SpriteConfig> Stored { get; } = [];

        public Task<Result<SpriteConfig>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Stored.TryGetValue(path, out var config)
                ? Task.FromResult(Result.Success(config))
                : Task.FromResult(Result.Failure<SpriteConfig>(Errors.NotFound(path)));

        public Task<Result> SaveAsync(string path, SpriteConfig config, CancellationToken cancellationToken)
        {
            Stored[path] = config;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class StubDmiReader(DmiAssetInfo asset) : IDmiReader
    {
        public Task<Result<DmiAssetInfo>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(asset));
    }

    private sealed class StubSettingsRepository(Result<WorkspaceSettings> loadResult) : ISettingsRepository
    {
        public WorkspaceSettings? SavedSettings { get; private set; }

        public Task<Result<WorkspaceSettings>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(loadResult);

        public Task<Result> SaveAsync(WorkspaceSettings settings, CancellationToken cancellationToken)
        {
            SavedSettings = settings;
            return Task.FromResult(Result.Success());
        }
    }
}
