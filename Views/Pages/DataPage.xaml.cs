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

        private Brush correctBrush = Brushes.ForestGreen;
        private Brush incorrectBrush = Brushes.Orange;
        private Brush badBrush = Brushes.Red;
        private Brush skipBrush = Brushes.Gray;
        private Brush doneBrush = Brushes.GreenYellow;

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
            string fullPath = $"{directory}\\{headerFile}";
            fileTreeItem.Foreground = GetColorTreeItem(headerFile, fullPath);
            if (headerFile.Contains(EnvironmentController.defaultFileFormat))
            {
                filesPaths.Add(fullPath);
            }
            return fileTreeItem;
        }

        private Brush GetColorTreeItem(string headerFile, string fullPath)
        {
            // No DMI file? No process.
            if (!headerFile.Contains(EnvironmentController.defaultFileFormat))
                return badBrush;

            if (fullPath == string.Empty)
                return badBrush;

            // Work with configs & DMI file states
            using DMIFile fileDmi = new DMIFile(fullPath);
            if (fileDmi.States.Count == 0)
                return badBrush;

            if (StatesController.isOverrideToggle)
                return correctBrush;

            foreach (ConfigItem config in selectedConfigs)
            {
                string exportPath = FilesSearcher.GetExportConfigPath(config.FileName, fullPath);
                if (File.Exists(exportPath) == false)
                    continue;
                DMIFile exportedDMIFile = new DMIFile(exportPath);
                foreach (DMIState state in fileDmi.States)
                {
                    foreach (DMIState exportedState in exportedDMIFile.States)
                    {
                        if (state.Name == exportedState.Name)
                            return incorrectBrush;
                    }
                }
            }

            return correctBrush;
        }

        #endregion Tree View


        #region Changed Events

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
            GenerateTreeItems();
        }

        #endregion Changed Events


        #region Buttons

        private void SetImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string path = ViewModel.FolderImportPath;
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

            string fullPath = EnvironmentController.lastImportPath;
            GenerateTreeItems();
        }

        private void ConfigRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            ConfigItem? config = button?.DataContext as ConfigItem;
            if (config == null) return;
            ViewModel.RemoveConfig(config);
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedConfigs.Count == 0)
            {
                ShowMessages.NoConfigSelected();
                return;
            }

            foreach (ConfigItem config in selectedConfigs)
            {
                if (config.State != ConfigState.NotSaved)
                    continue;

                ShowMessages.NotSavedConfigSelected();
                return;
            }

            if (filesPaths.Count == 0)
            {
                ShowMessages.NoDMIStatesFound();
                return;
            }

            if(!DMIStatesProcessor.IsIterEnded())
            {
                ShowMessages.ProcessInProgress();
                return;
            }

            DMIStatesProcessor.InitializeNewData(filesPaths, selectedConfigs.Count);
            DMIStatesProcessor.UpdateProgressBar(ProgressBarProcess, StatusMessage);
            foreach (var config in selectedConfigs)
                DMIStatesProcessor.ProcessFilesWithConfig(config);
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

        #endregion Helpers

        #region Edit

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

                Brush color = correctBrush;
                if (!StatesController.isOverrideToggle)
                {
                    foreach (ConfigItem config in selectedConfigs)
                    {
                        string exportPath = FilesSearcher.GetExportConfigPath(config.FileName, fullPath);
                        if (File.Exists(exportPath) == false)
                            continue;
                        DMIFile exportedDMIFile = new DMIFile(exportPath);
                        foreach (DMIState exportedState in exportedDMIFile.States)
                        {
                            if (state.Name == exportedState.Name)
                                color = incorrectBrush;
                        }
                    }
                }

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

        #endregion Edit

    }
}
