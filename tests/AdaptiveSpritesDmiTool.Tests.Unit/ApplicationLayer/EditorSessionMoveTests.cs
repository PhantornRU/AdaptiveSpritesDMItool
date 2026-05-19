using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Application;

/// <summary>
/// Regression tests for move-commit logic.
/// Simulates the exact transform that ApplyMovedEditableArea performs:
/// RemoveMapping at Origin → SetMappingForced at Destination with original Target.
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
    public void MoveTransform_RemoveOld_SetNewAtDestination()
    {
        // Arrange: config has PixelMapping(Source:(1,1), Target:(5,5))
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(1, 1), new PixelCoordinate(5, 5));
        var deltaX = 2;
        var deltaY = 1;

        // Act: simulate ApplyMovedEditableArea logic
        var result = session.ApplyTransform(config =>
        {
            var origin = new PixelCoordinate(1, 1);
            var destination = new PixelCoordinate(1 + deltaX, 1 + deltaY);
            return config
                .RemoveMapping(SpriteDirection.South, origin)
                .SetMappingForced(SpriteDirection.South, destination, new PixelCoordinate(5, 5));
        });

        // Assert: commit succeeded
        result.IsSuccess.Should().BeTrue();

        // Old mapping at (1,1) must be gone
        var allMappings = result.Value.GetMappings(SpriteDirection.South);
        allMappings.Should().NotContain(m => m.Source == new PixelCoordinate(1, 1));

        // New mapping at (3,2) must exist with correct Target
        allMappings.Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(3, 2) &&
            m.Target == new PixelCoordinate(5, 5));

        // Total: exactly 1 mapping
        allMappings.Should().HaveCount(1);
    }

    [Fact]
    public void MoveTransform_WithSetMapping_ShouldLoseMappingWhenDestinationEqualsTarget()
    {
        // Regression: SetMapping removes mapping when target == source (identity/no-op).
        // Move MUST use SetMappingForced to preserve the mapping.
        // Arrange: pixel at (0,0) maps to palette (3,3)
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(0, 0), new PixelCoordinate(3, 3));
        // delta (3,3) makes Destination == (3,3) == Target
        var deltaX = 3;
        var deltaY = 3;

        // Act: use SetMapping (NOT Forced) — simulates the BUG
        var result = session.ApplyTransform(config =>
        {
            var origin = new PixelCoordinate(0, 0);
            var destination = new PixelCoordinate(0 + deltaX, 0 + deltaY);
            return config
                .RemoveMapping(SpriteDirection.South, origin)
                .SetMapping(SpriteDirection.South, destination, new PixelCoordinate(3, 3));
        });

        // Assert: mapping was LOST because SetMapping's identity check
        // removed it (target(3,3) == source(3,3) parameter)
        result.IsSuccess.Should().BeTrue();
        result.Value.GetMappings(SpriteDirection.South).Should().BeEmpty();
    }

    [Fact]
    public void MoveTransform_WithSetMappingForced_ShouldPreserveMappingWhenDestinationEqualsTarget()
    {
        // Regression: SetMappingForced must keep the mapping even when target == source.
        // Arrange: pixel at (0,0) maps to palette (3,3)
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(0, 0), new PixelCoordinate(3, 3));
        // delta (3,3) makes Destination == (3,3) == Target
        var deltaX = 3;
        var deltaY = 3;

        // Act: use SetMappingForced — correct behavior
        var result = session.ApplyTransform(config =>
        {
            var origin = new PixelCoordinate(0, 0);
            var destination = new PixelCoordinate(0 + deltaX, 0 + deltaY);
            return config
                .RemoveMapping(SpriteDirection.South, origin)
                .SetMappingForced(SpriteDirection.South, destination, new PixelCoordinate(3, 3));
        });

        // Assert: mapping is preserved at destination
        result.IsSuccess.Should().BeTrue();
        result.Value.GetMappings(SpriteDirection.South).Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(3, 3) &&
            m.Target == new PixelCoordinate(3, 3));
    }

    [Fact]
    public void MoveTransform_ShouldSupportUndo()
    {
        // Arrange
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(0, 0), new PixelCoordinate(10, 10));
        var deltaX = 5;
        var deltaY = 5;

        // Act: apply move
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

        // Undo should restore original mapping at (0,0)
        var undo = session.Undo();
        undo.IsSuccess.Should().BeTrue();
        undo.Value.GetMappings(SpriteDirection.South).Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(0, 0) &&
            m.Target == new PixelCoordinate(10, 10));

        // Redo should re-apply the move
        var redo = session.Redo();
        redo.IsSuccess.Should().BeTrue();
        redo.Value.GetMappings(SpriteDirection.South).Should().ContainSingle(m =>
            m.Source == new PixelCoordinate(5, 5));
    }

    [Fact]
    public void MoveTransform_MultiplePixels_ShouldMoveAllIndependently()
    {
        // Arrange: config has two mappings
        var session = CreateSession();
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(0, 0), new PixelCoordinate(10, 10));
        session.UpsertMapping(SpriteDirection.South,
            new PixelCoordinate(1, 0), new PixelCoordinate(20, 20));

        var deltaX = 3;
        var deltaY = 2;

        // Act: move both pixels
        var result = session.ApplyTransform(config =>
            config
                .RemoveMapping(SpriteDirection.South, new PixelCoordinate(0, 0))
                .RemoveMapping(SpriteDirection.South, new PixelCoordinate(1, 0))
                .SetMappingForced(SpriteDirection.South,
                    new PixelCoordinate(0 + deltaX, 0 + deltaY), new PixelCoordinate(10, 10))
                .SetMappingForced(SpriteDirection.South,
                    new PixelCoordinate(1 + deltaX, 0 + deltaY), new PixelCoordinate(20, 20)));

        // Assert
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
