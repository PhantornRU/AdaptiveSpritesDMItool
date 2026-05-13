using AdaptiveSpritesDmiTool.Presentation.Wpf;
using FluentAssertions;

namespace AdaptiveSpritesDmiTool.Tests.Unit.Presentation;

public sealed class WorkspaceEnumParsingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("NotATab")]
    public void ParseDefinedEnumOrDefaultShouldRejectInvalidBottomWorkspaceTab(string? value)
    {
        var parsed = WorkspaceEnumParsing.ParseDefinedEnumOrDefault(
            value,
            BottomWorkspaceTab.Mappings);

        parsed.Should().Be(BottomWorkspaceTab.Mappings);
    }

    [Theory]
    [InlineData("Mappings", BottomWorkspaceTab.Mappings)]
    [InlineData("mappings", BottomWorkspaceTab.Mappings)]
    [InlineData("Advanced", BottomWorkspaceTab.Advanced)]
    public void ParseDefinedEnumOrDefaultShouldAcceptNamedBottomWorkspaceTabValues(
        string value,
        BottomWorkspaceTab expected)
    {
        var parsed = WorkspaceEnumParsing.ParseDefinedEnumOrDefault(
            value,
            BottomWorkspaceTab.Mappings);

        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("NotATheme")]
    public void ParseDefinedEnumOrDefaultShouldRejectInvalidThemeMode(string? value)
    {
        var parsed = WorkspaceEnumParsing.ParseDefinedEnumOrDefault(
            value,
            WorkspaceThemeMode.Dark);

        parsed.Should().Be(WorkspaceThemeMode.Dark);
    }

    [Theory]
    [InlineData("Dark", WorkspaceThemeMode.Dark)]
    [InlineData("dark", WorkspaceThemeMode.Dark)]
    [InlineData("Light", WorkspaceThemeMode.Light)]
    [InlineData("Warm", WorkspaceThemeMode.Warm)]
    public void ParseDefinedEnumOrDefaultShouldAcceptNamedThemeModeValues(
        string value,
        WorkspaceThemeMode expected)
    {
        var parsed = WorkspaceEnumParsing.ParseDefinedEnumOrDefault(
            value,
            WorkspaceThemeMode.Dark);

        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("NotAViewport")]
    public void ParseDefinedEnumOrDefaultShouldRejectInvalidEditorViewportMode(string? value)
    {
        var parsed = WorkspaceEnumParsing.ParseDefinedEnumOrDefault(
            value,
            EditorViewportMode.Matrix);

        parsed.Should().Be(EditorViewportMode.Matrix);
    }

    [Theory]
    [InlineData("Matrix", EditorViewportMode.Matrix)]
    [InlineData("matrix", EditorViewportMode.Matrix)]
    [InlineData("Focused", EditorViewportMode.Focused)]
    public void ParseDefinedEnumOrDefaultShouldAcceptNamedEditorViewportModeValues(
        string value,
        EditorViewportMode expected)
    {
        var parsed = WorkspaceEnumParsing.ParseDefinedEnumOrDefault(
            value,
            EditorViewportMode.Matrix);

        parsed.Should().Be(expected);
    }
}
