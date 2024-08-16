using AdaptiveSpritesDMItool.ViewModels.Pages;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using DMISharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Reflection;
using SixLabors.ImageSharp.Formats.Bmp;
using System.Runtime.CompilerServices;
using System.IO;
using AdaptiveSpritesDMItool.Models;
using System.Drawing;
using CommunityToolkit.HighPerformance.Helpers;
using AdaptiveSpritesDMItool.Controllers;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для StatesEditorPage.xaml
    /// </summary>
    public partial class StatesEditorPage : INavigableView<StatesEditorViewModel>
    {
        public StatesEditorViewModel ViewModel { get; }

        /// <summary>
        /// Determines whether the state is centralized - setting the pixel in the middle of the pixel
        /// </summary>
        bool isCentralizedState = true;
        bool isMirroredState = true;
        bool isShowGrid = true;
        bool isShowOverlay = true;

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
            InitializeSources();
            InitializeGrids();

            ControllButtonsAvailability();

            TestFunction();
        }

        private Dictionary<StateDirection, Dictionary<StateImageType, System.Windows.Controls.Image>> stateSourceDictionary;
        
        private void InitializeDictionaries()
        {
            stateSourceDictionary = new Dictionary<StateDirection, Dictionary<StateImageType, System.Windows.Controls.Image>>()
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

        private void InitializeSources()
        {
            foreach (var (stateDirection, images) in stateSourceDictionary)
            {
                images[StateImageType.Left].Source = EnvironmentController.GetEnvironmentImage(stateDirection, false);
                images[StateImageType.Right].Source = EnvironmentController.GetEnvironmentImage(stateDirection, true);
            }

            foreach (var (stateDirection, images) in stateSourceDictionary)
            {
                images[StateImageType.OverlayLeft].Source = EnvironmentController.GetEnvironmentImageOverlay(stateDirection, false);
                images[StateImageType.OverlayRight].Source = EnvironmentController.GetEnvironmentImageOverlay(stateDirection, true);
            }
        }

        private void TestFunction()
        {

        }

        #region User Controller

        private void state_MouseDown(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            var stateImage = stateSourceDictionary[_stateDirection][StateImageType.Right];
            EditorController.currentMouseDownPosition = MouseController.GetModifyMousePosition(e, stateImage);
            EditorController.currentMousePosition = EditorController.currentMouseDownPosition;
            EditorController.currentStateDirection = _stateDirection;

            switch (EditorController.currentStateEditMode)
            {
                case StateEditType.Single:
                    EditSingleMode();
                    break;
                case StateEditType.Fill:
                    EditFillModeStart();
                    break;
                case StateEditType.Pick:
                    EditPickMode();
                    break;
                case StateEditType.Delete:
                    EditDeleteMode();
                    break;
                case StateEditType.Select:
                    EditSelectModeStart();
                    break;
                case StateEditType.Move:
                    EditMoveMode();
                    break;
            }
        }

        private void state_MouseUp(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            var stateImage = stateSourceDictionary[_stateDirection][StateImageType.Right];
            EditorController.currentMouseUpPosition = MouseController.GetModifyMousePosition(e, stateImage);
            EditorController.currentMousePosition = EditorController.currentMouseUpPosition;
            EditorController.currentStateDirection = _stateDirection;

            switch (EditorController.currentStateEditMode)
            {
                case StateEditType.Fill:
                    EditFillModeEnd();
                    break;
                case StateEditType.Select:
                    EditSelectModeEnd();
                    break;
            }

        }

        private void state_MouseMove(MouseEventArgs e, StateDirection _stateDirection)
        {

            bool mouseIsDown = System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed;
            if (!mouseIsDown)
                return;
            var stateImage = stateSourceDictionary[_stateDirection][StateImageType.Right];
            EditorController.currentMousePosition = MouseController.GetModifyMousePosition(e, stateImage);
            EditorController.currentStateDirection = _stateDirection;

            switch (EditorController.currentStateEditMode)
            {
                case StateEditType.Single:
                    EditSingleMode( );
                    break;
                case StateEditType.Fill:
                    EditFillMode();
                    break;
                case StateEditType.Pick:
                    EditPickMode();
                    break;
                case StateEditType.Delete:
                    EditDeleteMode();
                    break;
                case StateEditType.Select:
                    EditSelectMode();
                    break;
                case StateEditType.Move:
                    EditMoveMode();
                    break;
            }
        }

        private void EditSingleMode()
        {
            EditorController.SetPixel(isCentralizedState, isMirroredState);
        }

        private void EditFillModeStart()
        {

        }

        private void EditFillMode()
        {

        }

        private void EditFillModeEnd()
        {

        }


        private void EditPickMode()
        {

        }

        private void EditDeleteMode()
        {

        }

        private void EditSelectModeStart()
        {

        }

        private void EditSelectMode()
        {

        }
        private void EditSelectModeEnd()
        {

        }

        private void EditMoveMode()
        {

        }

        #endregion  User Controller

        #region Mouse Controller

        #region Mouse Buttons - South Preview

        private void imagePreviewRightSouth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(e, StateDirection.South);
        }

        private void imagePreviewRightSouth_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(e, StateDirection.South);
        }

        private void imagePreviewRightSouth_MouseUp(object sender, MouseButtonEventArgs e)
        {
            state_MouseUp(e, StateDirection.South);
        }

        #endregion Mouse Buttons - South Preview

        #region Mouse Buttons - North Preview

        private void imagePreviewRightNorth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(e, StateDirection.North);
        }

        private void imagePreviewRightNorth_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(e, StateDirection.North);
        }

        private void imagePreviewRightNorth_MouseUp(object sender, MouseButtonEventArgs e)
        {

            state_MouseUp(e, StateDirection.North);
        }

        #endregion Mouse Buttons - North Preview


        #region Mouse Buttons - East Preview

        private void imagePreviewRightEast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(e, StateDirection.East);
        }

        private void imagePreviewRightEast_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(e, StateDirection.East);
        }

        private void imagePreviewRightEast_MouseUp(object sender, MouseButtonEventArgs e)
        {

            state_MouseUp(e, StateDirection.East);
        }

        #endregion Mouse Buttons - East Preview


        #region Mouse Buttons - West Preview

        private void imagePreviewRightWest_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(e, StateDirection.West);
        }

        private void imagePreviewRightWest_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(e, StateDirection.West);
        }

        private void imagePreviewRightWest_MouseUp(object sender, MouseButtonEventArgs e)
        {
            state_MouseUp(e, StateDirection.West);
        }

        #endregion Mouse Buttons - West Preview

        #endregion Mouse Controller

        #region Buttons Controller

        #region Buttons Edit Controller

        private void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            SingleButton.Appearance = ControlAppearance.Primary;
            EditorController.currentStateEditMode = StateEditType.Single;
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            FillButton.Appearance = ControlAppearance.Primary;

            EditorController.currentStateEditMode = StateEditType.Fill;
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            PickButton.Appearance = ControlAppearance.Primary;
            EditorController.currentStateEditMode = StateEditType.Pick;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            DeleteButton.Appearance = ControlAppearance.Primary;
            EditorController.currentStateEditMode = StateEditType.Delete;
        }

        #endregion Buttons Edit Controller


        #region Buttons Move Controller

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            SelectButton.Appearance = ControlAppearance.Primary;
            EditorController.currentStateEditMode = StateEditType.Select;
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            MoveButton.Appearance = ControlAppearance.Primary;
            EditorController.currentStateEditMode = StateEditType.Move;
        }

        #endregion Buttons Move Controller


        #region Buttons States Controller

        private void ChooseSingleStateButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseSingleStateButton.Appearance = ControlAppearance.Primary;
            EditorController.currentStateQuantityMode = StateQuantityType.Single;
            ControllButtonsAvailability();
        }

        private void ChooseParallelStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseParallelStatesButton.Appearance = ControlAppearance.Primary;
            EditorController.currentStateQuantityMode = StateQuantityType.Parallel;
            ControllButtonsAvailability();
        }

        private void ChooseAllStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseAllStatesButton.Appearance = ControlAppearance.Primary;
            EditorController.currentStateQuantityMode = StateQuantityType.All;
            ControllButtonsAvailability();
        }

        private void CentralizeStatesButton_Click(object sender, RoutedEventArgs e)
        {
            isCentralizedState = !isCentralizedState;
            CentralizeStatesButton.Appearance = isCentralizedState ? ControlAppearance.Primary : ControlAppearance.Secondary;
        }

        private void MirrorStatesButton_Click(object sender, RoutedEventArgs e)
        {
            isMirroredState = !isMirroredState;
            MirrorStatesButton.Appearance = isMirroredState ? ControlAppearance.Primary : ControlAppearance.Secondary;
            ControllButtonsAvailability();
        }

        private void ControllButtonsAvailability()
        {
            MirrorStatesButton.IsEnabled = EditorController.currentStateQuantityMode != StateQuantityType.Single;
            CentralizeStatesButton.IsEnabled = isMirroredState && (EditorController.currentStateQuantityMode != StateQuantityType.Single);

        }

        #endregion Buttons States Controller

        #region Buttons Environment Controller

        private void GridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            isShowGrid = !isShowGrid;
            GridEnvironmentButton.Appearance = isShowGrid ? ControlAppearance.Primary : ControlAppearance.Secondary;

            foreach (var state in stateSourceDictionary.Values)
            {
                state[StateImageType.BackgroundLeft].Visibility = isShowGrid ? Visibility.Visible : Visibility.Collapsed;
                state[StateImageType.BackgroundRight].Visibility = isShowGrid ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OverlayEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            isShowOverlay = !isShowOverlay;
            OverlayEnvironmentButton.Appearance = isShowOverlay ? ControlAppearance.Primary : ControlAppearance.Secondary;

            foreach (var state in stateSourceDictionary.Values)
            {
                state[StateImageType.OverlayLeft].Visibility = isShowOverlay ? Visibility.Visible : Visibility.Collapsed;
                state[StateImageType.OverlayRight].Visibility = isShowOverlay ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion Buttons Environment Controller

        private void ResetEditButtons()
        {
            SingleButton.Appearance = ControlAppearance.Secondary;
            FillButton.Appearance = ControlAppearance.Secondary;
            PickButton.Appearance = ControlAppearance.Secondary;
            DeleteButton.Appearance = ControlAppearance.Secondary;
            SelectButton.Appearance = ControlAppearance.Secondary;
            MoveButton.Appearance = ControlAppearance.Secondary;
        }

        private void ResetStatesButtons()
        {
            ChooseSingleStateButton.Appearance = ControlAppearance.Secondary;
            ChooseParallelStatesButton.Appearance = ControlAppearance.Secondary;
            ChooseAllStatesButton.Appearance = ControlAppearance.Secondary;
        }

        #endregion Buttons Controller

        #region Editor Controller

        private void InitializeGrids()
        {
            // !!!!!!! Допиши код определяющий pixelSize зависимый от pixelResolution. Например при pixelResolution = 64 pixelSize = 4, а при 32 будет 8, при 16 будет 16

            int pixelResolution = 32;
            int pixelSize = 8;

            // TODO: Make Better File Way
            string gridBitmapPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Resources", $"grid{pixelResolution}.png");
            WriteableBitmap gridBitmap;

            if (!File.Exists(gridBitmapPath))
            {
                gridBitmap = EditorController.MakeAndGetGrid(pixelSize: pixelSize);

                // Save the bitmap into a file.
                using (FileStream stream =
                    new FileStream(gridBitmapPath, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(gridBitmap));
                    encoder.Save(stream);
                }
            }
            else
            {
                BitmapImage bitmap = new BitmapImage(new Uri(gridBitmapPath, UriKind.Relative));
                gridBitmap = new WriteableBitmap(bitmap);
            }

            foreach (var state in stateSourceDictionary.Values)
            {
                state[StateImageType.BackgroundLeft].Source = gridBitmap;
                state[StateImageType.BackgroundRight].Source = gridBitmap;
            }
        }

        #endregion Editor
    }
}
