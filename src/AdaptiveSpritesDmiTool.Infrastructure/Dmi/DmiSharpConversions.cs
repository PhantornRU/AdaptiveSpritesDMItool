using AdaptiveSpritesDmiTool.Domain.Configurations;
using DMISharp;

namespace AdaptiveSpritesDmiTool.Infrastructure.Dmi;

internal static class DmiSharpConversions
{
    public static SupportedDirectionSet InferSupportedDirections(IEnumerable<DMIState> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        var maxDepth = states
            .Select(static state => state.DirectionDepth)
            .DefaultIfEmpty(DirectionDepth.Four)
            .Max();

        return maxDepth == DirectionDepth.Eight
            ? SupportedDirectionSet.Eight
            : SupportedDirectionSet.Four;
    }

    public static SpriteResolution InferResolution(DMIFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var firstState = file.States.FirstOrDefault();
        var width = file.Metadata.FrameWidth > 0 ? file.Metadata.FrameWidth : firstState?.Width ?? 0;
        var height = file.Metadata.FrameHeight > 0 ? file.Metadata.FrameHeight : firstState?.Height ?? 0;

        return new SpriteResolution(width, height);
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
            _ => throw new ArgumentOutOfRangeException(nameof(depth), depth, "Unsupported DMI direction depth.")
        };

    public static StateDirection ToDmiDirection(SpriteDirection direction) =>
        direction switch
        {
            SpriteDirection.South => StateDirection.South,
            SpriteDirection.North => StateDirection.North,
            SpriteDirection.East => StateDirection.East,
            SpriteDirection.West => StateDirection.West,
            SpriteDirection.SouthEast => StateDirection.SouthEast,
            SpriteDirection.SouthWest => StateDirection.SouthWest,
            SpriteDirection.NorthEast => StateDirection.NorthEast,
            SpriteDirection.NorthWest => StateDirection.NorthWest,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported domain direction.")
        };

    public static SpriteDirection ToDomainDirection(StateDirection direction) =>
        direction switch
        {
            StateDirection.South => SpriteDirection.South,
            StateDirection.North => SpriteDirection.North,
            StateDirection.East => SpriteDirection.East,
            StateDirection.West => SpriteDirection.West,
            StateDirection.SouthEast => SpriteDirection.SouthEast,
            StateDirection.SouthWest => SpriteDirection.SouthWest,
            StateDirection.NorthEast => SpriteDirection.NorthEast,
            StateDirection.NorthWest => SpriteDirection.NorthWest,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported DMI direction.")
        };
}