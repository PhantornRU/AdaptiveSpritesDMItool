using System.IO;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

internal static class AppStoragePaths
{
    private static readonly string RootDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AdaptiveSpritesDmiTool");

    public static string SettingsFilePath => Path.Combine(RootDirectory, "workspace-settings.json");

    public static string LogFilePath => Path.Combine(RootDirectory, "logs", "adaptive-sprites.log");
}
