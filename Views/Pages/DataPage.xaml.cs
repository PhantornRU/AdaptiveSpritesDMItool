using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.ViewModels.Pages;
using DMISharp;
using MetadataExtractor;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using static System.Net.WebRequestMethods;
using Directory = System.IO.Directory;
using File = System.IO.File;
using TreeViewItem = System.Windows.Controls.TreeViewItem;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public bool isOverrideToggle = false;

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

            if(loadedPath == string.Empty)
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
                var fileTreeItem = new TreeViewItem { Header = GetHeaderFile(file) };
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

            string fullPath = "";
            TreeViewItem? parent = item as TreeViewItem;
            fullPath = GetFullPath(listView, item);

            //if (fullPath.Length > 0 && fullPath.First() == '\\')
            //    fullPath = fullPath.Substring(1); // remove first slash
            fullPath = loadedPath + fullPath;

            ViewItemsFromSelectedDMI(fullPath);
            //Debug.WriteLine($"Tree View Item Changed to: {fullPath} \t\t- Item: {item}");
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
                //var random = new Random();
                //Brush color = new SolidColorBrush(
                //    Color.FromArgb(
                //        (byte)100,
                //        (byte)random.Next(0, 250),
                //        (byte)random.Next(0, 250),
                //        (byte)random.Next(0, 250)
                //    )
                //);

                Brush color = Brushes.GreenYellow;

                StateItem stateItem = new StateItem(
                        "NoName",
                        fullPath,
                        state.Name,
                        writeableBitmap,
                        color
                    );

                //StateItems.Add(stateItem);
                ViewModel.UpdateStatesCollection(stateItem);
            }
        }

        #endregion Helpers
    }
}
