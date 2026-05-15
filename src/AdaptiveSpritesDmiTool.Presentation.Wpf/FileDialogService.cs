using Microsoft.Win32;
using System.IO;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public interface IFileDialogService
{
    string? OpenDmiFile(string? initialPath);

    string? OpenConfigFile(string? initialPath);

    string? SaveConfigFile(string? initialPath, string? configName);

    string? OpenLegacyCsvFile(string? initialPath);

    string? SelectDirectory(string description, string? initialPath);
}

public sealed class FileDialogService : IFileDialogService
{
    public string? OpenDmiFile(string? initialPath) =>
        ShowOpenFileDialog("DMI files (*.dmi)|*.dmi", initialPath);

    public string? OpenConfigFile(string? initialPath) =>
        ShowOpenFileDialog("JSON config files (*.json)|*.json", initialPath);

    public string? SaveConfigFile(string? initialPath, string? configName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON config files (*.json)|*.json",
            Title = "Save sprite config",
            AddExtension = true,
            DefaultExt = ".json",
            OverwritePrompt = true
        };

        ApplyInitialPath(dialog, initialPath, NormalizeFileName(configName, "sprite-config") + ".json");
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? OpenLegacyCsvFile(string? initialPath) =>
        ShowOpenFileDialog("Legacy CSV config files (*.csv)|*.csv", initialPath);

    public string? SelectDirectory(string description, string? initialPath)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            var normalized = Path.GetFullPath(initialPath);
            var directory = Directory.Exists(normalized)
                ? normalized
                : Path.GetDirectoryName(normalized);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                dialog.InitialDirectory = directory;
            }
        }

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }

    private static string? ShowOpenFileDialog(string filter, string? initialPath)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false
        };

        ApplyInitialPath(dialog, initialPath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void ApplyInitialPath(Microsoft.Win32.FileDialog dialog, string? initialPath, string? fallbackFileName = null)
    {
        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            var normalized = Path.GetFullPath(initialPath);
            var directory = Directory.Exists(normalized)
                ? normalized
                : Path.GetDirectoryName(normalized);

            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                dialog.InitialDirectory = directory;
            }

            if (File.Exists(normalized))
            {
                dialog.FileName = Path.GetFileName(normalized);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackFileName))
        {
            dialog.FileName = fallbackFileName;
        }
    }

    private static string NormalizeFileName(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var normalized = new string(value.Trim().Select(character => invalidCharacters.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
