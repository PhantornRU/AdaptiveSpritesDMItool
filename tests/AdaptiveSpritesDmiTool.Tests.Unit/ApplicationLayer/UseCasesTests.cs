using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Application;

public sealed class UseCasesTests
{
    [Fact]
    public async Task SaveConfigUseCase_ShouldDelegateToRepository()
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
    public async Task LoadDmiFileUseCase_ShouldUpdateSessionAndWorkspace()
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
}