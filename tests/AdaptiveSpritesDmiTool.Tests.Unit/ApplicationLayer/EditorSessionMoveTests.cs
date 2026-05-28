using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Application;

/// <summary>
/// Regression tests for move-commit logic.
/// Simulates the exact transform that ApplyMovedEditableArea performs:
/// RemoveMapping at origin, then SetMappingForced at destination with the original target.
/// </summary>
public sealed class EditorSessionMoveTests
{
    private static EditorSession CreateSession()
    {
        var session = new EditorSession();
        session.LoadAsset(new DmiAssetInfo(
            "asset", null,
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four, []));
        session.CreateConfig("config",
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test"));
        return session;
    }

    [Fact]
    public void MoveTransformRemoveOldAndSetNewAtDestination()
    {
        // Arrange: config has PixelMapping(Source:(1,1), Target:(5,5)).
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(1, 1), new PixelCoordinate(5, 5));
        var deltaX = 2;
        var deltaY = 1;

        // Act: simulate ApplyMovedEditableArea logic.
        var result = session.ApplyTransform(config =>
        {
            var origin = new PixelCoordinate(1, 1);
            var destination = new PixelCoordinate(1 + deltaX, 1 + deltaY);
            return config
                .RemoveMapping(SpriteDirection.South, origin)
                .SetMappingForced(SpriteDirection.South, destination, new PixelCoordinate(5, 5));
        });

        result.IsSuccess.Should().BeTrue();

        var allMappings = result.Value.GetMappings(SpriteDirection.South);
        allMappings.Should().NotContain(m => m.Source == new PixelCoordinate(1, 1));
        allMappings.Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(3, 2) &&
            m.Target == new PixelCoordinate(5, 5));
        allMappings.Should().HaveCount(1);
    }

    [Fact]
    public void MoveTransformWithSetMappingLosesMappingWhenDestinationEqualsTarget()
    {
        // Regression: SetMapping removes mapping when target == source.
        // Move must use SetMappingForced to preserve the mapping.
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(0, 0), new PixelCoordinate(3, 3));
        var deltaX = 3;
        var deltaY = 3;

        // Act: use SetMapping, not SetMappingForced, to simulate the regression.
        var result = session.ApplyTransform(config =>
        {
            var origin = new PixelCoordinate(0, 0);
            var destination = new PixelCoordinate(0 + deltaX, 0 + deltaY);
            return config
                .RemoveMapping(SpriteDirection.South, origin)
                .SetMapping(SpriteDirection.South, destination, new PixelCoordinate(3, 3));
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.GetMappings(SpriteDirection.South).Should().BeEmpty();
    }

    [Fact]
    public void MoveTransformWithSetMappingForcedPreservesMappingWhenDestinationEqualsTarget()
    {
        // Regression: SetMappingForced must keep the mapping even when target == source.
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(0, 0), new PixelCoordinate(3, 3));
        var deltaX = 3;
        var deltaY = 3;

        // Act: use SetMappingForced for the correct move behavior.
        var result = session.ApplyTransform(config =>
        {
            var origin = new PixelCoordinate(0, 0);
            var destination = new PixelCoordinate(0 + deltaX, 0 + deltaY);
            return config
                .RemoveMapping(SpriteDirection.South, origin)
                .SetMappingForced(SpriteDirection.South, destination, new PixelCoordinate(3, 3));
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.GetMappings(SpriteDirection.South).Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(3, 3) &&
            m.Target == new PixelCoordinate(3, 3));
    }

    [Fact]
    public void MoveTransformSupportsUndo()
    {
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(0, 0), new PixelCoordinate(10, 10));
        var deltaX = 5;
        var deltaY = 5;

        var result = session.ApplyTransform(config =>
        {
            var origin = new PixelCoordinate(0, 0);
            var destination = new PixelCoordinate(0 + deltaX, 0 + deltaY);
            return config
                .RemoveMapping(SpriteDirection.South, origin)
                .SetMappingForced(SpriteDirection.South, destination, new PixelCoordinate(10, 10));
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.GetMappings(SpriteDirection.South).Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(5, 5));

        var undo = session.Undo();
        undo.IsSuccess.Should().BeTrue();
        undo.Value.GetMappings(SpriteDirection.South).Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(0, 0) &&
            m.Target == new PixelCoordinate(10, 10));

        var redo = session.Redo();
        redo.IsSuccess.Should().BeTrue();
        redo.Value.GetMappings(SpriteDirection.South).Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(5, 5));
    }

    [Fact]
    public void MoveTransformMultiplePixelsMovesAllIndependently()
    {
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(0, 0), new PixelCoordinate(10, 10));
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(1, 0), new PixelCoordinate(20, 20));

        var deltaX = 3;
        var deltaY = 2;

        var result = session.ApplyTransform(config =>
            config
                .RemoveMapping(SpriteDirection.South, new PixelCoordinate(0, 0))
                .RemoveMapping(SpriteDirection.South, new PixelCoordinate(1, 0))
                .SetMappingForced(SpriteDirection.South,
                    new PixelCoordinate(0 + deltaX, 0 + deltaY), new PixelCoordinate(10, 10))
                .SetMappingForced(SpriteDirection.South,
                    new PixelCoordinate(1 + deltaX, 0 + deltaY), new PixelCoordinate(20, 20)));

        result.IsSuccess.Should().BeTrue();
        var mappings = result.Value.GetMappings(SpriteDirection.South);
        mappings.Should().HaveCount(2);
        mappings.Should().Contain(m =>
            m.Source == new PixelCoordinate(3, 2) &&
            m.Target == new PixelCoordinate(10, 10));
        mappings.Should().Contain(m =>
            m.Source == new PixelCoordinate(4, 2) &&
            m.Target == new PixelCoordinate(20, 20));
    }
}
