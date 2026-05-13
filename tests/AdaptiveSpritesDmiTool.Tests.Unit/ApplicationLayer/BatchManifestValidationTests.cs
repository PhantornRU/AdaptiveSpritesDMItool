using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Application;

public sealed class BatchManifestValidationTests
{
    [Theory]
    [InlineData("sprites", "sprites/")]
    [InlineData("sprites", "./sprites")]
    [InlineData("sprites", "sprites/.")]
    [InlineData(".", "")]
    public async Task ExecuteAsyncShouldRejectDuplicateOutputDirectoriesWithEquivalentSpelling(
        string firstOutputSubdirectory,
        string secondOutputSubdirectory)
    {
        using var workspace = TestWorkspace.Create();
        var inputDirectory = workspace.CreateDirectory("input");
        var configPath = workspace.WriteTextFile("config.json", "{}");
        var outputRoot = workspace.CreateDirectory("out");

        var manifest = new BatchManifest(
            outputRoot,
            BatchRunMode.RebuildAll,
            [
                new BatchManifestJob(
                    "job-a",
                    "Job A",
                    Enabled: true,
                    inputDirectory,
                    firstOutputSubdirectory,
                    configPath,
                    OverwritePolicy.SkipExisting),
                new BatchManifestJob(
                    "job-b",
                    "Job B",
                    Enabled: true,
                    inputDirectory,
                    secondOutputSubdirectory,
                    configPath,
                    OverwritePolicy.SkipExisting)
            ]);

        var sut = new RunBatchManifestUseCase(
            new StubBatchManifestRepository(manifest),
            new ThrowingConfigRepository(),
            new ThrowingBatchProcessingService(),
            new ThrowingBatchArtifactsStore(),
            new ThrowingBatchFingerprintService());

        var result = await sut.ExecuteAsync(
            workspace.GetPath("manifest.json"),
            BatchRunMode.RebuildAll,
            null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("duplicate outputSubdirectory");
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string root) => Root = root;

        private string Root { get; }

        public static TestWorkspace Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "AdaptiveSpritesDmiTool.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TestWorkspace(root);
        }

        public string CreateDirectory(string relativePath)
        {
            var path = GetPath(relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        public string WriteTextFile(string relativePath, string contents)
        {
            var path = GetPath(relativePath);
            File.WriteAllText(path, contents);
            return path;
        }

        public string GetPath(string relativePath) => Path.Combine(Root, relativePath);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class StubBatchManifestRepository(BatchManifest manifest) : IBatchManifestRepository
    {
        public Task<Result<BatchManifest>> LoadAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(manifest));

        public Task<Result> SaveAsync(string path, BatchManifest manifest, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class ThrowingConfigRepository : IConfigRepository
    {
        public Task<Result<SpriteConfig>> LoadAsync(string path, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Config repository should not be called when manifest validation fails.");

        public Task<Result> SaveAsync(string path, SpriteConfig config, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Config repository should not be called when manifest validation fails.");
    }

    private sealed class ThrowingBatchProcessingService : IBatchProcessingService
    {
        public Task<Result<BatchJobResult>> RunAsync(
                BatchJobRequest request,
                IProgress<BatchProgress>? progress,
                CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Batch processing should not be called when manifest validation fails.");
    }

    private sealed class ThrowingBatchArtifactsStore : IBatchArtifactsStore
    {
        public Task<Result<BatchProcessedJournal>> LoadJournalAsync(string outputRoot, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Artifacts store should not be called when manifest validation fails.");

        public Task<Result> SaveJournalAsync(string outputRoot, BatchProcessedJournal journal, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Artifacts store should not be called when manifest validation fails.");

        public Task<Result<BatchArtifactsInfo>> WriteRunArtifactsAsync(
                string outputRoot,
                BatchRunReport report,
                CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Artifacts store should not be called when manifest validation fails.");
    }

    private sealed class ThrowingBatchFingerprintService : IBatchFingerprintService
    {
        public Task<Result<string>> ComputeInputFingerprintAsync(string inputPath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Fingerprint service should not be called when manifest validation fails.");

        public string ComputeConfigFingerprint(SpriteConfig config) =>
            throw new InvalidOperationException("Fingerprint service should not be called when manifest validation fails.");
    }
}
