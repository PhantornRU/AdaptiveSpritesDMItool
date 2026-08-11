using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.ApplicationLayer;

public sealed class EditorMutationTests
{
    public static IEnumerable<object[]> ProjectionMatrixCases()
    {
        foreach (var directionSet in new[] { SupportedDirectionSet.Four, SupportedDirectionSet.Eight })
        {
            foreach (var active in directionSet.GetDirections())
            {
                foreach (var scope in Enum.GetValues<DirectionPropagationScope>())
                {
                    yield return [directionSet, active, scope, false];
                    yield return [directionSet, active, scope, true];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ProjectionMatrixCases))]
    public void ProjectionShouldCoverDirectionScopeAndMirrorMatrix(
        SupportedDirectionSet directionSet,
        SpriteDirection active,
        DirectionPropagationScope scope,
        bool mirror)
    {
        var resolution = new SpriteResolution(4, 3);
        var coordinate = new PixelCoordinate(1, 2);
        var options = Options(resolution, directionSet, active, scope, mirror, offset: 0);

        var result = DirectionProjectionPolicy.Project(coordinate, options);

        result.IsSuccess.Should().BeTrue();
        var expectedDirections = scope switch
        {
            DirectionPropagationScope.ActiveOnly => [active],
            DirectionPropagationScope.Parallel =>
                new[] { active, DirectionProjectionPolicy.GetParallelDirection(active)!.Value },
            DirectionPropagationScope.All => directionSet.GetDirections().ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
        result.Value.Pixels.Select(static pixel => pixel.Direction).Should().BeEquivalentTo(expectedDirections);
        foreach (var pixel in result.Value.Pixels)
        {
            var expectedX = mirror && DirectionProjectionPolicy.HasOppositeOrientation(active, pixel.Direction)
                ? 2
                : 1;
            pixel.Coordinate.Should().Be(new PixelCoordinate(expectedX, 2));
        }

        result.Value.SkippedProjectionCount.Should().Be(0);
    }

    [Theory]
    [InlineData(SpriteDirection.South, SpriteDirection.North, 3)]
    [InlineData(SpriteDirection.East, SpriteDirection.West, 3)]
    [InlineData(SpriteDirection.SouthEast, SpriteDirection.NorthWest, 3)]
    [InlineData(SpriteDirection.SouthWest, SpriteDirection.NorthEast, 3)]
    public void ParallelProjectionShouldMirrorAcrossExactCenter(
        SpriteDirection active,
        SpriteDirection parallel,
        int expectedX)
    {
        var options = Options(
            new SpriteResolution(4, 3),
            SupportedDirectionSet.Eight,
            active,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset: 0);

        var result = DirectionProjectionPolicy.Project(new PixelCoordinate(0, 2), options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Pixels.Should().Contain(new ProjectedPixel(active, new PixelCoordinate(0, 2)));
        result.Value.Pixels.Should().Contain(new ProjectedPixel(parallel, new PixelCoordinate(expectedX, 2)));
        result.Value.SkippedProjectionCount.Should().Be(0);
    }

    [Fact]
    public void AllProjectionShouldUseOrientationParityWithoutFlippingY()
    {
        var options = Options(
            new SpriteResolution(5, 4),
            SupportedDirectionSet.Eight,
            SpriteDirection.North,
            DirectionPropagationScope.All,
            mirror: true,
            offset: 0);

        var result = DirectionProjectionPolicy.Project(new PixelCoordinate(1, 3), options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Pixels.Single(pixel => pixel.Direction == SpriteDirection.West).Coordinate
            .Should().Be(new PixelCoordinate(1, 3));
        result.Value.Pixels.Single(pixel => pixel.Direction == SpriteDirection.East).Coordinate
            .Should().Be(new PixelCoordinate(3, 3));
        result.Value.Pixels.Should().OnlyContain(static pixel => pixel.Coordinate.Y == 3);
    }

    [Fact]
    public void ShiftedAxisShouldUseConfiguredWholePixelOffset()
    {
        var options = Options(
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset: -1);

        var result = DirectionProjectionPolicy.Project(new PixelCoordinate(2, 7), options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Pixels.Single(pixel => pixel.Direction == SpriteDirection.North).Coordinate
            .Should().Be(new PixelCoordinate(27, 7));
    }

    [Fact]
    public void ShiftedAxisShouldSkipOutOfBoundsProjectionInsteadOfClamping()
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset: 1);

        var result = DirectionProjectionPolicy.Project(new PixelCoordinate(0, 1), options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Pixels.Should().Equal(new ProjectedPixel(SpriteDirection.South, new PixelCoordinate(0, 1)));
        result.Value.SkippedProjectionCount.Should().Be(1);
    }

    [Theory]
    [InlineData(5, -1, 0)]
    [InlineData(5, 1, 4)]
    [InlineData(6, -1, 1)]
    [InlineData(6, 1, 5)]
    public void ShiftedAxisShouldSupportOddEvenAndRectangularFrames(int width, int offset, int expectedX)
    {
        var options = Options(
            new SpriteResolution(width, 7),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset);

        var result = DirectionProjectionPolicy.Project(new PixelCoordinate(2, 6), options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Pixels.Single(pixel => pixel.Direction == SpriteDirection.North).Coordinate
            .Should().Be(new PixelCoordinate(expectedX, 6));
    }

    [Fact]
    public void InvalidMirrorAxisOffsetShouldRejectWholeProjection()
    {
        var result = DirectionProjectionPolicy.Project(
            new PixelCoordinate(0, 0),
            Options(
                new SpriteResolution(4, 3),
                SupportedDirectionSet.Four,
                SpriteDirection.South,
                DirectionPropagationScope.All,
                mirror: true,
                offset: 2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public void ShiftedProjectionShouldNeverClampDistinctCoordinatesOntoEdgePixel()
    {
        var options = Options(
            new SpriteResolution(4, 3),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset: 1);

        var projected = Enumerable.Range(0, 4)
            .Select(x => DirectionProjectionPolicy.Project(new PixelCoordinate(x, 0), options).Value)
            .SelectMany(static result => result.Pixels)
            .Where(static pixel => pixel.Direction == SpriteDirection.North)
            .Select(static pixel => pixel.Coordinate)
            .ToArray();

        projected.Should().OnlyHaveUniqueItems();
        projected.Should().Equal(new PixelCoordinate(3, 0), new PixelCoordinate(2, 0));
    }

    [Fact]
    public void MirrorDisabledShouldPreserveCoordinatesForEveryDirection()
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Eight,
            SpriteDirection.South,
            DirectionPropagationScope.All,
            mirror: false,
            offset: 1);

        var result = DirectionProjectionPolicy.Project(new PixelCoordinate(0, 1), options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Pixels.Should().HaveCount(8).And.OnlyContain(
            static pixel => pixel.Coordinate == new PixelCoordinate(0, 1));
    }

    [Fact]
    public void MappingProjectionShouldMirrorEditableAndSourceAcrossParallelDirections()
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset: 0);

        var result = DirectionProjectionPolicy.ProjectMapping(
            new PixelCoordinate(0, 1),
            new PixelCoordinate(2, 2),
            options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Mappings.Should().Equal(
            new ProjectedMapping(
                SpriteDirection.South,
                new PixelCoordinate(0, 1),
                new PixelCoordinate(2, 2)),
            new ProjectedMapping(
                SpriteDirection.North,
                new PixelCoordinate(3, 1),
                new PixelCoordinate(1, 2)));
        result.Value.SkippedProjectionCount.Should().Be(0);
    }

    [Fact]
    public void MappingProjectionShouldUseOrientationParityAcrossAllEightDirections()
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Eight,
            SpriteDirection.SouthEast,
            DirectionPropagationScope.All,
            mirror: true,
            offset: 0);

        var result = DirectionProjectionPolicy.ProjectMapping(
            new PixelCoordinate(0, 1),
            new PixelCoordinate(3, 2),
            options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Mappings.Should().HaveCount(8);
        foreach (var mapping in result.Value.Mappings)
        {
            var shouldMirror = DirectionProjectionPolicy.HasOppositeOrientation(
                SpriteDirection.SouthEast,
                mapping.Direction);
            mapping.EditableCoordinate.Should().Be(
                shouldMirror ? new PixelCoordinate(3, 1) : new PixelCoordinate(0, 1));
            mapping.SourceCoordinate.Should().Be(
                shouldMirror ? new PixelCoordinate(0, 2) : new PixelCoordinate(3, 2));
        }
    }

    [Theory]
    [InlineData(-1, 0, 1, 1, 0)]
    [InlineData(1, 3, 2, 2, 3)]
    public void MappingProjectionShouldApplyShiftedAxisToBothEndpoints(
        int offset,
        int editableX,
        int sourceX,
        int expectedEditableX,
        int expectedSourceX)
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset);

        var result = DirectionProjectionPolicy.ProjectMapping(
            new PixelCoordinate(editableX, 1),
            new PixelCoordinate(sourceX, 2),
            options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Mappings.Single(mapping => mapping.Direction == SpriteDirection.North)
            .Should().Be(
                new ProjectedMapping(
                    SpriteDirection.North,
                    new PixelCoordinate(expectedEditableX, 1),
                    new PixelCoordinate(expectedSourceX, 2)));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    public void MappingProjectionShouldSkipDirectionOnceWhenEitherEndpointIsOutOfBounds(
        int editableX,
        int sourceX)
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset: 1);

        var result = DirectionProjectionPolicy.ProjectMapping(
            new PixelCoordinate(editableX, 1),
            new PixelCoordinate(sourceX, 2),
            options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Mappings.Should().Equal(
            new ProjectedMapping(
                SpriteDirection.South,
                new PixelCoordinate(editableX, 1),
                new PixelCoordinate(sourceX, 2)));
        result.Value.SkippedProjectionCount.Should().Be(1);
    }

    [Fact]
    public void MappingProjectionShouldPreserveBothEndpointsWhenMirrorIsDisabled()
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Eight,
            SpriteDirection.South,
            DirectionPropagationScope.All,
            mirror: false,
            offset: 1);

        var result = DirectionProjectionPolicy.ProjectMapping(
            new PixelCoordinate(0, 1),
            new PixelCoordinate(2, 3),
            options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Mappings.Should().HaveCount(8).And.OnlyContain(mapping =>
            mapping.EditableCoordinate == new PixelCoordinate(0, 1) &&
            mapping.SourceCoordinate == new PixelCoordinate(2, 3));
    }

    [Fact]
    public void MappingProjectionShouldPreserveBothEndpointsForActiveOnlyScope()
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            SpriteDirection.North,
            DirectionPropagationScope.ActiveOnly,
            mirror: true,
            offset: -1);

        var result = DirectionProjectionPolicy.ProjectMapping(
            new PixelCoordinate(3, 1),
            new PixelCoordinate(1, 2),
            options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Mappings.Should().Equal(
            new ProjectedMapping(
                SpriteDirection.North,
                new PixelCoordinate(3, 1),
                new PixelCoordinate(1, 2)));
    }

    [Fact]
    public void SetSourceStrokeShouldUseProjectedMappingEndpoints()
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset: 0);

        var result = EditorMutationPlanFactory.CreateStroke(
            [new PixelCoordinate(0, 1)],
            EditorMappingMutationKind.SetSource,
            new PixelCoordinate(2, 2),
            options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Operations.Should().Equal(
            new EditorMappingMutation(
                EditorMappingMutationKind.SetSource,
                SpriteDirection.South,
                new PixelCoordinate(0, 1),
                new PixelCoordinate(2, 2)),
            new EditorMappingMutation(
                EditorMappingMutationKind.SetSource,
                SpriteDirection.North,
                new PixelCoordinate(3, 1),
                new PixelCoordinate(1, 2)));
    }

    [Theory]
    [InlineData(EditorMappingMutationKind.Restore)]
    [InlineData(EditorMappingMutationKind.SetTransparent)]
    public void TargetOnlyStrokeShouldKeepExistingProjectionSemantics(EditorMappingMutationKind kind)
    {
        var options = Options(
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            SpriteDirection.South,
            DirectionPropagationScope.Parallel,
            mirror: true,
            offset: 0);

        var result = EditorMutationPlanFactory.CreateStroke(
            [new PixelCoordinate(0, 1)],
            kind,
            null,
            options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Operations.Should().Equal(
            new EditorMappingMutation(kind, SpriteDirection.South, new PixelCoordinate(0, 1)),
            new EditorMappingMutation(kind, SpriteDirection.North, new PixelCoordinate(3, 1)));
        result.Value.Operations.Should().OnlyContain(static operation => operation.SourceCoordinate == null);
    }

    [Fact]
    public void StrokeInterpolatorShouldFillSparseHorizontalAndDiagonalSamples()
    {
        PixelStrokeInterpolator.Interpolate(
                [new PixelCoordinate(0, 0), new PixelCoordinate(4, 0), new PixelCoordinate(6, 2)])
            .Should().Equal(
                new PixelCoordinate(0, 0),
                new PixelCoordinate(1, 0),
                new PixelCoordinate(2, 0),
                new PixelCoordinate(3, 0),
                new PixelCoordinate(4, 0),
                new PixelCoordinate(5, 1),
                new PixelCoordinate(6, 2));
    }

    [Fact]
    public void ApplyMutationShouldCommitTransparentStrokeAsOneUndoStep()
    {
        var session = CreateSession(new SpriteResolution(4, 4));
        var planResult = EditorMutationPlanFactory.CreateStroke(
            PixelStrokeInterpolator.InterpolateSegment(new PixelCoordinate(0, 0), new PixelCoordinate(3, 0)),
            EditorMappingMutationKind.SetTransparent,
            null,
            Options(
                new SpriteResolution(4, 4),
                SupportedDirectionSet.Four,
                SpriteDirection.South,
                DirectionPropagationScope.ActiveOnly,
                mirror: true,
                offset: 0));

        var result = session.ApplyMutation(planResult.Value);

        result.IsSuccess.Should().BeTrue();
        result.Value.AppliedOperationCount.Should().Be(4);
        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().HaveCount(4);
        session.Undo().IsSuccess.Should().BeTrue();
        session.CurrentConfig.GetMappings(SpriteDirection.South).Should().BeEmpty();
        session.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void RestoreShouldRemoveExplicitTransparentMapping()
    {
        var session = CreateSession(new SpriteResolution(2, 2));
        var coordinate = new PixelCoordinate(1, 1);
        session.ApplyMutation(
            new EditorMutationPlan(
                [new EditorMappingMutation(EditorMappingMutationKind.SetTransparent, SpriteDirection.South, coordinate)]));

        var result = session.ApplyMutation(
            new EditorMutationPlan(
                [new EditorMappingMutation(EditorMappingMutationKind.Restore, SpriteDirection.South, coordinate)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Config.IsTransparent(SpriteDirection.South, coordinate).Should().BeFalse();
        result.Value.Config.GetEffectiveTarget(SpriteDirection.South, coordinate).Should().Be(coordinate);
    }

    [Fact]
    public void ConflictingWritesShouldLeaveSessionAndHistoryUntouched()
    {
        var session = CreateSession(new SpriteResolution(3, 3));
        var editable = new PixelCoordinate(0, 0);
        var result = session.ApplyMutation(
            new EditorMutationPlan(
            [
                new EditorMappingMutation(
                    EditorMappingMutationKind.SetSource,
                    SpriteDirection.South,
                    editable,
                    new PixelCoordinate(1, 1)),
                new EditorMappingMutation(
                    EditorMappingMutationKind.SetSource,
                    SpriteDirection.South,
                    editable,
                    new PixelCoordinate(2, 2))
            ]));

        result.IsFailure.Should().BeTrue();
        session.CurrentConfig!.GetMappings(SpriteDirection.South).Should().BeEmpty();
        session.CanUndo.Should().BeFalse();
        session.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void SemanticNoOpShouldNotCreateUndoEntry()
    {
        var session = CreateSession(new SpriteResolution(2, 2));
        var coordinate = new PixelCoordinate(1, 1);

        var result = session.ApplyMutation(
            new EditorMutationPlan(
                [new EditorMappingMutation(EditorMappingMutationKind.Restore, SpriteDirection.South, coordinate)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsChanged.Should().BeFalse();
        session.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void CleanSavedConfigSnapshotShouldRemainCleanAfterRestore()
    {
        var session = CreateSession(new SpriteResolution(4, 4));
        session.SetCurrentConfigPath("config.json").IsSuccess.Should().BeTrue();
        var snapshot = session.CaptureCurrentConfigSnapshot().Value;

        session.CreateConfig(
            "other",
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "tests"));

        session.RestoreCurrentConfigSnapshot(snapshot).IsSuccess.Should().BeTrue();

        session.CurrentConfigPath.Should().Be("config.json");
        session.CurrentConfig!.Name.Should().Be("config");
        session.IsDirty.Should().BeFalse();
        session.CanUndo.Should().BeFalse();
        session.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void DirtySavedConfigSnapshotShouldRestoreOriginalBaseline()
    {
        var session = CreateSession(new SpriteResolution(4, 4));
        var editable = new PixelCoordinate(1, 1);
        var target = new PixelCoordinate(2, 2);
        session.SetCurrentConfigPath("config.json").IsSuccess.Should().BeTrue();
        session.UpsertMapping(SpriteDirection.South, editable, target).IsSuccess.Should().BeTrue();
        var snapshot = session.CaptureCurrentConfigSnapshot().Value;

        session.CreateConfig(
            "other",
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "tests"));

        session.RestoreCurrentConfigSnapshot(snapshot).IsSuccess.Should().BeTrue();

        session.CurrentConfigPath.Should().Be("config.json");
        session.CurrentConfig!.GetEffectiveTarget(SpriteDirection.South, editable).Should().Be(target);
        session.IsDirty.Should().BeTrue();
        session.CanUndo.Should().BeFalse();
        session.CanRedo.Should().BeFalse();

        session.RemoveMapping(SpriteDirection.South, editable).IsSuccess.Should().BeTrue();
        session.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void UnsavedConfigSnapshotShouldRemainDirtyAfterRestore()
    {
        var session = CreateSession(new SpriteResolution(4, 4));
        var snapshot = session.CaptureCurrentConfigSnapshot().Value;
        session.SetCurrentConfigPath("other.json").IsSuccess.Should().BeTrue();

        session.RestoreCurrentConfigSnapshot(snapshot).IsSuccess.Should().BeTrue();

        session.CurrentConfigPath.Should().BeNull();
        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void EditorSettingsChangeShouldMarkSavedConfigDirtyAndUndoShouldRestoreCleanState()
    {
        var session = CreateSession(new SpriteResolution(5, 3));
        session.SetCurrentConfigPath("config.json").IsSuccess.Should().BeTrue();

        var result = session.ApplyTransform(config =>
            config.WithEditorSettings(new SpriteEditorSettings(1)));

        result.IsSuccess.Should().BeTrue();
        session.IsDirty.Should().BeTrue();
        session.Undo().IsSuccess.Should().BeTrue();
        session.IsDirty.Should().BeFalse();
    }

    private static DirectionProjectionOptions Options(
        SpriteResolution resolution,
        SupportedDirectionSet directions,
        SpriteDirection active,
        DirectionPropagationScope scope,
        bool mirror,
        int offset) =>
        new(resolution, directions, active, scope, mirror, offset);

    private static EditorSession CreateSession(SpriteResolution resolution)
    {
        var session = new EditorSession();
        session.LoadAsset(
            new DmiAssetInfo(
                "sprite",
                "sprite.dmi",
                resolution,
                SupportedDirectionSet.Four,
                [new DmiStateInfo("base", 1)]));
        session.CreateConfig(
            "config",
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "tests"));
        return session;
    }
}
