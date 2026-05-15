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
        var config = SpriteConfig.CreateEmpty("demo", new SpriteResolution(32, 32), SupportedDirectionSet.Eight, metadata)
            .SetMapping(SpriteDirection.South, new PixelCoordinate(1, 2), new PixelCoordinate(4, 5), metadata.UpdatedUtc)
            .SetMapping(SpriteDirection.NorthEast, new PixelCoordinate(3, 3), null, metadata.UpdatedUtc);

        var saveResult = await repository.SaveAsync(configPath, config, CancellationToken.None);
        var loadResult = await repository.LoadAsync(configPath, CancellationToken.None);

        saveResult.IsSuccess.Should().BeTrue();
        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value.Name.Should().Be("demo");
        loadResult.Value.SupportedDirections.Should().Be(SupportedDirectionSet.Eight);
        loadResult.Value.GetEffectiveTarget(SpriteDirection.South, new PixelCoordinate(1, 2)).Should().Be(new PixelCoordinate(4, 5));
        loadResult.Value.IsTransparent(SpriteDirection.NorthEast, new PixelCoordinate(3, 3)).Should().BeTrue();
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
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
