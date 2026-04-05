using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Domain;

public sealed class SupportedDirectionSetTests
{
    [Fact]
    public void FourShouldOnlySupportCardinalDirections()
    {
        var directionSet = SupportedDirectionSet.Four;

        directionSet.Supports(SpriteDirection.South).Should().BeTrue();
        directionSet.Supports(SpriteDirection.NorthEast).Should().BeFalse();
    }

    [Fact]
    public void FromDirectionsShouldResolveEightDirectionSet()
    {
        var directionSet = SupportedDirectionSet.FromDirections(
        [
            SpriteDirection.South,
            SpriteDirection.North,
            SpriteDirection.East,
            SpriteDirection.West,
            SpriteDirection.SouthEast,
            SpriteDirection.SouthWest,
            SpriteDirection.NorthEast,
            SpriteDirection.NorthWest
        ]);

        directionSet.Should().Be(SupportedDirectionSet.Eight);
    }
}
