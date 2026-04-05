using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.Settings;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Integration.Settings;

public sealed class JsonWorkspaceSettingsRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.SettingsTests", Guid.NewGuid().ToString("N"));

    public JsonWorkspaceSettingsRepositoryIntegrationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task RepositoryShouldRoundTripAllWorkspaceSettingsFields()
    {
        var path = Path.Combine(_tempDirectory, "workspace.json");
        var repository = new JsonWorkspaceSettingsRepository(path);
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
            SpriteDirection.NorthEast,
            OverwritePolicy.FailIfExists);

        (await repository.SaveAsync(settings, CancellationToken.None)).IsSuccess.Should().BeTrue();

        var loadResult = await repository.LoadAsync(CancellationToken.None);

        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value.Should().Be(settings);
    }

    [Fact]
    public async Task RepositoryShouldRejectUnsupportedVersion()
    {
        var path = Path.Combine(_tempDirectory, "invalid-version.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 2,
              "lastOpenedDmiPath": "sprite.dmi",
              "lastOpenedConfigPath": "config.json",
              "lastImportedLegacyCsvPath": "legacy.csv",
              "lastInputDirectory": "input",
              "lastOutputDirectory": "output",
              "lastDraftConfigName": "draft",
              "lastBaseState": "base",
              "lastLandmarkState": "landmark",
              "lastOverlayState": "overlay",
              "lastSelectedDirection": "North",
              "lastOverwritePolicy": "SkipExisting"
            }
            """);

        var repository = new JsonWorkspaceSettingsRepository(path);

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Unsupported workspace settings version");
    }

    [Fact]
    public async Task RepositoryShouldRejectMissingSettingsFile()
    {
        var repository = new JsonWorkspaceSettingsRepository(Path.Combine(_tempDirectory, "missing.json"));

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not-found");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
