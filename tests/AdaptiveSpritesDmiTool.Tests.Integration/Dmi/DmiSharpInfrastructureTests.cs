using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.Dmi;
using AdaptiveSpritesDmiTool.Infrastructure.Preview;
using DMISharp;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Tests.Integration.Dmi;

public sealed class DmiSharpInfrastructureTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.DmiTests", Guid.NewGuid().ToString("N"));

    public DmiSharpInfrastructureTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ReaderShouldInferEightDirectionAssets()
    {
        var dmiPath = Path.Combine(_tempDirectory, "eight-dir.dmi");
        CreateDmi(
            dmiPath,
            CreateState("base", DirectionDepth.Eight, 2, 1, static direction => direction switch
            {
                StateDirection.NorthEast => CreateImage(new Rgba32(10, 20, 30, 255), new Rgba32(40, 50, 60, 255)),
                _ => CreateImage(new Rgba32(1, 2, 3, 255), new Rgba32(4, 5, 6, 255))
            }));

        var reader = new DmiSharpReader();

        var result = await reader.LoadAsync(dmiPath, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Resolution.Should().Be(new SpriteResolution(2, 1));
        result.Value.SupportedDirections.Should().Be(SupportedDirectionSet.Eight);
        result.Value.States.Should().ContainSingle(state => state.Name == "base");
    }

    [Fact]
    public async Task ReaderShouldPreserveDmiStateOrder()
    {
        var dmiPath = Path.Combine(_tempDirectory, "ordered-states.dmi");
        CreateDmi(
            dmiPath,
            CreateState("human32x", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(1, 1, 1, 255))),
            CreateState("monkey", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(2, 2, 2, 255))),
            CreateState("vox", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(3, 3, 3, 255))),
            CreateState("dwarb", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(4, 4, 4, 255))),
            CreateState("humanBad", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(5, 5, 5, 255))),
            CreateState("underDefault", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(6, 6, 6, 255))),
            CreateState("coatDefault", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(7, 7, 7, 255))),
            CreateState("color32x", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(8, 8, 8, 255))),
            CreateState("colourUnderDefault", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(9, 9, 9, 255))),
            CreateState("Empty", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(10, 10, 10, 255))));

        var reader = new DmiSharpReader();

        var result = await reader.LoadAsync(dmiPath, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.States.Select(static state => state.Name).Should().Equal(
            "human32x",
            "monkey",
            "vox",
            "dwarb",
            "humanBad",
            "underDefault",
            "coatDefault",
            "color32x",
            "colourUnderDefault",
            "Empty");
    }

    [Fact]
    public async Task ReaderShouldRejectEmptyDmiFiles()
    {
        var dmiPath = Path.Combine(_tempDirectory, "empty.dmi");
        await File.WriteAllBytesAsync(dmiPath, []);

        var reader = new DmiSharpReader();

        var result = await reader.LoadAsync(dmiPath, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("empty");
    }

    [Fact]
    public async Task PreviewBuilderShouldHandleMissingLandmarkAndOverlayGracefully()
    {
        var dmiPath = Path.Combine(_tempDirectory, "preview.dmi");
        CreateDmi(
            dmiPath,
            CreateState("base", DirectionDepth.Four, 2, 1, static _ => CreateImage(
                new Rgba32(255, 0, 0, 255),
                new Rgba32(0, 255, 0, 255))));

        var reader = new DmiSharpReader();
        var asset = (await reader.LoadAsync(dmiPath, CancellationToken.None)).Value;
        var config = SpriteConfig.CreateEmpty(
                "preview",
                asset.Resolution,
                asset.SupportedDirections,
                ConfigMetadata.CreateNew(ConfigSource.UserCreated, "preview"))
            .SetMapping(SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(1, 0));

        var previewBuilder = new DmiSharpPreviewBuilder();
        var request = new AdaptiveSpritesDmiTool.Application.PreviewBuildRequest(
            asset,
            config,
            new AdaptiveSpritesDmiTool.Application.PreviewSelection("base", "missing-landmark", null),
            SpriteDirection.South);

        var result = await previewBuilder.BuildAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseImage.Should().NotBeNull();
        result.Value.LandmarkImage.Should().BeNull();
        result.Value.OverlayImage.Should().BeNull();
        result.Value.CompositeImage.Should().NotBeNull();
        ReadPixel(result.Value.BaseImage!, 0, 0).Should().Be(new Rgba32(255, 0, 0, 255));
        ReadPixel(result.Value.CompositeImage!, 0, 0).Should().Be(new Rgba32(0, 255, 0, 255));
    }

    [Fact]
    public async Task PreviewBuilderShouldComposeOptionalLayersWhenAvailable()
    {
        var dmiPath = Path.Combine(_tempDirectory, "layered-preview.dmi");
        CreateDmi(
            dmiPath,
            CreateState("base", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(255, 0, 0, 255))),
            CreateState("landmark", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(0, 0, 255, 255))),
            CreateState("overlay", DirectionDepth.Four, 1, 1, static _ => CreateImage(new Rgba32(0, 255, 0, 128))));

        var reader = new DmiSharpReader();
        var asset = (await reader.LoadAsync(dmiPath, CancellationToken.None)).Value;
        var config = SpriteConfig.CreateEmpty(
            "preview",
            asset.Resolution,
            asset.SupportedDirections,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "preview"));

        var previewBuilder = new DmiSharpPreviewBuilder();
        var request = new AdaptiveSpritesDmiTool.Application.PreviewBuildRequest(
            asset,
            config,
            new AdaptiveSpritesDmiTool.Application.PreviewSelection("base", "landmark", "overlay"),
            SpriteDirection.South);

        var result = await previewBuilder.BuildAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LandmarkImage.Should().NotBeNull();
        result.Value.OverlayImage.Should().NotBeNull();
        result.Value.CompositeImage.Should().NotBeNull();
        ReadPixel(result.Value.CompositeImage!, 0, 0).A.Should().BeGreaterThan((byte)0);
    }

    private static void CreateDmi(string path, params DMIState[] states)
    {
        using var dmiFile = new DMIFile(states[0].Width, states[0].Height);
        foreach (var state in states)
        {
            dmiFile.AddState(state);
        }

        dmiFile.Save(path);
        File.Exists(path).Should().BeTrue();
    }

    private static DMIState CreateState(
        string name,
        DirectionDepth depth,
        int width,
        int height,
        Func<StateDirection, Image<Rgba32>> frameFactory)
    {
        var state = new DMIState(name, depth, 1, width, height);
        foreach (var direction in GetDirections(depth))
        {
            state.SetFrame(frameFactory(direction), direction, 0);
        }

        return state;
    }

    private static IReadOnlyList<StateDirection> GetDirections(DirectionDepth depth) =>
        depth switch
        {
            DirectionDepth.One => [StateDirection.South],
            DirectionDepth.Four => [StateDirection.South, StateDirection.North, StateDirection.East, StateDirection.West],
            DirectionDepth.Eight =>
            [
                StateDirection.South,
                StateDirection.North,
                StateDirection.East,
                StateDirection.West,
                StateDirection.SouthEast,
                StateDirection.SouthWest,
                StateDirection.NorthEast,
                StateDirection.NorthWest
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(depth), depth, null)
        };

    private static Image<Rgba32> CreateImage(params Rgba32[] pixels)
    {
        var image = new Image<Rgba32>(pixels.Length, 1);
        for (var index = 0; index < pixels.Length; index++)
        {
            image[index, 0] = pixels[index];
        }

        return image;
    }

    private static Rgba32 ReadPixel(AdaptiveSpritesDmiTool.Application.SpriteImage image, int x, int y)
    {
        var index = ((y * image.Width) + x) * 4;
        return new Rgba32(
            image.RgbaBytes[index],
            image.RgbaBytes[index + 1],
            image.RgbaBytes[index + 2],
            image.RgbaBytes[index + 3]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
