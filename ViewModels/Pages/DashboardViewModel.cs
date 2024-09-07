using AdaptiveSpritesDMItool.Helpers;
using AdaptiveSpritesDMItool.Models;
using DMISharp;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDMItool.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _counter = 0;

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }


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

        private static ObservableCollection<StateItem> GenerateStateItems()
        {
            var random = new Random();
            var StateItems = new ObservableCollection<StateItem>();

            var fileNames = new[]
            {
            "John",
            "Winston",
            "Adrianna",
            "Spencer",
            "Phoebe",
            "Lucas",
            "Carl",
            "Marissa",
            "Brandon",
            "Antoine",
            "Arielle",
            "Arielle",
            "Jamie",
            "Alexzander"
            };
            var stateNames = new[]
            {
            "Doe",
            "Tapia",
            "Cisneros",
            "Lynch",
            "Munoz",
            "Marsh",
            "Hudson",
            "Bartlett",
            "Gregory",
            "Banks",
            "Hood",
            "Fry",
            "Carroll"
            };

            // !!!! Test !!!!
            string path = "TestImages";
            string fullpath = $"{path}/testBodyHuman.dmi";
            using DMIFile file = new DMIFile(fullpath);
            DMIState currentState = file.States.First();

            StateDirection direction = StateDirection.South;
            BitmapSource bmp = ImageEncoder.GetBMPFromDMIState(currentState, direction);

            for (int i = 0; i < 50; i++)
            {
                StateItems.Add(
                    new StateItem(
                        fileNames[random.Next(0, fileNames.Length)],
                        stateNames[random.Next(0, stateNames.Length)],
                        bmp
                    // !!!!!!!!! БИТМАП ЗАГРУЗИТЬ !!!!!!

                    )
                );
            }

            return StateItems;
        }

        private void UpdateListViewSelectionMode(int selectionModeIndex)
        {
            ListViewSelectionMode = selectionModeIndex switch
            {
                1 => SelectionMode.Multiple,
                2 => SelectionMode.Extended,
                _ => SelectionMode.Single
            };
        }

        #endregion List View


        #region File Open

        [ObservableProperty]
        private Visibility _openedFilePathVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _openedFilePath = string.Empty;

        [ObservableProperty]
        private Visibility _openedPicturePathVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _openedPicturePath = string.Empty;

        [ObservableProperty]
        private Visibility _openedMultiplePathVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _openedMultiplePath = string.Empty;

        [ObservableProperty]
        private Visibility _openedFolderPathVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _openedFolderPath = string.Empty;

        [RelayCommand]
        public void OnOpenFile()
        {
            OpenedFilePathVisibility = Visibility.Collapsed;

            OpenFileDialog openFileDialog =
                new()
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Filter = "All files (*.*)|*.*"
                };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            if (!File.Exists(openFileDialog.FileName))
            {
                return;
            }

            OpenedFilePath = openFileDialog.FileName;
            OpenedFilePathVisibility = Visibility.Visible;
        }

        [RelayCommand]
        public void OnOpenPicture()
        {
            OpenedPicturePathVisibility = Visibility.Collapsed;

            OpenFileDialog openFileDialog =
                new()
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    //Filter = "Image files (*.bmp;*.jpg;*.jpeg;*.png)|*.bmp;*.jpg;*.jpeg;*.png|All files (*.*)|*.*"
                    Filter = "DMI file (*.dmi)|*.dmi;*.dmi"
                };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            if (!File.Exists(openFileDialog.FileName))
            {
                return;
            }

            OpenedPicturePath = openFileDialog.FileName;
            OpenedPicturePathVisibility = Visibility.Visible;
        }

        [RelayCommand]
        public void OnOpenMultiple()
        {
            OpenedMultiplePathVisibility = Visibility.Collapsed;

            OpenFileDialog openFileDialog =
                new()
                {
                    Multiselect = true,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
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

            OpenedMultiplePath = string.Join("\n", fileNames);
            OpenedMultiplePathVisibility = Visibility.Visible;
        }

        [RelayCommand]
        public void OnOpenFolder()
        {
#if NET8_0_OR_GREATER
        OpenedFolderPathVisibility = Visibility.Collapsed;

        OpenFolderDialog openFolderDialog =
            new()
            {
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

        if (openFolderDialog.ShowDialog() != true)
        {
            return;
        }

        if (openFolderDialog.FolderNames.Length == 0)
        {
            return;
        }

        OpenedFolderPath = string.Join("\n", openFolderDialog.FolderNames);
        OpenedFolderPathVisibility = Visibility.Visible;
#else
            OpenedFolderPath = "OpenFolderDialog requires .NET 8 or newer";
            OpenedFolderPathVisibility = Visibility.Visible;
#endif
        }


        #endregion File Open

    }
}
