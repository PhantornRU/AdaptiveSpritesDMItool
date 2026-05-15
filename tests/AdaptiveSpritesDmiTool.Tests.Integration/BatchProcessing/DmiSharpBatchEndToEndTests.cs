using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Application.Common;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using DMISharp;
using FluentAssertions;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Tests.Integration.BatchProcessing;

public sealed class DmiSharpBatchEndToEndTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.BatchE2eTests", Guid.NewGuid().ToString("N"));

    public DmiSharpBatchEndToEndTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task RunAsyncShouldApplyConfigAndOverwriteExistingOutputDeterministically()
    {
        var inputDirectory = CreateDirectory("input");
        var outputDirectory = CreateDirectory("output");
        var inputPath = Path.Combine(inputDirectory, "sprite.dmi");
        var outputPath = Path.Combine(outputDirectory, "sprite.dmi");

        TestDmiFactory.CreateDmi(
            inputPath,
            TestDmiFactory.CreateState("base", DirectionDepth.Four, 2, 1, static direction => direction switch
            {
                StateDirection.South => TestDmiFactory.CreateImage(new Rgba32(255, 0, 0, 255), new Rgba32(0, 255, 0, 255)),
                StateDirection.North => TestDmiFactory.CreateImage(new Rgba32(0, 0, 255, 255), new Rgba32(255, 255, 0, 255)),
                StateDirection.East => TestDmiFactory.CreateImage(new Rgba32(255, 0, 255, 255), new Rgba32(0, 255, 255, 255)),
                _ => TestDmiFactory.CreateImage(new Rgba32(32, 32, 32, 255), new Rgba32(64, 64, 64, 255))
            }));

        TestDmiFactory.CreateDmi(
            outputPath,
            TestDmiFactory.CreateState("base", DirectionDepth.Four, 2, 1, static _ => TestDmiFactory.CreateImage(
                new Rgba32(1, 1, 1, 255),
                new Rgba32(2, 2, 2, 255))));

        var config = SpriteConfig.CreateEmpty(
                "config",
                new SpriteResolution(2, 1),
                SupportedDirectionSet.Four,
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "batch-e2e"))
            .SetMapping(SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(1, 0));

        var service = new DeterministicBatchProcessingService(new DmiSharpConfigWriter());
        var progress = new RecordingProgress();

        var result = await service.RunAsync(
            new BatchJobRequest(inputDirectory, outputDirectory, config, OverwritePolicy.OverwriteExisting),
            progress,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().ContainSingle();
        result.Value.Files[0].Status.Should().Be(BatchFileStatus.Processed);
        result.Value.Files[0].InputPath.Should().Be(Path.GetFullPath(inputPath));
        result.Value.Files[0].OutputPath.Should().Be(Path.GetFullPath(outputPath));
        progress.Values.Should().NotBeEmpty();
        progress.Values[^1].ProcessedFiles.Should().Be(1);

        using var outputFile = new DMIFile(outputPath);
        var southFrame = outputFile.States.First().GetFrame(StateDirection.South, 0);
        southFrame.Should().NotBeNull();
        TestDmiFactory.ReadPixel(southFrame!, 0, 0).Should().Be(new Rgba32(0, 255, 0, 255));
    }

    [Fact]
    public async Task RunAsyncShouldSkipExistingFilesAndPreserveInputPathInResult()
    {
        var inputDirectory = CreateDirectory("input");
        var outputDirectory = CreateDirectory("output");
        var inputPath = Path.Combine(inputDirectory, "sprite.dmi");
        var outputPath = Path.Combine(outputDirectory, "sprite.dmi");

        await File.WriteAllTextAsync(inputPath, "input");
        await File.WriteAllTextAsync(outputPath, "existing");

        var service = new DeterministicBatchProcessingService(new RecordingWriter());

        var result = await service.RunAsync(
            new BatchJobRequest(
                inputDirectory,
                outputDirectory,
                SpriteConfig.CreateEmpty(
                    "config",
                    new SpriteResolution(1, 1),
                    SupportedDirectionSet.Four,
                    ConfigMetadata.CreateNew(ConfigSource.UserCreated, "batch-e2e")),
                OverwritePolicy.SkipExisting),
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().ContainSingle(file => file.Status == BatchFileStatus.Skipped);
        result.Value.Files[0].InputPath.Should().Be(Path.GetFullPath(inputPath));
        result.Value.Files[0].OutputPath.Should().Be(Path.GetFullPath(outputPath));
    }

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

    private sealed class RecordingWriter : IDmiWriter
    {
        public Task<Result<BatchFileResult>> ApplyAsync(ApplyConfigToFileRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(new BatchFileResult(request.InputPath, request.OutputPath, BatchFileStatus.Processed, "processed")));
    }

    private sealed class RecordingProgress : IProgress<BatchProgress>
    {
        public List<BatchProgress> Values { get; } = [];

        public void Report(BatchProgress value) => Values.Add(value);
    }
}
