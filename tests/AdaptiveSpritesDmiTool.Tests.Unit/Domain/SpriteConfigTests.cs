using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Workspaces;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Domain;

public sealed class SpriteConfigTests
{
    [Fact]
    public void ValidateCandidate_ShouldRejectOutOfBoundsMappings()
    {
        var resolution = new SpriteResolution(32, 32);

        var validation = SpriteConfig.ValidateCandidate(
            resolution,
            SupportedDirectionSet.Four,
            new Dictionary<SpriteDirection, IReadOnlyCollection<PixelMapping>>
            {
                [SpriteDirection.South] =
                [
                    new PixelMapping(new PixelCoordinate(0, 0), null),
                    new PixelMapping(new PixelCoordinate(31, 31), null)
                ],
                [SpriteDirection.North] = [],
                [SpriteDirection.East] = [],
                [SpriteDirection.West] = []
            });

        var invalidValidation = SpriteConfig.ValidateCandidate(
            resolution,
            SupportedDirectionSet.Four,
            new Dictionary<SpriteDirection, IReadOnlyCollection<PixelMapping>>
            {
                [SpriteDirection.South] =
                [
                    new PixelMapping(new PixelCoordinate(31, 31), new PixelCoordinate(40, 0))
                ],
                [SpriteDirection.North] = [],
                [SpriteDirection.East] = [],
                [SpriteDirection.West] = []
            });

        validation.IsValid.Should().BeTrue();
        invalidValidation.IsValid.Should().BeFalse();
        invalidValidation.Errors.Should().Contain(issue => issue.Code == "target-out-of-bounds");
    }

    [Fact]
    public void SetMapping_ShouldRemoveIdentityMappings()
    {
        var config = SpriteConfig.CreateEmpty(
            "config",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test", utcNow: DateTimeOffset.UtcNow));

        var updated = config.SetMapping(SpriteDirection.South, new PixelCoordinate(2, 2), new PixelCoordinate(2, 2));

        updated.GetMappings(SpriteDirection.South).Should().BeEmpty();
    }

    [Fact]
    public void SetMapping_ShouldRejectUnsupportedDirections()
    {
        var config = SpriteConfig.CreateEmpty(
            "config",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test", utcNow: DateTimeOffset.UtcNow));

        var action = () => config.SetMapping(SpriteDirection.NorthEast, new PixelCoordinate(1, 1), null);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ValidateCompatibilityShouldRejectResolutionMismatch()
    {
        var config = SpriteConfig.CreateEmpty(
            "config",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test", utcNow: DateTimeOffset.UtcNow));

        var validation = config.ValidateCompatibility(new SpriteResolution(64, 32), SupportedDirectionSet.Four);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle(issue => issue.Code == "config.compatibility.resolution");
    }

    [Fact]
    public void ValidateCompatibilityShouldRejectDirectionSetMismatch()
    {
        var config = SpriteConfig.CreateEmpty(
            "config",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test", utcNow: DateTimeOffset.UtcNow));

        var validation = config.ValidateCompatibility(new SpriteResolution(32, 32), SupportedDirectionSet.Eight);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle(issue => issue.Code == "config.compatibility.direction_set");
    }

    [Fact]
    public void ValidateShouldRejectMetadataWhereUpdatedPrecedesCreated()
    {
        var created = DateTimeOffset.UtcNow;
        var invalidMetadata = new ConfigMetadata(
            created,
            created.AddMinutes(-1),
            ConfigSource.UserCreated,
            "test",
            null);

        var config = SpriteConfig.CreateEmpty(
            "config",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            invalidMetadata);

        var validation = config.Validate();

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle(issue => issue.Code == "config.metadata.invalid");
    }

    [Fact]
    public void ValidateCandidateShouldRejectUnsupportedDirections()
    {
        var validation = SpriteConfig.ValidateCandidate(
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            new Dictionary<SpriteDirection, IReadOnlyCollection<PixelMapping>>
            {
                [SpriteDirection.NorthEast] =
                [
                    new PixelMapping(new PixelCoordinate(1, 1), new PixelCoordinate(2, 2))
                ]
            });

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle(issue => issue.Code == "unsupported-direction");
    }

    [Fact]
    public void EmptyWorkspace_ShouldBeMarkedAsEmpty()
    {
        WorkspaceState.Empty.IsEmpty.Should().BeTrue();
        WorkspaceState.Empty.LoadedDocumentPath.Should().BeNull();
    }
}