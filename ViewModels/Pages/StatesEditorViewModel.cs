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
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDMItool.ViewModels.Pages
{
    public partial class StatesEditorViewModel : ObservableObject
    {

        #region List View

        private int _listViewSelectionModeComboBoxSelectedIndex = 0;

        public int ListViewSelectionModeComboBoxSelectedIndex
        {
            get => _listViewSelectionModeComboBoxSelectedIndex;
            set
            {
                _ = SetProperty(ref _listViewSelectionModeComboBoxSelectedIndex, value);
                UpdateListViewSelectionMode(value);
            }
        }

        [ObservableProperty]
        private SelectionMode _listViewSelectionMode = SelectionMode.Single;

        [ObservableProperty]
        private ObservableCollection<StateItem> _basicListViewItems = GenerateStateItems();

        private void UpdateListViewSelectionMode(int selectionModeIndex)
        {
            ListViewSelectionMode = selectionModeIndex switch
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

            var StateItems = GetListStateItems(fileDmi);
            return StateItems;
        }

        private static ObservableCollection<StateItem> GetListStateItems(DMIFile fileDmi)
        {
            var StateItems = new ObservableCollection<StateItem>();

            string fileName = EnvironmentController.defaultFileName;
            StateDirection direction = StateDirection.South;

            for (int index = 0; index < fileDmi.States.Count; index++)
            {
                DMIState currentState = fileDmi.States.ElementAt(index);
                BitmapSource bmp = ImageEncoder.GetBMPFromDMIState(currentState, direction);

                StateItems.Add(
                    new StateItem(
                        fileName,
                        currentState.Name,
                        bmp
                    )
                );
            }

            return StateItems;
        }

        #endregion List View


        #region Files

        [ObservableProperty]
        private Visibility _openedMultiplePathVisibility = Visibility.Collapsed;

        //[ObservableProperty]
        //private string _openedMultiplePath = string.Empty;

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

            if(BasicListViewItems == null)
                BasicListViewItems = new ObservableCollection<StateItem>();

            foreach (var path in fileNames)
            {
                using DMIFile fileDmi = new DMIFile(path);
                var states = GetListStateItems(fileDmi);
                foreach (var state in states)
                {
                    BasicListViewItems.Add(state);
                }
            }

            //OpenedMultiplePath = string.Join("\n", fileNames);
            OpenedMultiplePathVisibility = Visibility.Visible;
        }

        [RelayCommand]
        public void OnClearList() => BasicListViewItems.Clear(); //BasicListViewItems.Clear();

        #endregion Files


        #region Config

        [ObservableProperty]
        private string _fileToSaveName = string.Empty;

        [ObservableProperty]
        private string _fileToSaveContents = string.Empty;

        [ObservableProperty]
        private Visibility _savedConfigNoticeVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _savedConfigNotice = string.Empty;

        [RelayCommand]
        public async Task OnSaveConfig(CancellationToken cancellation)
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

            if (!string.IsNullOrEmpty(FileToSaveName))
            {
                var invalidChars =
                    new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

                saveFileDialog.FileName = string.Join(
                        "_",
                        FileToSaveName.Split(invalidChars.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
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
                EnvironmentController.dataPixelStorage.SavePixelStorage(fileName);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);

                return;
            }

            SavedConfigNoticeVisibility = Visibility.Visible;
            SavedConfigNotice = $"File {saveFileDialog.FileName} was saved.";
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

            OpenedLoadConfig = openFileDialog.FileName;
            OpenedLoadConfigVisibility = Visibility.Visible;
            EnvironmentController.dataPixelStorage.LoadPixelStorageToEnvironment(OpenedLoadConfig);
        }


        #endregion Config
    }
}
