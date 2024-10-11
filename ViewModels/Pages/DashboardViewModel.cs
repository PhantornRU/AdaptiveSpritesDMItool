using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Resources;
using DMISharp;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
                var fileName = Path.GetFileName(file);
                fileName = fileName.Substring(0, fileName.Length - EnvironmentController.savesFormat.Length);

                var lastWriteTime = File.GetLastWriteTime(file);

                environmentItems.Add(new EnvironmentItem(fileName, file, lastWriteTime));
            }

            // sort by last modified date
            environmentItems = new ObservableCollection<EnvironmentItem>(
                    environmentItems.OrderBy(x => x.LastModifiedDate)
                );

            return environmentItems;
        }

    }
}
