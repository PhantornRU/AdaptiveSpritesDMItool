using Microsoft.Win32;
using System.IO;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Documents;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public enum SpriteSourceChangeChoice
{
    Cancel = 0,
    AcceptNewFingerprint = 1,
    Relink = 2
}

public interface IFileDialogService
{
    string? OpenDmiFile(string? initialPath);

    string? OpenConfigFile(string? initialPath);

    string? SaveConfigFile(string? initialPath, string? configName);

    string? OpenLegacyCsvFile(string? initialPath);

    string? SelectDirectory(string description, string? initialPath);

    int? ConfigureMirrorAxisOffset(int currentOffset, int maximumOffset) => null;

    string? OpenSpriteDocumentFile(string? initialPath) => null;

    IReadOnlyList<string> OpenPngFiles(string? initialPath) => [];

    string? SaveSpriteDocumentProject(string? initialPath, string? documentName) => null;

    string? SaveDmiDocument(string? initialPath, string? documentName) => null;

    SpriteSheetSlicingRecipe? ConfigureSpriteSheet(SpriteImage preview, string stateName) => null;

    SpriteSourceChangeChoice ResolveSpriteSourceChange(SpriteDocumentSourceIssue issue) =>
        SpriteSourceChangeChoice.Cancel;

    string? RelinkSpriteSource(SpriteDocumentSourceIssue issue, string? initialPath) => null;
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

    public int? ConfigureMirrorAxisOffset(int currentOffset, int maximumOffset)
    {
        var dialog = new MirrorAxisOffsetDialog(currentOffset, maximumOffset)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true ? dialog.SelectedOffset : null;
    }

    public string? OpenSpriteDocumentFile(string? initialPath) =>
        ShowOpenFileDialog(
            "Sprite documents (*.dmi;*.adaptive-dmi.json)|*.dmi;*.adaptive-dmi.json|DMI files (*.dmi)|*.dmi|Adaptive DMI projects (*.adaptive-dmi.json)|*.adaptive-dmi.json|PNG files for content probing (*.png)|*.png|All files (*.*)|*.*",
            initialPath);

    public IReadOnlyList<string> OpenPngFiles(string? initialPath)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true,
            Title = "Import PNG graphics"
        };
        ApplyInitialPath(dialog, initialPath);
        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    public string? SaveSpriteDocumentProject(string? initialPath, string? documentName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Adaptive DMI projects (*.adaptive-dmi.json)|*.adaptive-dmi.json",
            Title = "Save sprite document project",
            AddExtension = true,
            DefaultExt = ".adaptive-dmi.json",
            OverwritePrompt = true
        };
        ApplyInitialPath(dialog, initialPath, NormalizeFileName(documentName, "sprite") + ".adaptive-dmi.json");
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveDmiDocument(string? initialPath, string? documentName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "DMI files (*.dmi)|*.dmi",
            Title = "Export DMI",
            AddExtension = true,
            DefaultExt = ".dmi",
            OverwritePrompt = true
        };
        ApplyInitialPath(dialog, initialPath, NormalizeFileName(documentName, "sprite") + ".dmi");
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public SpriteSheetSlicingRecipe? ConfigureSpriteSheet(SpriteImage preview, string stateName)
    {
        var dialog = new SpriteSheetImportDialog(preview, stateName)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        return dialog.ShowDialog() == true ? dialog.SelectedRecipe : null;
    }

    public SpriteSourceChangeChoice ResolveSpriteSourceChange(SpriteDocumentSourceIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        var message = PresentationText.Format(
            "Text.Documents.SourceChangedPrompt",
            "The source '{0}' changed after this project was saved.\n\nYes: accept its new fingerprint.\nNo: choose a replacement file.\nCancel: leave the current document unchanged.",
            issue.RelativePath);
        var result = System.Windows.MessageBox.Show(
            message,
            App.Text("Text.Documents.SourceChangedTitle", "Sprite source changed"),
            System.Windows.MessageBoxButton.YesNoCancel,
            System.Windows.MessageBoxImage.Warning);
        return result switch
        {
            System.Windows.MessageBoxResult.Yes => SpriteSourceChangeChoice.AcceptNewFingerprint,
            System.Windows.MessageBoxResult.No => SpriteSourceChangeChoice.Relink,
            _ => SpriteSourceChangeChoice.Cancel
        };
    }

    public string? RelinkSpriteSource(SpriteDocumentSourceIssue issue, string? initialPath)
    {
        ArgumentNullException.ThrowIfNull(issue);
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "DMI and PNG sources (*.dmi;*.png)|*.dmi;*.png|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = PresentationText.Format(
                "Text.Documents.RelinkSourceTitleFormat",
                "Relink sprite source: {0}",
                issue.RelativePath)
        };
        ApplyInitialPath(dialog, initialPath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
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
