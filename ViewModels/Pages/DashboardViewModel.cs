using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Resources;
using DMISharp;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace AdaptiveSpritesDMItool.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<EnvironmentItem> _BasicListEnvironmentViewItems = GenerateEnvironmentItems();

        private static ObservableCollection<EnvironmentItem> GenerateEnvironmentItems()
        {
            var environmentItems = new ObservableCollection<EnvironmentItem>();

            string path = EnvironmentController.defaultSavesPath;
            string searchPattern = $"*{EnvironmentController.savesFormat}";
            var files = Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                //fileName = fileName.Substring(0, fileName.Length - EnvironmentController.savesFormat.Length);
                var lastWriteTime = File.GetLastWriteTime(file);

                environmentItems.Add(new EnvironmentItem(fileName, file, lastWriteTime));
            }
            SortEnvironmentItems(environmentItems);

            return environmentItems;
        }


        #region Commands

        [RelayCommand]
        public async Task OnNewEnvironment(CancellationToken cancellation)
        {
            Debug.WriteLine("New Environment button pressed");
            string fileName = $"New EnvironmentItem (Not Saved)";
            string filePath = EnvironmentController.defaultSavesPath;
            DateTime fileDate = DateTime.Now;
            EnvironmentItem environment = new EnvironmentItem(fileName, filePath, fileDate);

            BasicListEnvironmentViewItems.Add(environment);
            SortEnvironmentItems(BasicListEnvironmentViewItems);

            EnvironmentController.SetSaveFile(environment);
        }
        [ObservableProperty]
        private string _fileToSaveFullPath = string.Empty;

        [ObservableProperty]
        private Visibility _savedEnvironmentNoticeVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _savedEnvironmentNotice = string.Empty;

        [RelayCommand]
        public async Task OnSaveEnvironment(CancellationToken cancellation)
        {
            EnvironmentItem? environment = EnvironmentController.choosenSaveFile;
            if (environment == null)
            {
                _ = OnSaveAsEnvironment(cancellation);
                return;
            }

            SaveSettings(environment);
            SortEnvironmentItems(BasicListEnvironmentViewItems);

            try
            {
                string exportpath = EnvironmentController.lastExportPath;
                if (exportpath != string.Empty)
                {
                    EnvironmentController.dataPixelStorage.SavePixelStorage(exportpath);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);

                return;
            }
        }

        [RelayCommand]
        public async Task OnSaveAsEnvironment(CancellationToken cancellation)
        {
            SavedEnvironmentNoticeVisibility = Visibility.Collapsed;
            EnvironmentItem? file = EnvironmentController.choosenSaveFile;
            string? path = file == null ? EnvironmentController.defaultSavesPath : Path.GetDirectoryName(file.FilePath);
            string filesFormat = EnvironmentController.savesFormat;
            SaveFileDialog saveFileDialog =
                new()
                {
                    Filter = $"Environment Files (*{filesFormat})|*{filesFormat}",
                    InitialDirectory = path
                };

            if (!string.IsNullOrEmpty(FileToSaveFullPath))
            {
                var invalidChars =
                    new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

                saveFileDialog.FileName = string.Join(
                        "_",
                        FileToSaveFullPath.Split(invalidChars.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                    )
                    .Trim();
            }

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                string fileName = saveFileDialog.FileName;
                FileToSaveFullPath = fileName;

                string exportpath = EnvironmentController.lastExportPath;
                if (exportpath != string.Empty)
                {
                    EnvironmentController.dataPixelStorage.SavePixelStorage(exportpath);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);

                return;
            }

            SavedEnvironmentNoticeVisibility = Visibility.Visible;
            SavedEnvironmentNotice = $"File {saveFileDialog.FileName} was saved.";

            foreach (EnvironmentItem item in BasicListEnvironmentViewItems)
            {
                if (item.FileName == saveFileDialog.SafeFileName
                    && item.FilePath == FileToSaveFullPath)
                    return;
            }

            EnvironmentItem environment = new EnvironmentItem(saveFileDialog.SafeFileName, FileToSaveFullPath, DateTime.Now);

            BasicListEnvironmentViewItems.Add(environment);
            SaveSettings(environment);
            SortEnvironmentItems(BasicListEnvironmentViewItems);

            EnvironmentController.SetSaveFile(environment);
        }


        [ObservableProperty]
        private Visibility _openedLoadEnvironmentVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _openedLoadEnvironment = string.Empty;

        [RelayCommand]
        public void OnLoadEnvironment()
        {
            OpenedLoadEnvironmentVisibility = Visibility.Collapsed;
            string path = EnvironmentController.GetPixelStoragePath();
            string savesFormat = EnvironmentController.savesFormat;

            OpenFileDialog openFileDialog =
                new()
                {
                    //Multiselect = true,
                    InitialDirectory = path,
                    Filter = $"Environment Files (*{savesFormat})|*{savesFormat};*{savesFormat}"
                };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            if (!File.Exists(openFileDialog.FileName))
            {
                return;
            }

            foreach (EnvironmentItem item in BasicListEnvironmentViewItems)
            {
                if (item.FileName != openFileDialog.SafeFileName)
                    continue;
                ShowMessages.FileAlreadyLoaded();
                return;
            }

            OpenedLoadEnvironment = openFileDialog.FileName;
            OpenedLoadEnvironmentVisibility = Visibility.Visible;

            var fileName = Path.GetFileNameWithoutExtension(openFileDialog.SafeFileName);
            DateTime lastTime = File.GetLastWriteTime(openFileDialog.FileName);
            EnvironmentItem environment = new EnvironmentItem(fileName, OpenedLoadEnvironment, lastTime);

            BasicListEnvironmentViewItems.Add(environment);
            SortEnvironmentItems(BasicListEnvironmentViewItems);
        }

        #endregion Commands


        #region Helpers

        private static void SaveSettings(EnvironmentItem environment)
        {
            string fullpath = environment.FilePath;
            Debug.WriteLine($"Save Environment: {fullpath} - {environment.LastModifiedDate}");
            EnvironmentController.SaveSettings(fullpath);
        }

        private static void SortEnvironmentItems(ObservableCollection<EnvironmentItem> items)
        {
            var chosenFileName = EnvironmentController.choosenSaveFile?.FileName;
            if (chosenFileName != null)
            {
                var itemToUpdate = items.FirstOrDefault(item => item.FileName == chosenFileName);
                if (itemToUpdate != null)
                {
                    var updatedItem = new EnvironmentItem(itemToUpdate.FileName, itemToUpdate.FilePath, DateTime.Now);
                    int index = items.IndexOf(itemToUpdate);
                    items[index] = updatedItem;
                }
            }

            // sort by last modified date in descending order
            items = new ObservableCollection<EnvironmentItem>(
                    items.OrderByDescending(x => x.LastModifiedDate)
                );
        }

        #endregion Helpers
    }
}
