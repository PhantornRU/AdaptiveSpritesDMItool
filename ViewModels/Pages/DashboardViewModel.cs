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

            return environmentItems; // !!!!!!!!!!!!!!

            string path = EnvironmentController.defaultSavesPath;

            if (!Directory.Exists(path))
            {
                throw new Exception("Cant find path to dmi files.");
                //Directory.CreateDirectory(path);
            }

            var files = Directory.GetFiles(path, "*.dmi");

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var lastModified = File.GetLastWriteTime(file);

                var item = new EnvironmentItem(fileName, file, lastModified);
                environmentItems.Add(item);
            }

            // sort by last modified date
            environmentItems = new ObservableCollection<EnvironmentItem>(
                    environmentItems.OrderBy(x => x.LastModifiedDate)
                );

            return environmentItems;
        }

    }
}
