using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using AdaptiveSpritesDmiTool.Infrastructure.BatchProcessing;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using AdaptiveSpritesDmiTool.Infrastructure.Documents;
using DMISharp;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Tests.Integration.BatchProcessing;

public sealed class NativeAssetBatchIntegrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "AdaptiveSpritesDmiTool.NativeBatchTests",
        Guid.NewGuid().ToString("N"));

    public NativeAssetBatchIntegrationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task PngInputShouldProduceIndependentDmiAndManagedPngOutputs()
    {
        var inputDirectory = CreateDirectory("mixed-input");
        var outputDirectory = CreateDirectory("mixed-output");
        var inputPath = Path.Combine(inputDirectory, "sprite.png");
        await SaveTwoPixelPngAsync(inputPath);
        var config = CreateFourDirectionConfig();

        var result = await CreateService().RunAsync(
            new BatchJobRequest(
                inputDirectory,
                outputDirectory,
                config,
                OverwritePolicy.OverwriteExisting,
                OutputFormats: [WorkspaceBatchOutputFormat.Dmi, WorkspaceBatchOutputFormat.Png],
                RasterExportSettings: WorkspaceRasterExportSettings.Default),
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().HaveCount(2);
        result.Value.Files.Should().OnlyContain(static file => file.Status == BatchFileStatus.Processed);

        var dmiPath = Path.Combine(outputDirectory, "sprite.dmi");
        using var dmi = new DMIFile(dmiPath);
        dmi.States.Should().ContainSingle();
        using var south = dmi.States.First().GetFrame(StateDirection.South, 0)!.Clone();
        south[0, 0].Should().Be(new Rgba32(20, 21, 22, 255));

        var pngDirectory = Path.Combine(outputDirectory, "sprite.png-export");
        File.Exists(Path.Combine(pngDirectory, "sprite.png")).Should().BeTrue();
        File.Exists(Path.Combine(pngDirectory, ".adaptive-dmi-export.json")).Should().BeTrue();
        Directory.GetFiles(pngDirectory, "*.adaptive-dmi.json").Should().ContainSingle();
    }

    [Fact]
    public async Task EightDirectionRasterMismatchShouldFailOnlyPngOutput()
    {
        var inputDirectory = CreateDirectory("profile-input");
        var outputDirectory = CreateDirectory("profile-output");
        await SaveTwoPixelPngAsync(Path.Combine(inputDirectory, "profile.png"));

        var result = await CreateService().RunAsync(
            new BatchJobRequest(
                inputDirectory,
                outputDirectory,
                CreateFourDirectionConfig(),
                OverwritePolicy.OverwriteExisting,
                OutputFormats: [WorkspaceBatchOutputFormat.Dmi, WorkspaceBatchOutputFormat.Png],
                RasterExportSettings: new WorkspaceRasterExportSettings(
                    SpriteDirectionDepth.Eight,
                    SpriteDocumentExportFormat.PngSheet)),
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().ContainSingle(file =>
            file.Status == BatchFileStatus.Processed && file.OutputPath!.EndsWith(".dmi", StringComparison.OrdinalIgnoreCase));
        result.Value.Files.Should().ContainSingle(file =>
            file.Status == BatchFileStatus.Failed && file.Message.Contains("8-direction", StringComparison.OrdinalIgnoreCase));
        File.Exists(Path.Combine(outputDirectory, "profile.dmi")).Should().BeTrue();
        Directory.Exists(Path.Combine(outputDirectory, "profile.png-export")).Should().BeFalse();
    }

    [Fact]
    public async Task UnmanagedPngDestinationShouldNotBlockDmiOutput()
    {
        var inputDirectory = CreateDirectory("guard-input");
        var outputDirectory = CreateDirectory("guard-output");
        await SaveTwoPixelPngAsync(Path.Combine(inputDirectory, "guard.png"));
        var pngDirectory = Path.Combine(outputDirectory, "guard.png-export");
        Directory.CreateDirectory(pngDirectory);
        var protectedPath = Path.Combine(pngDirectory, "keep.txt");
        await File.WriteAllTextAsync(protectedPath, "keep");

        var result = await CreateService().RunAsync(
            new BatchJobRequest(
                inputDirectory,
                outputDirectory,
                CreateFourDirectionConfig(),
                OverwritePolicy.OverwriteExisting,
                OutputFormats: [WorkspaceBatchOutputFormat.Dmi, WorkspaceBatchOutputFormat.Png],
                RasterExportSettings: WorkspaceRasterExportSettings.Default),
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().ContainSingle(static file => file.Status == BatchFileStatus.Processed);
        result.Value.Files.Should().ContainSingle(static file => file.Status == BatchFileStatus.Failed);
        File.Exists(Path.Combine(outputDirectory, "guard.dmi")).Should().BeTrue();
        (await File.ReadAllTextAsync(protectedPath)).Should().Be("keep");
    }

    [Fact]
    public async Task FourDirectionDmiShouldAcceptEightDirectionConfigForFourDirectionRasterProfile()
    {
        var inputDirectory = CreateDirectory("dmi-input");
        var outputDirectory = CreateDirectory("dmi-output");
        var inputPath = Path.Combine(inputDirectory, "native.dmi");
        TestDmiFactory.CreateDmi(
            inputPath,
            TestDmiFactory.CreateState(
                "idle",
                DirectionDepth.Four,
                2,
                1,
                static _ => TestDmiFactory.CreateImage(
                    new Rgba32(1, 2, 3, 255),
                    new Rgba32(4, 5, 6, 255))));
        var config = SpriteConfig.CreateEmpty(
            "eight-direction-config",
            new SpriteResolution(2, 1),
            SupportedDirectionSet.Eight,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "native-batch-test"));

        var result = await CreateService().RunAsync(
            new BatchJobRequest(
                inputDirectory,
                outputDirectory,
                config,
                OverwritePolicy.OverwriteExisting,
                OutputFormats: [WorkspaceBatchOutputFormat.Dmi, WorkspaceBatchOutputFormat.Png],
                RasterExportSettings: new WorkspaceRasterExportSettings(
                    SpriteDirectionDepth.Four,
                    SpriteDocumentExportFormat.PngSequence)),
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().HaveCount(2);
        result.Value.Files.Should().OnlyContain(static file => file.Status == BatchFileStatus.Processed);
        File.Exists(Path.Combine(outputDirectory, "native.dmi")).Should().BeTrue();
        Directory.GetFiles(
                Path.Combine(outputDirectory, "native.png-export"),
                "*.png",
                SearchOption.AllDirectories)
            .Should().HaveCount(4);
    }

    [Fact]
    public async Task SameStemInputsShouldFailCollidingOutputsWithoutOverwritingEitherSource()
    {
        var inputDirectory = CreateDirectory("collision-input");
        var outputDirectory = CreateDirectory("collision-output");
        var pngPath = Path.Combine(inputDirectory, "same.png");
        var dmiPath = Path.Combine(inputDirectory, "same.dmi");
        await SaveTwoPixelPngAsync(pngPath);
        TestDmiFactory.CreateDmi(
            dmiPath,
            TestDmiFactory.CreateState(
                "idle",
                DirectionDepth.One,
                2,
                1,
                static _ => TestDmiFactory.CreateImage(
                    new Rgba32(1, 2, 3, 255),
                    new Rgba32(4, 5, 6, 255))));

        var result = await CreateService().RunAsync(
            new BatchJobRequest(
                inputDirectory,
                outputDirectory,
                CreateFourDirectionConfig(),
                OverwritePolicy.OverwriteExisting,
                OutputFormats: [WorkspaceBatchOutputFormat.Dmi, WorkspaceBatchOutputFormat.Png],
                RasterExportSettings: WorkspaceRasterExportSettings.Default),
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().HaveCount(4);
        result.Value.Files.Should().OnlyContain(file =>
            file.Status == BatchFileStatus.Failed &&
            file.Message.Contains("collision", StringComparison.OrdinalIgnoreCase));
        Directory.EnumerateFileSystemEntries(outputDirectory).Should().BeEmpty();
        File.Exists(pngPath).Should().BeTrue();
        File.Exists(dmiPath).Should().BeTrue();
    }

    private static DeterministicBatchProcessingService CreateService()
    {
        var probe = new AssetProbeService();
        var importer = new SpriteDocumentImporter(probe);
        var repository = new SpriteDocumentSidecarRepository(probe);
        var frameSource = new SpriteFrameSource(probe);
        var exporter = new SpriteDocumentExporter(frameSource, probe, repository);
        return new DeterministicBatchProcessingService(
            new DmiSharpConfigWriter(),
            probe,
            importer,
            frameSource,
            exporter);
    }

    private static SpriteConfig CreateFourDirectionConfig() =>
        SpriteConfig.CreateEmpty(
                "native-batch",
                new SpriteResolution(2, 1),
                SupportedDirectionSet.Four,
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "native-batch-test"))
            .SetMapping(SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(1, 0));

    private static async Task SaveTwoPixelPngAsync(string path)
    {
        using var image = new Image<Rgba32>(2, 1);
        image[0, 0] = new Rgba32(10, 11, 12, 255);
        image[1, 0] = new Rgba32(20, 21, 22, 255);
        await image.SaveAsPngAsync(path);
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
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
