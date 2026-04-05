using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Domain;

public sealed class PixelCoordinateTests
{
    [Fact]
    public void ConstructorShouldRejectNegativeX()
    {
        var action = () => new PixelCoordinate(-1, 0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConstructorShouldRejectNegativeY()
    {
        var action = () => new PixelCoordinate(0, -1);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
