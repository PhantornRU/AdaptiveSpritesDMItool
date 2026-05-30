using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Infrastructure.Settings;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Integration.Settings;

public sealed class JsonWorkspaceSettingsRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AdaptiveSpritesDmiTool.SettingsTests", Guid.NewGuid().ToString("N"));

    public JsonWorkspaceSettingsRepositoryIntegrationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task RepositoryShouldRoundTripAllWorkspaceSettingsFields()
    {
        var path = Path.Combine(_tempDirectory, "workspace.json");
        var repository = new JsonWorkspaceSettingsRepository(path);
        var settings = new WorkspaceSettings(
            "sprite.dmi",
            "config.json",
            "legacy.csv",
            "input",
            "output",
            "draft",
            "base",
            "landmark",
            "overlay",
            SpriteDirection.NorthEast,
            OverwritePolicy.FailIfExists,
            null,
            "Focused",
            "Mappings",
            false,
            false,
            "Russian",
            ImportedStates:
            [
                new WorkspaceImportedStateSettings(
                    "human32x",
                    "default.dmi",
                    "default.dmi",
                    IsSourceAssigned: true,
                    IsEditableAssigned: false,
                    PlacementMode: "Background",
                    Order: 0,
                    OpacityPercent: 75),
                new WorkspaceImportedStateSettings(
                    "coatDefault",
                    "clothes.dmi",
                    "clothes.dmi",
                    IsSourceAssigned: false,
                    IsEditableAssigned: true,
                    PlacementMode: "Overlay",
                    Order: 3,
                    OpacityPercent: 20)
            ]);

        (await repository.SaveAsync(settings, CancellationToken.None)).IsSuccess.Should().BeTrue();

        var loadResult = await repository.LoadAsync(CancellationToken.None);

        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value.Should().BeEquivalentTo(settings);
    }

    [Fact]
    public async Task RepositoryShouldLoadVersionOneDocumentsAndDefaultMissingFields()
    {
        var path = Path.Combine(_tempDirectory, "version1.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 1,
              "lastOpenedDmiPath": "sprite.dmi",
              "lastOpenedConfigPath": "config.json",
              "lastImportedLegacyCsvPath": "legacy.csv",
              "lastInputDirectory": "input",
              "lastOutputDirectory": "output",
              "lastDraftConfigName": "draft",
              "lastBaseState": "base",
              "lastLandmarkState": "landmark",
              "lastOverlayState": "overlay",
              "lastSelectedDirection": "North",
              "lastOverwritePolicy": "SkipExisting"
            }
            """);

        var repository = new JsonWorkspaceSettingsRepository(path);

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LastEditorViewportMode.Should().BeNull();
        result.Value.LastBottomWorkspaceTab.Should().BeNull();
        result.Value.LastUiLanguage.Should().BeNull();
        result.Value.IsPreviewInspectorExpanded.Should().BeTrue();
        result.Value.IsBottomWorkspaceExpanded.Should().BeTrue();
        result.Value.ImportedStates.Should().NotBeNull();
        result.Value.ImportedStates!.Should().BeEmpty();
    }

    [Fact]
    public async Task RepositoryShouldRejectUnsupportedImportedStatePlacementMode()
    {
        var path = Path.Combine(_tempDirectory, "invalid-imported-state-placement.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 5,
              "lastOverwritePolicy": "SkipExisting",
              "importedStates": [
                {
                  "stateName": "human32x",
                  "sourcePath": "default.dmi",
                  "sourceFileLabel": "default.dmi",
                  "isSourceAssigned": true,
                  "isEditableAssigned": false,
                  "placementMode": "Middle",
                  "order": 0
                }
              ]
            }
            """);

        var repository = new JsonWorkspaceSettingsRepository(path);

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Unsupported imported state placement mode");
    }

    [Fact]
    public async Task RepositoryShouldLoadVersionFiveImportedStatesWithDefaultOpacity()
    {
        var path = Path.Combine(_tempDirectory, "version5-imported-state.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 5,
              "lastOverwritePolicy": "SkipExisting",
              "importedStates": [
                {
                  "stateName": "human32x",
                  "sourcePath": "default.dmi",
                  "sourceFileLabel": "default.dmi",
                  "isSourceAssigned": true,
                  "isEditableAssigned": false,
                  "placementMode": "Overlay",
                  "order": 0
                }
              ]
            }
            """);

        var repository = new JsonWorkspaceSettingsRepository(path);

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedStates.Should().ContainSingle();
        result.Value.ImportedStates![0].OpacityPercent.Should().Be(100);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task RepositoryShouldRejectOutOfRangeImportedStateOpacity(int opacityPercent)
    {
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(
            path,
            $$"""
            {
              "version": 6,
              "lastOverwritePolicy": "SkipExisting",
              "importedStates": [
                {
                  "stateName": "human32x",
                  "sourcePath": "default.dmi",
                  "sourceFileLabel": "default.dmi",
                  "isSourceAssigned": true,
                  "isEditableAssigned": false,
                  "placementMode": "Overlay",
                  "order": 0,
                  "opacityPercent": {{opacityPercent}}
                }
              ]
            }
            """);

        var repository = new JsonWorkspaceSettingsRepository(path);

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Imported state opacity");
    }

    [Fact]
    public async Task RepositoryShouldRejectUnsupportedVersion()
    {
        var path = Path.Combine(_tempDirectory, "invalid-version.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": 99,
              "lastOpenedDmiPath": "sprite.dmi",
              "lastOpenedConfigPath": "config.json",
              "lastImportedLegacyCsvPath": "legacy.csv",
              "lastInputDirectory": "input",
              "lastOutputDirectory": "output",
              "lastDraftConfigName": "draft",
              "lastBaseState": "base",
              "lastLandmarkState": "landmark",
              "lastOverlayState": "overlay",
              "lastSelectedDirection": "North",
              "lastOverwritePolicy": "SkipExisting",
              "lastEditorViewportMode": "Matrix",
              "lastBottomWorkspaceTab": "Assets",
              "isPreviewInspectorExpanded": true
            }
            """);

        var repository = new JsonWorkspaceSettingsRepository(path);

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Unsupported workspace settings version");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("999")]
    [InlineData("DeleteExisting")]
    public async Task RepositoryShouldRejectNumericOrUndefinedOverwritePolicy(string overwritePolicy)
    {
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(
            path,
            $$"""
            {
              "version": 3,
              "lastOverwritePolicy": "{{overwritePolicy}}"
            }
            """);

        var repository = new JsonWorkspaceSettingsRepository(path);

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Unsupported overwrite policy");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("999")]
    [InlineData("Sideways")]
    public async Task RepositoryShouldRejectNumericOrUndefinedSelectedDirection(string selectedDirection)
    {
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(
            path,
            $$"""
            {
              "version": 3,
              "lastSelectedDirection": "{{selectedDirection}}",
              "lastOverwritePolicy": "SkipExisting"
            }
            """);

        var repository = new JsonWorkspaceSettingsRepository(path);

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        result.Error.Message.Should().Contain("Unsupported sprite direction");
    }

    [Fact]
    public async Task RepositoryShouldRejectMissingSettingsFile()
    {
        var repository = new JsonWorkspaceSettingsRepository(Path.Combine(_tempDirectory, "missing.json"));

        var result = await repository.LoadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not-found");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
