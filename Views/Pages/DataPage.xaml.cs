using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Processors;
using AdaptiveSpritesDMItool.ViewModels.Pages;
using DMISharp;
using MetadataExtractor;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Controls;
using static System.Net.WebRequestMethods;
using Button = Wpf.Ui.Controls.Button;
using Directory = System.IO.Directory;
using File = System.IO.File;
using TreeViewItem = System.Windows.Controls.TreeViewItem;
using SixLabors.ImageSharp.Processing;
using static System.Net.Mime.MediaTypeNames;
using AdaptiveSpritesDMItool.Resources;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {

        /// <summary> Paths to files to be processed. </summary>
        private List<string> filesPaths = new List<string>();

        private Brush incorrectBrush = Brushes.Orange;
        private Brush correctBrush = Brushes.Green;
        private Brush badBrush = Brushes.Red;

        public DataViewModel ViewModel { get; }
        public DataPage(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            loadedPath = string.Empty;
            GenerateTreeItems();
        }


        #region Tree View
        private string loadedPath = string.Empty;

        private void GenerateTreeItems()
        {
            DataTreeView.Items.Clear();
            filesPaths.Clear();

            if (loadedPath == string.Empty)
                loadedPath = EnvironmentController.defaultImportPath;
            FillTree(DataTreeView, loadedPath);
        }

        private IEnumerable<TreeViewItem> GetTreeItems(IEnumerable<string> directories)
        {
            foreach (var directory in directories)
            {
                yield return GetTreeItem(directory);
            }
        }

        private TreeViewItem GetTreeItem(string directory)
        {
            var treeItem = new TreeViewItem { Header = GetHeaderFile(directory), IsEnabled = true };
            FillTreeItem(treeItem, directory);
            return treeItem;
        }

        private void FillTree(TreeView tree, string directory)
        {
            var directories = FilesSearcher.GetDirectories(loadedPath, searchOption: SearchOption.TopDirectoryOnly);
            var treeItems = GetTreeItems(directories);
            foreach (var item in treeItems)
            {
                DataTreeView.Items.Add(item);
            }

            var files = System.IO.Directory.GetFiles(directory);
            foreach (var file in files)
            {
                TreeViewItem fileTreeItem = GetTreeViewItem(directory, file);
                tree.Items.Add(fileTreeItem);
            }
        }

        private void FillTreeItem(TreeViewItem treeItem,string directory)
        {
            var subDirectories = FilesSearcher.GetDirectories(directory, searchOption: SearchOption.TopDirectoryOnly);
            var treeItems = GetTreeItems(subDirectories);
            foreach (var item in treeItems)
            {
                treeItem.Items.Add(item);
            }

            var files = System.IO.Directory.GetFiles(directory);
            foreach (var file in files)
            {
                TreeViewItem fileTreeItem = GetTreeViewItem(directory, file);
                treeItem.Items.Add(fileTreeItem);
            }
        }

        private TreeViewItem GetTreeViewItem(string directory, string file)
        {
            string headerFile = GetHeaderFile(file);
            var fileTreeItem = new TreeViewItem { Header = headerFile };
            if (!headerFile.Contains(EnvironmentController.defaultFileFormat))
                fileTreeItem.Foreground = incorrectBrush;
            else
            {
                string fullPath = $"{directory}\\{headerFile}";
                filesPaths.Add(fullPath);
            }
            return fileTreeItem;
        }



        #endregion Tree View


        #region Buttons

        public void TreeItemChanged(object sender, RoutedEventArgs e)
        {
            TreeView? listView = sender as TreeView;
            if (listView == null) return;
            var item = listView.SelectedItem;
            if (item == null) return;

            TreeViewItem? parent = item as TreeViewItem;
            string fullPath = GetFullPath(listView, item);
            fullPath = loadedPath + fullPath;

            ViewItemsFromSelectedDMI(fullPath);
        }

        List<ConfigItem> selectedConfigs = new List<ConfigItem>();

        private void ConfigChanged(object sender, SelectionChangedEventArgs e)
        {
            Wpf.Ui.Controls.ListView? listView = sender as Wpf.Ui.Controls.ListView;
            if (listView == null) return;
            var items = listView.SelectedItems;
            selectedConfigs.Clear();
            foreach (var item in items)
            {
                ConfigItem? config = item as ConfigItem;
                selectedConfigs.Add(config);
            }
        }

        private void SetImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string path = ViewModel.FolderPath;
            if (!Directory.Exists(path))
                return;

            loadedPath = path;
            GenerateTreeItems();
        }

        private void OverrideButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.isOverrideToggle = !StatesController.isOverrideToggle;
            var pressed = StatesController.GetPressedButtonAppearance();
            var unpressed = StatesController.GetUnPressedButtonAppearance();
            OverrideButton.Appearance = StatesController.isOverrideToggle ? pressed : unpressed;
        }

        private void ConfigRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ConfigItem config = button.DataContext as ConfigItem;
            ViewModel.RemoveConfig(config);
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedConfigs.Count == 0)
            {

                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "No config selected",
                    Content =
                        "The process has been cancelled." +
                        "\nPlease upload the configs that will process the files, then select those that will participate in the process. " +
                        "\nYou can select several at once.",
                };
                uiMessageBox.Content = 
                _ = await uiMessageBox.ShowDialogAsync();
                return;
            }

            if (filesPaths.Count == 0)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "No DMI states found",
                    Content =
                        "Please upload DMI files by selecting the folder of their content directory. All subfolders in this category will be processed as well. " +
                        "\nThe final result will be uploaded to separate files and directories under the config name. " +
                        "\nAll files will have the same names and states.",
                };
                _ = await uiMessageBox.ShowDialogAsync();
                return;
            }

            if(!DMIStatesProcessor.IsIterEnded())
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Process in progress",
                    Content =
                        "The program is processing programs. " +
                        "\nIt is not possible to start new processes, please wait until the end.",
                };
                _ = await uiMessageBox.ShowDialogAsync();
                return;
            }

            DMIStatesProcessor.InitializeNewData(filesPaths);
            DMIStatesProcessor.UpdateProgressBar(ProgressBarProcess, StatusMessage);
            foreach (var config in selectedConfigs)
                DMIStatesProcessor.ProcessFilesWithConfig(config);
        }




        private void Test()
        {
            string path = EnvironmentController.defaultImportPath + "\\testBodies.dmi";
            using DMIFile file = new DMIFile(path);

            var firstState = file.States.First();
            int width = firstState.Width;
            int height = firstState.Height;
            using var newDMI = new DMIFile(width, height);
            newDMI.ImportStates(file);

            ConfigItem config = selectedConfigs.First();
            DataPixelStorage dataPixelStorage = new DataPixelStorage(config.FilePath, width, height);

            foreach (DMIState state in newDMI.States)
            {
                Debug.WriteLine($"state: {state.Name}, --- {state.Frames} - {state.FrameCapacity} - {state.TotalFrames} - {state.Width} - {state.Height}");

                StateDirection[] stateDirections = StatesController.GetAllStateDirections(state.DirectionDepth);
                
                for (int i = 0; i < state.Frames; i++)
                {
                    foreach (StateDirection direction in stateDirections)
                    {
                        //Debug.WriteLine($"{state.Name} [{i}] {direction}");
                        Image<Rgba32>? img = state.GetFrame(direction, i); 
                        if (img == null)
                            continue;
                        using Image<Rgba32>? imgCopy = img.Clone();
                        img.ProcessPixelRows(accessor =>
                        {
                            // Color is pixel-agnostic, but it's implicitly convertible to the Rgba32 pixel type
                            Rgba32 transparent = SixLabors.ImageSharp.Color.Transparent;

                            for (int y = 0; y < accessor.Height; y++)
                            {
                                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                                // pixelRow.Length has the same value as accessor.Width,
                                // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                                for (int x = 0; x < pixelRow.Length; x++)
                                {
                                    var currentPoint = new System.Drawing.Point(x, y);
                                    var storagePoint = dataPixelStorage.GetPointStorage(direction, currentPoint);
                                    if (currentPoint.X == storagePoint.X && currentPoint.Y == storagePoint.Y)
                                        continue;

                                    // Get a reference to the pixel at position x
                                    ref Rgba32 pixel = ref pixelRow[x];

                                    // Get current color
                                    Rgba32 color;
                                    if(storagePoint.X == -1 || storagePoint.Y == -1)
                                    {
                                        color = transparent;
                                    }
                                    else
                                    {
                                        color = imgCopy[storagePoint.X, storagePoint.Y];
                                    }

                                    pixel = color;
                                }
                            }
                        });
                    }
                }
            }

            string configName = config.FileName.Replace(EnvironmentController.configFormat, "");
            string filePath = Path.Combine(EnvironmentController.lastExportPath, configName, path);
            string? directoryPath = Path.GetDirectoryName(filePath);

            //newDMI.SortStates();
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            newDMI.Save(filePath);
        }





















        private void ChoosenItemButton_Click(object sender, RoutedEventArgs e)
        {
            return;
        }

        #endregion Buttons


        #region Helpers

        private string GetFullPath(TreeView listView, object item)
        {
            var fullPath = string.Empty;
            var parent = item as TreeViewItem;
            if (parent == null) return fullPath;
            fullPath = GetFullPath(listView, parent.Parent) + "\\" + parent.Header;
            return fullPath;
        }

        private string GetHeaderFile(string directory)
        {
            var pathParts = directory.Split('\\');
            return pathParts.Last();
        }

        private void ViewItemsFromSelectedDMI(string fullPath)
        {
            if (!fullPath.Contains(".dmi"))
                return;
            ViewModel.ClearStatesCollection();


            //var StateItems = new ObservableCollection<StateItem>();
            using DMIFile fileDmi = new DMIFile(fullPath);
            var states = fileDmi.States;
            foreach (DMIState state in states)
            {
                WriteableBitmap writeableBitmap = ImageEncoder.GetBMPFromDMIState(state, StateDirection.South);
                Brush color = Brushes.GreenYellow;
                StateItem stateItem = new StateItem(
                        "NoName",
                        fullPath,
                        state.Name,
                        writeableBitmap,
                        color
                    );
                ViewModel.UpdateStatesCollection(stateItem);
            }
        }

        #endregion Helpers

    }
}
