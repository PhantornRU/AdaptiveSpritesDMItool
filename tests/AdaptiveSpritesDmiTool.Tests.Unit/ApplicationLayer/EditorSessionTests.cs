using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Application;

public sealed class EditorSessionTests
{
    [Fact]
    public void CreateConfig_ShouldRequireLoadedAsset()
    {
        var session = new EditorSession();

        var result = session.CreateConfig("config", ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public void UndoRedo_ShouldRestorePreviousConfigSnapshots()
    {
        var session = new EditorSession();
        var asset = new DmiAssetInfo(
            "asset",
            "sample.dmi",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Four,
            []);

        session.LoadAsset(asset);
        session.CreateConfig("config", ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test"));

        var source = new PixelCoordinate(1, 1);
        session.UpsertMapping(SpriteDirection.South, source, new PixelCoordinate(2, 2));
        session.UpsertMapping(SpriteDirection.South, source, null);

        session.Undo().Value.IsTransparent(SpriteDirection.South, source).Should().BeFalse();
        session.Redo().Value.IsTransparent(SpriteDirection.South, source).Should().BeTrue();
    }

    [Fact]
    public void ApplyTransform_ShouldCaptureBulkEditAsSingleUndoStep()
    {
        var session = new EditorSession();
        var asset = new DmiAssetInfo(
            "asset",
            "sample.dmi",
            new SpriteResolution(4, 4),
            SupportedDirectionSet.Four,
            []);

        session.LoadAsset(asset);
        session.CreateConfig("config", ConfigMetadata.CreateNew(ConfigSource.UserCreated, "test"));

        var result = session.ApplyTransform(config =>
            config
                .SetMapping(SpriteDirection.South, new PixelCoordinate(0, 0), new PixelCoordinate(1, 1))
                .SetMapping(SpriteDirection.South, new PixelCoordinate(2, 2), null));

        result.IsSuccess.Should().BeTrue();
        result.Value.GetMappings(SpriteDirection.South).Should().HaveCount(2);

        var undo = session.Undo();
        undo.IsSuccess.Should().BeTrue();
        undo.Value.GetMappings(SpriteDirection.South).Should().BeEmpty();
    }

    [Fact]
    public void LoadAsset_ShouldCreateLoadedWorkspace()
    {
        var session = new EditorSession();
        var asset = new DmiAssetInfo(
            "asset",
            "sample.dmi",
            new SpriteResolution(32, 32),
            SupportedDirectionSet.Eight,
            []);

        session.LoadAsset(asset);

        session.Workspace.IsEmpty.Should().BeFalse();
        session.Workspace.LoadedDocumentPath.Should().Be("sample.dmi");
        session.SelectedDirection.Should().Be(SpriteDirection.South);
    }
}
