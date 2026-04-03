using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.Configs;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Integration.Configs;

public sealed class ConfigValidationIntegrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.Tests", Guid.NewGuid().ToString("N"));

    public ConfigValidationIntegrationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task JsonRepositoryShouldRejectUnsupportedVersion()
    {
        var path = Path.Combine(_tempDirectory, "unsupported-version.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 2,
              "name": "demo",
              "resolution": { "width": 32, "height": 32 },
              "supportedDirections": "four",
              "metadata": {
                "createdUtc": "2026-01-01T00:00:00+00:00",
                "updatedUtc": "2026-01-01T00:00:00+00:00",
                "source": "Json",
                "sourceIdentifier": "demo.json",
                "importedFromLegacy": null
              },
              "mappings": {}
            }
            """);

        var repository = new JsonSpriteConfigRepository();

        var result = await repository.LoadAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Unsupported config version");
    }

    [Fact]
    public async Task JsonRepositoryShouldRejectMalformedJson()
    {
        var path = Path.Combine(_tempDirectory, "malformed.json");
        await File.WriteAllTextAsync(path, "{");

        var repository = new JsonSpriteConfigRepository();

        var result = await repository.LoadAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Config JSON is invalid");
    }

    [Fact]
    public async Task JsonRepositoryShouldRejectOutOfBoundsMappingsDuringLoad()
    {
        var path = Path.Combine(_tempDirectory, "out-of-bounds.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 1,
              "name": "demo",
              "resolution": { "width": 32, "height": 32 },
              "supportedDirections": "four",
              "metadata": {
                "createdUtc": "2026-01-01T00:00:00+00:00",
                "updatedUtc": "2026-01-01T00:00:00+00:00",
                "source": "Json",
                "sourceIdentifier": "demo.json",
                "importedFromLegacy": null
              },
              "mappings": {
                "South": [
                  {
                    "source": { "x": 0, "y": 0 },
                    "target": { "x": 40, "y": 0 }
                  }
                ]
              }
            }
            """);

        var repository = new JsonSpriteConfigRepository();

        var result = await repository.LoadAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("outside of the configured resolution");
    }

    [Fact]
    public async Task JsonRepositoryShouldRejectSaveForInvalidMetadata()
    {
        var created = DateTimeOffset.UtcNow;
        var config = SpriteConfig.CreateEmpty(
            "demo",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "demo"))
            .WithMetadata(new ConfigMetadata(
                created,
                created.AddMinutes(-1),
                ConfigSource.UserCreated,
                "demo",
                null));

        var repository = new JsonSpriteConfigRepository();

        var result = await repository.SaveAsync(Path.Combine(_tempDirectory, "invalid.json"), config, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Updated time must not be earlier");
    }

    [Fact]
    public async Task LegacyImporterShouldRejectEmptyCsv()
    {
        var path = Path.Combine(_tempDirectory, "empty.csv");
        await File.WriteAllTextAsync(path, string.Empty);

        var importer = new LegacyCsvConfigImporter();

        var result = await importer.ImportAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("empty");
    }

    [Fact]
    public async Task LegacyImporterShouldRejectUnsupportedDirection()
    {
        var path = Path.Combine(_tempDirectory, "bad-direction.csv");
        await File.WriteAllLinesAsync(path, ["Up,0,0,1,1"]);

        var importer = new LegacyCsvConfigImporter();

        var result = await importer.ImportAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("unsupported direction");
    }

    [Fact]
    public async Task LegacyImporterShouldRejectWrongColumnCount()
    {
        var path = Path.Combine(_tempDirectory, "bad-columns.csv");
        await File.WriteAllLinesAsync(path, ["South,0,0,1"]);

        var importer = new LegacyCsvConfigImporter();

        var result = await importer.ImportAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("5 columns");
    }

    [Fact]
    public async Task LegacyImporterShouldRejectNonNumericCoordinates()
    {
        var path = Path.Combine(_tempDirectory, "bad-coordinates.csv");
        await File.WriteAllLinesAsync(path, ["South,0,0,a,1"]);

        var importer = new LegacyCsvConfigImporter();

        var result = await importer.ImportAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("non-numeric coordinates");
    }

    [Fact]
    public async Task LegacyImporterShouldSupportEightDirectionConfigs()
    {
        var path = Path.Combine(_tempDirectory, "eight-dir.csv");
        await File.WriteAllLinesAsync(
            path,
            [
                "South,0,0,1,1",
                "North,0,0,1,1",
                "East,0,0,1,1",
                "West,0,0,1,1",
                "SouthEast,0,0,1,1",
                "SouthWest,0,0,1,1",
                "NorthEast,0,0,1,1",
                "NorthWest,0,0,1,1"
            ]);

        var importer = new LegacyCsvConfigImporter();

        var result = await importer.ImportAsync(path, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SupportedDirections.Should().Be(SupportedDirectionSet.Eight);
    }

    [Fact]
    public async Task LegacyImporterShouldReturnCancelledForCancelledOperation()
    {
        var path = Path.Combine(_tempDirectory, "cancelled.csv");
        await File.WriteAllLinesAsync(path, ["South,0,0,1,1"]);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var importer = new LegacyCsvConfigImporter();

        var result = await importer.ImportAsync(path, cancellationTokenSource.Token);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("cancelled");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}