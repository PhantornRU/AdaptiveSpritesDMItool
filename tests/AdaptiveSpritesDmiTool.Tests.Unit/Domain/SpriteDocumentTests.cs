using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Domain;

public sealed class SpriteDocumentTests
{
    [Fact]
    public void OneDirectionDepthShouldMapToSouthOnly()
    {
        SpriteDirectionDepth.One.GetDirections().Should().Equal(SpriteDirection.South);
    }

    [Fact]
    public void StateShouldRequireEveryDirectionAndFrameExactlyOnce()
    {
        var sourceId = Guid.NewGuid();
        var frame = new SpriteDocumentFrame(
            SpriteDirection.South,
            0,
            new SpriteFrameReference(sourceId, new SpriteSourceRectangle(0, 0, 1, 1)));

        var action = () => new SpriteDocumentState(
            "walk",
            SpriteDirectionDepth.Four,
            1,
            SpriteAnimationMetadata.Static,
            [frame, frame, frame, frame]);

        action.Should().Throw<ArgumentException>().WithMessage("*South*frame*");
    }

    [Fact]
    public void DocumentShouldRejectFrameCropOutsideSourceBounds()
    {
        var sourceId = Guid.NewGuid();
        var source = new SpriteSourceReference(
            sourceId,
            "source.png",
            "C:\\sprites\\source.png",
            SpriteSourceFormat.Png,
            2,
            2,
            10,
            new string('a', 64));
        var state = new SpriteDocumentState(
            "idle",
            SpriteDirectionDepth.One,
            1,
            SpriteAnimationMetadata.Static,
            [new SpriteDocumentFrame(
                SpriteDirection.South,
                0,
                new SpriteFrameReference(sourceId, new SpriteSourceRectangle(1, 1, 2, 2)))]);

        var action = () => new SpriteDocument(
            Guid.NewGuid(),
            "document",
            new SpriteResolution(2, 2),
            [source],
            [state]);

        action.Should().Throw<ArgumentException>().WithMessage("*outside source bounds*");
    }

    [Fact]
    public void AnimationShouldRejectNonPositiveDelay()
    {
        var action = () => new SpriteAnimationMetadata([1, 0.0]);

        action.Should().Throw<ArgumentException>().WithMessage("*finite positive*");
    }
}
