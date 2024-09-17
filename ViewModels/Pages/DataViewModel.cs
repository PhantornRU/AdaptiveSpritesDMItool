using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Models;
using DMISharp;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace AdaptiveSpritesDMItool.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private IEnumerable<StateItem> _statesData = new List<StateItem>();

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            var statesCollection = new List<StateItem>();
            StatesData = statesCollection;
            _isInitialized = true;
        }


        #region States

        public void ClearStatesCollection()
        {
            StatesData = new List<StateItem>();
        }

        public void UpdateStatesCollection(StateItem item)
        {
            var statesCollection = new List<StateItem>();

            statesCollection.AddRange(StatesData);
            statesCollection.Add(item);

            StatesData = statesCollection;
        }

        #endregion States


        #region Tree View

        [ObservableProperty]
        public string _folderPath = string.Empty;

        [RelayCommand]
        public void OnSetFolder()
        {
            Debug.WriteLine(FolderPath);
            if (!File.Exists(FolderPath))
                return;

        }

        #endregion Tree View


        #region Config List

        [ObservableProperty]
        private ObservableCollection<ConfigItem> _BasicListConfigViewItems = GenerateConfigItems();

        private static ObservableCollection<ConfigItem> GenerateConfigItems()
        {
            var ConfigItems = StatesController.listConfigItems;
            return ConfigItems;
        }



        [ObservableProperty]
        private Visibility _openedLoadConfigVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _openedLoadConfig = string.Empty;

        [RelayCommand]
        public void OnLoadConfig()
        {
            OpenedLoadConfigVisibility = Visibility.Collapsed;
            string path = EnvironmentController.GetPixelStoragePath();
            string configFormat = EnvironmentController.configFormat;

            OpenFileDialog openFileDialog =
                new()
                {
                    //Multiselect = true,
                    InitialDirectory = path,
                    Filter = $"Config Files (*{configFormat})|*{configFormat};*{configFormat}"
                };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            if (!File.Exists(openFileDialog.FileName))
            {
                return;
            }

            foreach (ConfigItem item in BasicListConfigViewItems)
            {
                if (item.FileName == openFileDialog.SafeFileName)
                {
                    return;
                }
            }

            OpenedLoadConfig = openFileDialog.FileName;
            OpenedLoadConfigVisibility = Visibility.Visible;
            ConfigItem config = new ConfigItem(openFileDialog.SafeFileName, OpenedLoadConfig);
            BasicListConfigViewItems.Add(config);
        }

        public void RemoveConfig(ConfigItem config)
        {
            BasicListConfigViewItems.Remove(config);
        }

        #endregion Config List

    }
}
