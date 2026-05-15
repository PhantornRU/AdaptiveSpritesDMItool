using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using DMISharp;
using FluentAssertions;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Tests.Integration.Dmi;

public sealed class DmiSharpConfigWriterIntegrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.DmiWriterTests", Guid.NewGuid().ToString("N"));

    public DmiSharpConfigWriterIntegrationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ApplyAsyncShouldTransformFourDirectionDmiAndOverwriteExistingOutput()
    {
        var inputPath = Path.Combine(_tempDirectory, "input.dmi");
        var outputPath = Path.Combine(_tempDirectory, "output.dmi");

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
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"))
            .SetMapping(SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(1, 0));

        var writer = new DmiSharpConfigWriter();
        var request = new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting);

        var result = await writer.ApplyAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BatchFileStatus.Processed);
        result.Value.InputPath.Should().Be(Path.GetFullPath(inputPath));
        result.Value.OutputPath.Should().Be(Path.GetFullPath(outputPath));

        using var outputFile = new DMIFile(outputPath);
        var southFrame = outputFile.States.First().GetFrame(StateDirection.South, 0);
        southFrame.Should().NotBeNull();
        TestDmiFactory.ReadPixel(southFrame!, 0, 0).Should().Be(new Rgba32(0, 255, 0, 255));
        TestDmiFactory.ReadPixel(southFrame!, 1, 0).Should().Be(new Rgba32(0, 255, 0, 255));
    }

    [Fact]
    public async Task ApplyAsyncShouldSupportEightDirectionDmi()
    {
        var inputPath = Path.Combine(_tempDirectory, "eight-dir.dmi");
        var outputPath = Path.Combine(_tempDirectory, "eight-dir-output.dmi");

        TestDmiFactory.CreateDmi(
            inputPath,
            TestDmiFactory.CreateState("hero", DirectionDepth.Eight, 1, 1, static direction => TestDmiFactory.CreateImage(
                direction == StateDirection.NorthEast
                    ? new Rgba32(10, 20, 30, 255)
                    : new Rgba32(200, 200, 200, 255))));

        var config = SpriteConfig.CreateEmpty(
                "config-eight",
                new SpriteResolution(1, 1),
                SupportedDirectionSet.Eight,
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"))
            .SetMapping(SpriteDirection.NorthEast, new PixelCoordinate(0, 0), null);

        var writer = new DmiSharpConfigWriter();

        var result = await writer.ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, outputPath, config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        using var outputFile = new DMIFile(outputPath);
        var northEastFrame = outputFile.States.First().GetFrame(StateDirection.NorthEast, 0);
        northEastFrame.Should().NotBeNull();
        TestDmiFactory.ReadPixel(northEastFrame!, 0, 0).A.Should().Be(0);
    }

    [Fact]
    public async Task ApplyAsyncShouldRejectEmptyDmiFiles()
    {
        var inputPath = Path.Combine(_tempDirectory, "empty.dmi");
        await File.WriteAllBytesAsync(inputPath, []);

        var writer = new DmiSharpConfigWriter();
        var config = SpriteConfig.CreateEmpty(
            "config",
            new SpriteResolution(1, 1),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "writer-test"));

        var result = await writer.ApplyAsync(
            new ApplyConfigToFileRequest(inputPath, Path.Combine(_tempDirectory, "out.dmi"), config, OverwritePolicy.OverwriteExisting),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("empty");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
