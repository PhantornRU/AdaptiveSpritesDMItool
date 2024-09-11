using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using AdaptiveSpritesDMItool.Models;
using DMISharp;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Wpf.Ui.Controls;


namespace AdaptiveSpritesDMItool.ViewModels.Pages
{
    public partial class StatesEditorViewModel : ObservableObject
    {

        #region List View

        #region Preview

        #region Preview Select

        private int _ListViewPreviewSelectionModeComboBoxSelectedIndex = 0;

        public int ListViewPreviewSelectionModeComboBoxSelectedIndex
        {
            get => _ListViewPreviewSelectionModeComboBoxSelectedIndex;
            set
            {
                _ = SetProperty(ref _ListViewPreviewSelectionModeComboBoxSelectedIndex, value);
                UpdateListViewPreviewSelectionMode(value);
            }
        }

        #endregion Preview Select

        [ObservableProperty]
        private SelectionMode _ListViewPreviewSelectionMode = SelectionMode.Single;

        [ObservableProperty]
        private ObservableCollection<StateItem> _BasicListPreviewViewItems = GenerateStateItems();

        private void UpdateListViewPreviewSelectionMode(int selectionModeIndex)
        {
            ListViewPreviewSelectionMode = selectionModeIndex switch
            {
                1 => SelectionMode.Multiple,
                2 => SelectionMode.Extended,
                _ => SelectionMode.Single
            };
        }

        private static ObservableCollection<StateItem> GenerateStateItems()
        {
            string path = EnvironmentController.defaultPath;
            string fileName = EnvironmentController.defaultFileName;
            string fullpath = $"{path}/{fileName}.dmi";

            using DMIFile fileDmi = new DMIFile(fullpath);

            var StateItems = GetListStateItems(fileDmi, fullpath);
            return StateItems;
        }

        private static ObservableCollection<StateItem> GetListStateItems(DMIFile fileDmi, string fullPath)
        {
            var StateItems = new ObservableCollection<StateItem>();
            string fileName = EnvironmentController.defaultFileName;

            for (int index = 0; index < fileDmi.States.Count; index++)
            {
                DMIState currentState = fileDmi.States.ElementAt(index);
                Debug.WriteLine($"GetListStateItems - Loaded {fileName}({fileDmi.States.Count}).");
                WriteableBitmap writeableBitmap = ImageEncoder.GetBMPFromDMIState(currentState, StateDirection.South);
                StateItems.Add(
                    new StateItem(
                        fileName,
                        fullPath,
                        currentState.Name,
                        writeableBitmap

                    )
                );
            }

            return StateItems;
        }

        /// <summary> Selected config item in the list </summary>
        StateItem? currentState;

        public void StateChanged(StateItem? stateItem)
        {
            // Save last selected config
            //if (currentState != null)
            //    EnvironmentController.dataPixelStorage.SavePixelStorage(currentState.FilePath);

            // Load selected config
            currentState = stateItem;
            if (stateItem == null)
            {
                Debug.WriteLine("State nullified.");
                //EnvironmentController.currentStateFullPath = string.Empty;
                return;
            }

            using DMIFile fileDmi = new DMIFile(stateItem.FilePath);
            var states = fileDmi.States;
            DMIState? state = null;
            foreach(DMIState item in states)
            {
                if (item.Name != stateItem.StateName)
                    continue;
                state = item;
            }
            if (state == null)
                return;

            var statePreviewMode = StatesController.currentStatePreviewMode;
            switch (ListViewPreviewSelectionMode)
            {
                case SelectionMode.Single:
                    EnvironmentController.dataImageState.ReplaceDMIState(state, statePreviewMode);
                    break;
                case SelectionMode.Multiple:
                    // !!!!!!!!!!! ЗАПИХНУТЬ ПРОВЕРКУ НА СРАЗУ НЕСКОЛЬКО ВЫДЕЛЕННЫХ СТЕЙТОВ !!!!!!!!!!!!!!!!!!!!
                    //EnvironmentController.dataImageState.CombineDMIState(state, statePreviewMode);
                    break;
                default:
                    break;
            }
            Debug.WriteLine($"State Changed to: {stateItem.FileName} {stateItem.StateName}");
        }

        #endregion Preview


        #endregion List View


        #region Files

        [ObservableProperty]
        private Visibility _openedMultiplePathVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _openedMultiplePath = string.Empty;

        [RelayCommand]
        public void OnOpenMultiple()
        {
            OpenedMultiplePathVisibility = Visibility.Collapsed;

            OpenFileDialog openFileDialog =
                new()
                {
                    Multiselect = true,
                    InitialDirectory = EnvironmentController.lastPath,
                    //Filter = "All files (*.*)|*.*"
                    Filter = "DMI files (*.dmi)|*.dmi;*.dmi"
                };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            if (openFileDialog.FileNames.Length == 0)
            {
                return;
            }

            var fileNames = openFileDialog.FileNames;

            if(BasicListPreviewViewItems == null)
                BasicListPreviewViewItems = new ObservableCollection<StateItem>();

            foreach (var path in fileNames)
            {
                using DMIFile fileDmi = new DMIFile(path);
                var states = GetListStateItems(fileDmi, path);
                foreach (var state in states)
                {
                    BasicListPreviewViewItems.Add(state);
                }
            }

            OpenedMultiplePath = string.Join("\n", fileNames);
            OpenedMultiplePathVisibility = Visibility.Visible;
        }

        [RelayCommand]
        public void OnClearList()
        {
            BasicListPreviewViewItems.Clear(); //BasicListPreviewViewItems.Clear();
            OpenedMultiplePath = string.Empty;
            OpenedMultiplePathVisibility = Visibility.Collapsed;
        }

        #endregion Files


        #region Config

        /// <summary> Selected config item in the list </summary>
        ConfigItem? currentConfig;

        /// <summary> The last item selected in the list </summary>
        private int lastIndexConfig = 0;

        [ObservableProperty]
        private ObservableCollection<ConfigItem> _BasicListConfigViewItems = GenerateConfigItems();

        private static ObservableCollection<ConfigItem> GenerateConfigItems()
        {
            var ConfigItems = new ObservableCollection<ConfigItem>();
            ConfigItem config = GetNewConfigItem();
            ConfigItems.Add(config);
            return ConfigItems;
        }

        public void ConfigChanged(ConfigItem? config, int index)
        {
            // Save last selected config
            if (currentConfig != null)
                EnvironmentController.dataPixelStorage.SavePixelStorage(currentConfig.FilePath);

            // Load selected config
            lastIndexConfig = index;
            currentConfig = config;
            if (config == null)
            {
                Debug.WriteLine("Config nullified.");
                EnvironmentController.currentConfigFullPath = string.Empty;
                return;
            }
            Debug.WriteLine($"Config Changed to: {config.FileName} \nPath: {config.FilePath}");
            EnvironmentController.currentConfigFullPath = config.FilePath;
            EnvironmentController.dataPixelStorage.LoadPixelStorageToEnvironment(config.FilePath);
        }

        [RelayCommand]
        public async Task OnNewConfig(CancellationToken cancellation)
        {
            FileToSaveFullPath = string.Empty;
            ConfigItem config = GetNewConfigItem();
            BasicListConfigViewItems.Add(config);
        }

        [RelayCommand]
        public void OnClearConfig()
        {
            BasicListConfigViewItems.Clear();
            currentConfig = null;
            FileToSaveFullPath = string.Empty;
            SavedConfigNoticeVisibility = Visibility.Collapsed;
            SavedConfigNotice = string.Empty;
        }


        [ObservableProperty]
        private string _fileToSaveFullPath = string.Empty;

        [ObservableProperty]
        private Visibility _savedConfigNoticeVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _savedConfigNotice = string.Empty;

        [RelayCommand]
        public async Task OnSaveConfig(CancellationToken cancellation)
        {
            string fullpath = EnvironmentController.currentConfigFullPath;
            if (fullpath == string.Empty)
            {
                OnSaveAsConfig(cancellation);
            }
            else
            {
                ConfigItem selectedItem = BasicListConfigViewItems[lastIndexConfig];
                selectedItem.State = ConfigState.Saved;
                EnvironmentController.dataPixelStorage.SavePixelStorage(fullpath);
            }


        }

        [RelayCommand]
        public async Task OnSaveAsConfig(CancellationToken cancellation)
        {
            SavedConfigNoticeVisibility = Visibility.Collapsed;
            string path = EnvironmentController.GetPixelStoragePath();
            string configFormat = EnvironmentController.configFormat;
            SaveFileDialog saveFileDialog =
                new()
                {
                    Filter = $"Config Files (*{configFormat})|*{configFormat}",
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

            if (File.Exists(saveFileDialog.FileName))
            {
                // Protect the user from accidental writes
                return;
            }

            try
            {
                string fileName = saveFileDialog.FileName;
                FileToSaveFullPath = fileName;
                EnvironmentController.dataPixelStorage.SavePixelStorage(fileName);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);

                return;
            }

            SavedConfigNoticeVisibility = Visibility.Visible;
            SavedConfigNotice = $"File {saveFileDialog.FileName} was saved.";

            ConfigItem config = new ConfigItem(saveFileDialog.SafeFileName, FileToSaveFullPath);
            config.State = ConfigState.Saved;
            if (currentConfig != null && currentConfig.FilePath == string.Empty)
            {
                int index = BasicListConfigViewItems.IndexOf(currentConfig);
                if (index != -1)
                    BasicListConfigViewItems[index] = config;
                currentConfig = config;
                return;
            }
            BasicListConfigViewItems.Add(config);
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
            
            foreach(ConfigItem item in BasicListConfigViewItems)
            {
                if(item.FileName == openFileDialog.SafeFileName)
                {
                    return;
                }
            }

            OpenedLoadConfig = openFileDialog.FileName;
            OpenedLoadConfigVisibility = Visibility.Visible;
            BasicListConfigViewItems.Add(new ConfigItem(openFileDialog.SafeFileName, OpenedLoadConfig));
        }


        #region Helpers

        private static int numItem = 0;
        private static ConfigItem GetNewConfigItem()
        {
            numItem++;
            ConfigItem config = new ConfigItem($"NewConfigItem{numItem} (Not Saved)", string.Empty);
            config.State = ConfigState.NotSaved;
            return config;
        }

        #endregion Helpers

        #endregion Config
    }
}
