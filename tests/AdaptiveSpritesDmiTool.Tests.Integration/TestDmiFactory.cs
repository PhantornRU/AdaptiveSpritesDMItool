using DMISharp;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AdaptiveSpritesDmiTool.Tests.Integration;

internal static class TestDmiFactory
{
    public static void CreateDmi(string path, params DMIState[] states)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(states);
        if (states.Length == 0)
        {
            throw new ArgumentException("At least one state is required.", nameof(states));
        }

        using var dmiFile = new DMIFile(states[0].Width, states[0].Height);
        foreach (var state in states)
        {
            dmiFile.AddState(state);
        }

        dmiFile.Save(path);
        File.Exists(path).Should().BeTrue();
    }

    public static DMIState CreateState(
        string name,
        DirectionDepth depth,
        int width,
        int height,
        Func<StateDirection, Image<Rgba32>> frameFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(frameFactory);

        var state = new DMIState(name, depth, 1, width, height);
        foreach (var direction in GetDirections(depth))
        {
            state.SetFrame(frameFactory(direction), direction, 0);
        }

        return state;
    }

    public static IReadOnlyList<StateDirection> GetDirections(DirectionDepth depth) =>
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

    public static Image<Rgba32> CreateImage(params Rgba32[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);

        var image = new Image<Rgba32>(pixels.Length, 1);
        for (var index = 0; index < pixels.Length; index++)
        {
            image[index, 0] = pixels[index];
        }

        return image;
    }

    public static Rgba32 ReadPixel(Image<Rgba32> image, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(image);
        return image[x, y];
    }
}
