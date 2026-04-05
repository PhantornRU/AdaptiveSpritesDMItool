using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Domain;

public sealed class SpriteResolutionTests
{
    [Fact]
    public void ConstructorShouldRejectNonPositiveValues()
    {
        var action = () => new SpriteResolution(0, 32);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ContainsShouldReturnTrueForCoordinateWithinBounds()
    {
        var resolution = new SpriteResolution(32, 32);

        resolution.Contains(new PixelCoordinate(31, 31)).Should().BeTrue();
    }
}
