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
        private IEnumerable<DataColor> _colorsData;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            var random = new Random();
            var colorCollection = new List<DataColor>();

            for (int i = 0; i < 32; i++)
                colorCollection.Add(
                    new DataColor
                    {
                        Color = new SolidColorBrush(
                            Color.FromArgb(
                                (byte)200,
                                (byte)random.Next(0, 250),
                                (byte)random.Next(0, 250),
                                (byte)random.Next(0, 250)
                            )
                        )
                    }
                );

            ColorsData = colorCollection;

            _isInitialized = true;
        }


        /// <summary> Selected config item in the list </summary>
        StateItem? currentState;

        public void TreeItemChanged(StateItem? stateItem)
        {
            // Load selected config
            currentState = stateItem;
            var statePreviewMode = StatesController.currentStatePreviewMode;

            if (stateItem == null)
            {
                Debug.WriteLine("State nullified.");
                //EnvironmentController.currentStateFullPath = string.Empty;
                return;
            }

            using DMIFile fileDmi = new DMIFile(stateItem.FilePath);
            var states = fileDmi.States;
            DMIState? state = null;
            foreach (DMIState item in states)
            {
                if (item.Name != stateItem.StateName)
                    continue;
                state = item;
            }
            if (state == null)
                return;

            //switch (ListViewPreviewSelectionMode)
            //{
            //    case SelectionMode.Single:
            //        EnvironmentController.dataImageState.ReplaceDMIState(state, statePreviewMode);
            //        break;
            //    case SelectionMode.Multiple:
            //        EnvironmentController.dataImageState.CombineDMIState(state, statePreviewMode);
            //        break;
            //    default:
            //        break;
            //}
            Debug.WriteLine($"State Changed to: {stateItem.FileName} {stateItem.StateName}");
        }
    }
}
