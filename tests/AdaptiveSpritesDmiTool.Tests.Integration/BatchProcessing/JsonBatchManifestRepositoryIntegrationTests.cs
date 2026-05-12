using AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Integration.BatchProcessing;

public sealed class JsonBatchManifestRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "AdaptiveSpritesDmiTool.BatchManifestTests",
        Guid.NewGuid().ToString("N"));

    public JsonBatchManifestRepositoryIntegrationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("NotARealPolicy")]
    public async Task RepositoryShouldRejectNumericOrUndefinedOverwritePolicy(string overwritePolicy)
    {
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.json");
        await WriteManifestAsync(path, overwritePolicy: overwritePolicy);
        var repository = new JsonBatchManifestRepository();

        var result = await repository.LoadAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Unsupported overwrite policy");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("NotARealMode")]
    public async Task RepositoryShouldRejectNumericOrUndefinedRunMode(string runMode)
    {
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.json");
        await WriteManifestAsync(path, runMode: runMode);
        var repository = new JsonBatchManifestRepository();

        var result = await repository.LoadAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Unsupported batch run mode");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    private static async Task WriteManifestAsync(
        string path,
        string runMode = "Incremental",
        string overwritePolicy = "SkipExisting")
    {
        await File.WriteAllTextAsync(
            path,
            $$"""
            {
              "version": 1,
              "outputRoot": "out",
              "defaultRunMode": "{{runMode}}",
              "jobs": [
                {
                  "jobId": "job",
                  "title": "Job",
                  "enabled": true,
                  "inputDirectory": "input",
                  "outputSubdirectory": "sprites",
                  "configPath": "config.json",
                  "overwritePolicy": "{{overwritePolicy}}"
                }
              ]
            }
            """);
    }
}
