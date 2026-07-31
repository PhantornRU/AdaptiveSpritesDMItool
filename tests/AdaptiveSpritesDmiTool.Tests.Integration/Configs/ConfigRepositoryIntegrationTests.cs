using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.Configs;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Integration.Configs;

public sealed class ConfigRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.Tests", Guid.NewGuid().ToString("N"));

    public ConfigRepositoryIntegrationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task JsonRepositoryShouldRoundTripConfig()
    {
        var repository = new JsonSpriteConfigRepository();
        var configPath = Path.Combine(_tempDirectory, "config.json");
        var metadata = ConfigMetadata.CreateNew(ConfigSource.UserCreated, "integration", utcNow: DateTimeOffset.UtcNow);
        var config = SpriteConfig.CreateEmpty(
                "demo",
                new SpriteResolution(32, 32),
                SupportedDirectionSet.Eight,
                metadata,
                new SpriteEditorSettings(-3))
            .SetMapping(SpriteDirection.South, new PixelCoordinate(1, 2), new PixelCoordinate(4, 5), metadata.UpdatedUtc)
            .SetMapping(SpriteDirection.NorthEast, new PixelCoordinate(3, 3), null, metadata.UpdatedUtc)
            .SetMappingForced(SpriteDirection.West, new PixelCoordinate(7, 7), new PixelCoordinate(7, 7), metadata.UpdatedUtc);

        var saveResult = await repository.SaveAsync(configPath, config, CancellationToken.None);
        var loadResult = await repository.LoadAsync(configPath, CancellationToken.None);

        saveResult.IsSuccess.Should().BeTrue();
        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value.Name.Should().Be("demo");
        loadResult.Value.SupportedDirections.Should().Be(SupportedDirectionSet.Eight);
        loadResult.Value.EditorSettings.MirrorAxisOffsetPixels.Should().Be(-3);
        loadResult.Value.GetEffectiveTarget(SpriteDirection.South, new PixelCoordinate(1, 2)).Should().Be(new PixelCoordinate(4, 5));
        loadResult.Value.IsTransparent(SpriteDirection.NorthEast, new PixelCoordinate(3, 3)).Should().BeTrue();
        loadResult.Value.GetMappings(SpriteDirection.West).Should().ContainSingle(
            mapping => mapping.Source == new PixelCoordinate(7, 7) && mapping.Target == new PixelCoordinate(7, 7));
        (await File.ReadAllTextAsync(configPath)).Should().Contain("\"Version\": 2").And
            .Contain("\"mirrorAxisOffsetPixels\": -3");
    }

    [Fact]
    public async Task JsonRepositoryShouldMigrateVersionOneConfigWithCenteredMirrorAxis()
    {
        var path = Path.Combine(_tempDirectory, "version-one.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 1,
              "name": "legacy",
              "resolution": { "width": 4, "height": 3 },
              "supportedDirections": "four",
              "metadata": {
                "createdUtc": "2026-01-01T00:00:00+00:00",
                "updatedUtc": "2026-01-01T00:00:00+00:00",
                "source": "Json"
              },
              "mappings": {}
            }
            """);

        var result = await new JsonSpriteConfigRepository().LoadAsync(path, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EditorSettings.MirrorAxisOffsetPixels.Should().Be(0);
    }

    [Fact]
    public async Task JsonRepositoryShouldRejectMirrorAxisOffsetOutsideResolution()
    {
        var path = Path.Combine(_tempDirectory, "invalid-axis.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 2,
              "name": "invalid",
              "resolution": { "width": 4, "height": 3 },
              "supportedDirections": "four",
              "metadata": {
                "createdUtc": "2026-01-01T00:00:00+00:00",
                "updatedUtc": "2026-01-01T00:00:00+00:00",
                "source": "Json"
              },
              "editorSettings": { "mirrorAxisOffsetPixels": 2 },
              "mappings": {}
            }
            """);

        var result = await new JsonSpriteConfigRepository().LoadAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Mirror axis offset");
    }

    [Fact]
    public async Task LegacyImporterShouldImportCsvAndInferTransparency()
    {
        var csvPath = Path.Combine(_tempDirectory, "legacy.csv");
        await File.WriteAllLinesAsync(
            csvPath,
            [
                "South,0,0,1,1",
                "North,2,2,2,2",
                "East,3,3,-1,-1",
                "West,1,1,0,0"
            ]);

        var importer = new LegacyCsvConfigImporter();

        var result = await importer.ImportAsync(csvPath, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("legacy");
        result.Value.SupportedDirections.Should().Be(SupportedDirectionSet.Four);
        result.Value.GetEffectiveTarget(SpriteDirection.South, new PixelCoordinate(0, 0)).Should().Be(new PixelCoordinate(1, 1));
        result.Value.IsTransparent(SpriteDirection.East, new PixelCoordinate(3, 3)).Should().BeTrue();
        result.Value.Metadata.Source.Should().Be(ConfigSource.ImportedLegacyCsv);
        result.Value.EditorSettings.MirrorAxisOffsetPixels.Should().Be(0);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
