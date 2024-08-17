using AdaptiveSpritesDMItool.ViewModels.Pages;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using DMISharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using System.Windows.Media.Imaging;
using System.IO;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Controllers;
using System.Windows.Controls;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для StatesEditorPage.xaml
    /// </summary>
    public partial class StatesEditorPage : INavigableView<StatesEditorViewModel>
    {
        public StatesEditorViewModel ViewModel { get; }

        public StatesEditorPage(StatesEditorViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            // Pre Initializers

            InitializeComponent();

            // Load Files
            EnvironmentController.LoadEnvironment();

            // Post Initializers
            InitializeDictionaries();
            StatesController.InitializeSources();
            StatesController.InitializeGrids();

            ControllButtonsAvailability();

            TestFunction();
        }

        private void TestFunction()
        {

        }


        private void InitializeDictionaries()
        {
            StatesController.stateSourceDictionary = new Dictionary<StateDirection, Dictionary<StateImageType, System.Windows.Controls.Image>>()
            {
                { StateDirection.South, new Dictionary<StateImageType, System.Windows.Controls.Image>()
                    {
                        { StateImageType.Left, imagePreviewLeftSouth },
                        { StateImageType.Right, imagePreviewRightSouth },
                        { StateImageType.BackgroundLeft, imageBackgroundPreviewLeftSouth },
                        { StateImageType.BackgroundRight, imageBackgroundPreviewRightSouth },
                        { StateImageType.OverlayLeft, imageOverlayPreviewLeftSouth },
                        { StateImageType.OverlayRight, imageOverlayPreviewRightSouth },
                        { StateImageType.SelectionLeft, imageSelectionPreviewLeftSouth },
                        { StateImageType.SelectionRight, imageSelectionPreviewRightSouth }
                    }
                },

                { StateDirection.North, new Dictionary<StateImageType, System.Windows.Controls.Image>()
                    {
                        { StateImageType.Left, imagePreviewLeftNorth },
                        { StateImageType.Right, imagePreviewRightNorth },
                        { StateImageType.BackgroundLeft, imageBackgroundPreviewLeftNorth },
                        { StateImageType.BackgroundRight, imageBackgroundPreviewRightNorth },
                        { StateImageType.OverlayLeft, imageOverlayPreviewLeftNorth },
                        { StateImageType.OverlayRight, imageOverlayPreviewRightNorth },
                        { StateImageType.SelectionLeft, imageSelectionPreviewLeftNorth },
                        { StateImageType.SelectionRight, imageSelectionPreviewRightNorth }
                    }
                },

                { StateDirection.East, new Dictionary<StateImageType, System.Windows.Controls.Image>()
                    {
                        { StateImageType.Left, imagePreviewLeftEast },
                        { StateImageType.Right, imagePreviewRightEast },
                        { StateImageType.BackgroundLeft, imageBackgroundPreviewLeftEast },
                        { StateImageType.BackgroundRight, imageBackgroundPreviewRightEast },
                        { StateImageType.OverlayLeft, imageOverlayPreviewLeftEast },
                        { StateImageType.OverlayRight, imageOverlayPreviewRightEast },
                        { StateImageType.SelectionLeft, imageSelectionPreviewLeftEast },
                        { StateImageType.SelectionRight, imageSelectionPreviewRightEast }
                    }
                },

                { StateDirection.West, new Dictionary<StateImageType, System.Windows.Controls.Image>()
                    {
                        { StateImageType.Left, imagePreviewLeftWest },
                        { StateImageType.Right, imagePreviewRightWest },
                        { StateImageType.BackgroundLeft, imageBackgroundPreviewLeftWest },
                        { StateImageType.BackgroundRight, imageBackgroundPreviewRightWest },
                        { StateImageType.OverlayLeft, imageOverlayPreviewLeftWest },
                        { StateImageType.OverlayRight, imageOverlayPreviewRightWest },
                        { StateImageType.SelectionLeft, imageSelectionPreviewLeftWest },
                        { StateImageType.SelectionRight, imageSelectionPreviewRightWest }
                    }
                }
            };

        }

        #region Mouse Controller

        #region Mouse Buttons - South Preview

        private void imagePreviewRightSouth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EditorController.state_MouseDown(e, StateDirection.South);
        }

        private void imagePreviewRightSouth_MouseMove(object sender, MouseEventArgs e)
        {
            EditorController.state_MouseMove(e, StateDirection.South);
        }

        private void imagePreviewRightSouth_MouseUp(object sender, MouseButtonEventArgs e)
        {
            EditorController.state_MouseUp(e, StateDirection.South);
        }

        #endregion Mouse Buttons - South Preview

        #region Mouse Buttons - North Preview

        private void imagePreviewRightNorth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EditorController.state_MouseDown(e, StateDirection.North);
        }

        private void imagePreviewRightNorth_MouseMove(object sender, MouseEventArgs e)
        {
            EditorController.state_MouseMove(e, StateDirection.North);
        }

        private void imagePreviewRightNorth_MouseUp(object sender, MouseButtonEventArgs e)
        {

            EditorController.state_MouseUp(e, StateDirection.North);
        }

        #endregion Mouse Buttons - North Preview


        #region Mouse Buttons - East Preview

        private void imagePreviewRightEast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EditorController.state_MouseDown(e, StateDirection.East);
        }

        private void imagePreviewRightEast_MouseMove(object sender, MouseEventArgs e)
        {
            EditorController.state_MouseMove(e, StateDirection.East);
        }

        private void imagePreviewRightEast_MouseUp(object sender, MouseButtonEventArgs e)
        {

            EditorController.state_MouseUp(e, StateDirection.East);
        }

        #endregion Mouse Buttons - East Preview


        #region Mouse Buttons - West Preview

        private void imagePreviewRightWest_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EditorController.state_MouseDown(e, StateDirection.West);
        }

        private void imagePreviewRightWest_MouseMove(object sender, MouseEventArgs e)
        {
            EditorController.state_MouseMove(e, StateDirection.West);
        }

        private void imagePreviewRightWest_MouseUp(object sender, MouseButtonEventArgs e)
        {
            EditorController.state_MouseUp(e, StateDirection.West);
        }

        #endregion Mouse Buttons - West Preview

        #endregion Mouse Controller

        #region Buttons Controller

        #region Buttons Edit Controller

        private void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            SingleButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Single);
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            FillButton.Appearance = StatesController.GetPressedButtonAppearance();

            StatesController.SetCurrentStateEditMode(StateEditType.Fill);
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            PickButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Pick);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            DeleteButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Delete);
        }

        #endregion Buttons Edit Controller


        #region Buttons Move Controller

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            SelectButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Select);
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            MoveButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Move);
        }

        #endregion Buttons Move Controller


        #region Buttons States Controller

        private void ChooseSingleStateButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseSingleStateButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.Single);
            ControllButtonsAvailability();
        }

        private void ChooseParallelStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseParallelStatesButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.Parallel);
            ControllButtonsAvailability();
        }

        private void ChooseAllStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseAllStatesButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.All);
            ControllButtonsAvailability();
        }

        private void CentralizeStatesButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleCentralizedState();
            CentralizeStatesButton.Appearance = StatesController.GetControlAppearanceCentralize();
        }

        private void MirrorStatesButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleMirroredState();
            MirrorStatesButton.Appearance = StatesController.GetControlAppearanceMirror();
            ControllButtonsAvailability();
        }

        private void ControllButtonsAvailability()
        {
            MirrorStatesButton.IsEnabled = StatesController.GetEnableStateMirrorButton();
            CentralizeStatesButton.IsEnabled = StatesController.GetEnableStateCentralizeButton();
            GridZIndexEnvironmentButton.IsEnabled = StatesController.GetEnableStateGridZIndexButton();
            GridZIndexEnvironmentUpdate();
        }

        #endregion Buttons States Controller

        #region Buttons Environment Controller

        private void GridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleShowGrid();
            GridEnvironmentUpdate();
        }

        private void GridEnvironmentUpdate()
        {
            GridEnvironmentButton.Appearance = StatesController.GetControlAppearanceGrid();
            GridZIndexEnvironmentButton.IsEnabled = StatesController.GetEnableStateGridZIndexButton();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                state[StateImageType.BackgroundLeft].Visibility = StatesController.GetVisibilityGrid();
                state[StateImageType.BackgroundRight].Visibility = StatesController.GetVisibilityGrid();
            }
        }

        private void GridZIndexEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleShowAboveGrid();
            GridZIndexEnvironmentUpdate();
        }

        private void GridZIndexEnvironmentUpdate()
        {
            GridZIndexEnvironmentButton.Appearance = StatesController.GetControlAppearanceGridZIndex();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                Panel.SetZIndex(state[StateImageType.BackgroundLeft], StatesController.GetBackgroundZIndex());
                Panel.SetZIndex(state[StateImageType.BackgroundRight], StatesController.GetBackgroundZIndex());
            }
        }

        private void OverlayEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleShowOverlay();
            OverlayEnvironmentButton.Appearance = StatesController.GetControlAppearanceOverlay();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                state[StateImageType.OverlayLeft].Visibility = StatesController.GetVisibilityOverlay();
                state[StateImageType.OverlayRight].Visibility = StatesController.GetVisibilityOverlay();
            }
        }

        #endregion Buttons Environment Controller

        private void ResetEditButtons()
        {
            SingleButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            FillButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            PickButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            DeleteButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            SelectButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            MoveButton.Appearance = StatesController.GetUnPressedButtonAppearance();
        }

        private void ResetStatesButtons()
        {
            ChooseSingleStateButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            ChooseParallelStatesButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            ChooseAllStatesButton.Appearance = StatesController.GetUnPressedButtonAppearance();
        }

        #endregion Buttons Controller

    }
}
