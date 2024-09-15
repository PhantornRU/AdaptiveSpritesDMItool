using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Models;
using DMISharp;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace AdaptiveSpritesDMItool.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private IEnumerable<StateItem> _statesData;

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


        //[ObservableProperty]
        //private Visibility _openedFolderPathVisibility = Visibility.Collapsed;

        //[ObservableProperty]
        //private string _openedFolderPath = string.Empty;

        //[RelayCommand]
        //public void OnOpenFolder()
        //{
        //    OpenedFolderPathVisibility = Visibility.Collapsed;

        //    OpenFolderDialog openFolderDialog =
        //        new()
        //        {
        //            Multiselect = true,
        //            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        //        };

        //    if (openFolderDialog.ShowDialog() != true)
        //    {
        //        return;
        //    }

        //    if (openFolderDialog.FolderNames.Length == 0)
        //    {
        //        return;
        //    }

        //    OpenedFolderPath = string.Join("\n", openFolderDialog.FolderNames);
        //    OpenedFolderPathVisibility = Visibility.Visible;
        //}

    }
}
