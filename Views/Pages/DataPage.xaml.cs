using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.ViewModels.Pages;
using MetadataExtractor;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using static System.Net.WebRequestMethods;
using Directory = System.IO.Directory;
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

            GenerateTreeItems();
        }


        #region TreeView

        private void GenerateTreeItems()
        {
            //DataTreeView.Items.Clear();

            string path = EnvironmentController.defaultPath;
            var directories = FilesSearcher.GetDirectories(path, searchOption: SearchOption.TopDirectoryOnly);
            
            var treeItems = GetTreeItems(directories);
            foreach (var item in treeItems)
            {
                DataTreeView.Items.Add(item);
            }

            var files = System.IO.Directory.GetFiles(path);
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

        private string GetHeaderFile(string directory)
        {
            var pathParts = directory.Split('\\');
            return pathParts.Last();
        }

        #endregion TreeView


        #region Buttons

        public void TreeItemChanged(object sender, RoutedEventArgs e)
        {
            TreeView? listView = sender as TreeView;
            if (listView == null) return;
            var item = listView.SelectedItem;
            //TreeViewItem? state = listView.SelectedItem as TreeViewItem;
            if (item == null) return;
            Debug.WriteLine($"State Changed to: {item}");
        }

        private void OverrideButton_Click(object sender, RoutedEventArgs e)
        {
            isOverrideToggle = !isOverrideToggle;
            var pressed = StatesController.GetPressedButtonAppearance();
            var unpressed = StatesController.GetUnPressedButtonAppearance();
            OverrideButton.Appearance = isOverrideToggle ? pressed : unpressed;
        }
        #endregion Buttons
    }
}
