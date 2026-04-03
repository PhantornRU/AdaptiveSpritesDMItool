using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Integration.BatchProcessing;

public sealed class DeterministicBatchProcessingServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.BatchTests", Guid.NewGuid().ToString("N"));

    public DeterministicBatchProcessingServiceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task RunAsyncShouldProcessFilesInDeterministicOrder()
    {
        var inputDirectory = CreateDirectory("input");
        var outputDirectory = CreateDirectory("output");
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "b.dmi"), "b");
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "a.dmi"), "a");

        var writer = new RecordingDmiWriter();
        var service = new DeterministicBatchProcessingService(writer);
        var request = CreateRequest(inputDirectory, outputDirectory, OverwritePolicy.OverwriteExisting);

        var result = await service.RunAsync(request, progress: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        writer.Requests.Select(static item => Path.GetFileName(item.InputPath))
            .Should()
            .ContainInOrder("a.dmi", "b.dmi");
    }

    [Fact]
    public async Task RunAsyncShouldSkipExistingFilesWhenPolicyIsSkipExisting()
    {
        var inputDirectory = CreateDirectory("input");
        var outputDirectory = CreateDirectory("output");
        var inputPath = Path.Combine(inputDirectory, "sprite.dmi");
        var outputPath = Path.Combine(outputDirectory, "sprite.dmi");

        await File.WriteAllTextAsync(inputPath, "input");
        await File.WriteAllTextAsync(outputPath, "existing");

        var writer = new RecordingDmiWriter();
        var service = new DeterministicBatchProcessingService(writer);

        var result = await service.RunAsync(CreateRequest(inputDirectory, outputDirectory, OverwritePolicy.SkipExisting), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().ContainSingle();
        result.Value.Files[0].Status.Should().Be(BatchFileStatus.Skipped);
        writer.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsyncShouldFailExistingFilesWhenPolicyIsFailIfExists()
    {
        var inputDirectory = CreateDirectory("input");
        var outputDirectory = CreateDirectory("output");
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "sprite.dmi"), "input");
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "sprite.dmi"), "existing");

        var writer = new RecordingDmiWriter();
        var service = new DeterministicBatchProcessingService(writer);

        var result = await service.RunAsync(CreateRequest(inputDirectory, outputDirectory, OverwritePolicy.FailIfExists), null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files[0].Status.Should().Be(BatchFileStatus.Failed);
        result.Value.Files[0].Message.Should().Contain("already exists");
        writer.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsyncShouldReportProgressAndCancellation()
    {
        var inputDirectory = CreateDirectory("input");
        var outputDirectory = CreateDirectory("output");
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "a.dmi"), "a");
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "b.dmi"), "b");

        var writer = new RecordingDmiWriter();
        var service = new DeterministicBatchProcessingService(writer);
        using var cancellationTokenSource = new CancellationTokenSource();
        var progress = new RecordingProgress(value =>
        {
            if (value.ProcessedFiles == 1)
            {
                cancellationTokenSource.Cancel();
            }
        });

        var result = await service.RunAsync(
            CreateRequest(inputDirectory, outputDirectory, OverwritePolicy.OverwriteExisting),
            progress,
            cancellationTokenSource.Token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(file => file.Status == BatchFileStatus.Cancelled);
        progress.Values.Should().NotBeEmpty();
        progress.Values[^1].ProcessedFiles.Should().Be(2);
    }

    private static BatchJobRequest CreateRequest(string inputDirectory, string outputDirectory, OverwritePolicy overwritePolicy) =>
        new(
            inputDirectory,
            outputDirectory,
            SpriteConfig.CreateEmpty(
                "config",
                new SpriteResolution(32, 32),
                SupportedDirectionSet.Four,
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test")),
            overwritePolicy);

    private string CreateDirectory(string name)
    {
        var directory = Path.Combine(_tempDirectory, name);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    private sealed class RecordingDmiWriter : IDmiWriter
    {
        public List<ApplyConfigToFileRequest> Requests { get; } = [];

        public Task<Result<BatchFileResult>> ApplyAsync(ApplyConfigToFileRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(
                Result.Success(
                    new BatchFileResult(request.InputPath, request.OutputPath, BatchFileStatus.Processed, "processed")));
        }
    }

    private sealed class RecordingProgress(Action<BatchProgress> onReport) : IProgress<BatchProgress>
    {
        public List<BatchProgress> Values { get; } = [];

        public void Report(BatchProgress value)
        {
            Values.Add(value);
            onReport(value);
        }
    }
}