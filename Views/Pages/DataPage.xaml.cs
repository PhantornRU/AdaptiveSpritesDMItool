using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Processors;
using AdaptiveSpritesDMItool.ViewModels.Pages;
using DMISharp;
using MetadataExtractor;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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

namespace AdaptiveSpritesDMItool.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public bool isOverrideToggle = false;

        private Brush incorrectBrush = Brushes.Orange;
        private Brush correctBrush = Brushes.Green;

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

            if (loadedPath == string.Empty)
                loadedPath = EnvironmentController.defaultPath;
            var directories = FilesSearcher.GetDirectories(loadedPath, searchOption: SearchOption.TopDirectoryOnly);

            var treeItems = GetTreeItems(directories);
            foreach (var item in treeItems)
            {
                DataTreeView.Items.Add(item);
            }

            var files = System.IO.Directory.GetFiles(loadedPath);
            foreach (var file in files)
            {
                var fileTreeItem = new TreeViewItem { Header = GetHeaderFile(file) };
                DataTreeView.Items.Add(fileTreeItem);
            }
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

            var subDirectories = FilesSearcher.GetDirectories(directory, searchOption: SearchOption.TopDirectoryOnly);
            var treeItems = GetTreeItems(subDirectories);
            foreach (var item in treeItems)
            {
                treeItem.Items.Add(item);
            }

            var files = System.IO.Directory.GetFiles(directory);
            foreach (var file in files)
            {
                string headerFile = GetHeaderFile(file);
                var fileTreeItem = new TreeViewItem { Header = headerFile };
                if (!headerFile.Contains(EnvironmentController.defaultFileFormat))
                    fileTreeItem.Foreground = incorrectBrush;
                treeItem.Items.Add(fileTreeItem);
            }

            return treeItem;
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

        private void SetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string path = ViewModel.FolderPath;
            if (!Directory.Exists(path))
                return;

            loadedPath = path;
            GenerateTreeItems();
        }

        private void OverrideButton_Click(object sender, RoutedEventArgs e)
        {
            isOverrideToggle = !isOverrideToggle;
            var pressed = StatesController.GetPressedButtonAppearance();
            var unpressed = StatesController.GetUnPressedButtonAppearance();
            OverrideButton.Appearance = isOverrideToggle ? pressed : unpressed;
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

            var statesData = ViewModel.StatesData;
            if (statesData == null || statesData.Count() == 0)
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

            foreach (var config in selectedConfigs)
                DMIStatesProcessor.ProcessStatesWithConfig(config, statesData);

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
