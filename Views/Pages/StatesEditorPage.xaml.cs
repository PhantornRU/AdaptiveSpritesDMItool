﻿using AdaptiveSpritesDMItool.ViewModels.Pages;
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
using AdaptiveSpritesDMItool.Helpers;
using System.Drawing;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для StatesEditorPage.xaml
    /// </summary>
    public partial class StatesEditorPage : INavigableView<StatesEditorViewModel>
    {
        public StatesEditorViewModel ViewModel { get; }
        DataImageState dataImageState;

        StateEditMode currentStateEditMode = StateEditMode.Single;
        StateQuantityMode currentStateQuantityMode = StateQuantityMode.Single;

        /// <summary>
        /// Determines whether the state is centralized - setting the pixel in the middle of the pixel
        /// </summary>
        bool isCentralizedState = true;
        bool isMirroredState = true;

        public StatesEditorPage(StatesEditorViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            ControllButtonsAvailability();



            // Load File

            string path = "TestImages";
            string fullpath = $"{path}/testBodyHuman.dmi";
            using DMIFile file = new DMIFile(fullpath);

            Debug.WriteLine($"Loaded {file}({file.States.Count}).");

            DMIState currentState = file.States.First();

            dataImageState = new DataImageState(currentState);

            TestFunction();
        }


        private Dictionary<StateDirection, System.Windows.Controls.Image> stateSourceOrigDictionary = new Dictionary<StateDirection, System.Windows.Controls.Image>();
        private Dictionary<StateDirection, System.Windows.Controls.Image> stateSourceEditDictionary = new Dictionary<StateDirection, System.Windows.Controls.Image>();

        private void TestFunction()
        {
            // Preview
            imageLeftPreviewSouth.Source = dataImageState.GetBMPstate(StateDirection.South, false);
            imageLeftPreviewNorth.Source = dataImageState.GetBMPstate(StateDirection.North, false);
            imageLeftPreviewEast.Source = dataImageState.GetBMPstate(StateDirection.East, false);
            imageLeftPreviewWest.Source = dataImageState.GetBMPstate(StateDirection.West, false);

            stateSourceOrigDictionary.Add(StateDirection.South, imageLeftPreviewSouth);
            stateSourceOrigDictionary.Add(StateDirection.North, imageLeftPreviewNorth);
            stateSourceOrigDictionary.Add(StateDirection.East, imageLeftPreviewEast);
            stateSourceOrigDictionary.Add(StateDirection.West, imageLeftPreviewWest);

            // Edit
            imageRightPreviewSouth.Source = dataImageState.GetBMPstate(StateDirection.South, true);
            imageRightPreviewNorth.Source = dataImageState.GetBMPstate(StateDirection.North, true);
            imageRightPreviewEast.Source = dataImageState.GetBMPstate(StateDirection.East, true);
            imageRightPreviewWest.Source = dataImageState.GetBMPstate(StateDirection.West, true);

            stateSourceEditDictionary.Add(StateDirection.South, imageRightPreviewSouth);
            stateSourceEditDictionary.Add(StateDirection.North, imageRightPreviewNorth);
            stateSourceEditDictionary.Add(StateDirection.East, imageRightPreviewEast);
            stateSourceEditDictionary.Add(StateDirection.West, imageRightPreviewWest);
        }

        #region User Controller


        System.Drawing.Point currentMouseDownPosition;
        System.Drawing.Point currentMousePosition;
        System.Drawing.Point currentMouseUpPosition;

        private void state_MouseDown(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            currentMouseDownPosition = MouseController.GetModifyMousePosition(e, stateSourceEditDictionary[_stateDirection]);
        }

        private void state_MouseMove(MouseEventArgs e, StateDirection _stateDirection)
        {
            bool mouseIsDown = System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed;
            if (!mouseIsDown)
                return;
            currentMousePosition = MouseController.GetModifyMousePosition(e, stateSourceEditDictionary[_stateDirection]);
            SetPixel(_stateDirection);
        }

        private void state_MouseUp(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            currentMouseUpPosition = MouseController.GetModifyMousePosition(e, stateSourceEditDictionary[_stateDirection]);

            SetPixel(_stateDirection);

            switch (currentStateEditMode)
            {
                case StateEditMode.Single:
                    EditSingleMode(e, _stateDirection);
                    break;
                case StateEditMode.Fill:
                    EditFillMode(e, _stateDirection);
                    break;
                case StateEditMode.Pick:
                    EditPickMode(e, _stateDirection);
                    break;
                case StateEditMode.Delete:
                    EditDeleteMode(e, _stateDirection);
                    break;
                case StateEditMode.Select:
                    EditSelectMode(e, _stateDirection);
                    break;
                case StateEditMode.Move:
                    EditMoveMode(e, _stateDirection);
                    break;
            }
        }

        private void EditSingleMode(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            SetPixel(_stateDirection);
        }

        private void EditFillMode(MouseButtonEventArgs e, StateDirection _stateDirection)
        {

        }

        private void EditPickMode(MouseButtonEventArgs e, StateDirection _stateDirection)
        {

        }

        private void EditDeleteMode(MouseButtonEventArgs e, StateDirection _stateDirection)
        {

        }

        private void EditSelectMode(MouseButtonEventArgs e, StateDirection _stateDirection)
        {

        }
        private void EditMoveMode(MouseButtonEventArgs e, StateDirection _stateDirection)
        {

        }


        private void SetPixel(StateDirection _stateDirection)
        {
            System.Drawing.Point mousePos = currentMousePosition;
            System.Windows.Media.Color color = GetColorModify();

            var stateDirections = GetStateDirections(_stateDirection, mousePos);

            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmap = dataImageState.GetBMPstate(stateDirectionToModify, true);
                mousePos.X = CorrectMousePositionX(stateDirectionToModify, mousePos.X, (int)bitmap.Width);
                bitmap.SetPixel(mousePos.X, mousePos.Y, color);
            }
        }

        private IEnumerable<StateDirection> GetStateDirections(StateDirection _stateDirection, System.Drawing.Point _mousePos)
        {
            switch (currentStateQuantityMode)
            {
                case StateQuantityMode.Single:
                    return new[] { _stateDirection };

                case StateQuantityMode.Parallel:
                    return new[] { _stateDirection, (StateDirection)(_stateDirection + 1) };

                case StateQuantityMode.All:
                    return Enum.GetValues(typeof(StateDirection))
                        .Cast<StateDirection>()
                        .Where(direction => stateSourceEditDictionary.ContainsKey(direction));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int CorrectMousePositionX(StateDirection stateDirection, int mouseX, int bitmapWidth)
        {
            if (!isMirroredState || ((int)stateDirection % 2 == 0))
            {
                return mouseX;
            }
            var additionValueX = isCentralizedState ? -1 : 0;
            int result = bitmapWidth - mouseX - 1 + additionValueX; 
            result = Math.Max(result, 0);
            result = Math.Min(result, bitmapWidth - 1);
            return result;

        }

        private System.Windows.Media.Color GetColorModify()
        {
            return System.Windows.Media.Colors.Red;
        }

        #endregion  User Controller

        #region Mouse Controller

        #region Mouse Buttons - South Preview

        private void imageRightPreviewSouth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(e, StateDirection.South);
        }

        private void imageRightPreviewSouth_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(e, StateDirection.South);
        }

        private void imageRightPreviewSouth_MouseUp(object sender, MouseButtonEventArgs e)
        {
            state_MouseUp(e, StateDirection.South);
        }

        #endregion Mouse Buttons - South Preview

        #region Mouse Buttons - North Preview

        private void imageRightPreviewNorth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(e, StateDirection.North);
        }

        private void imageRightPreviewNorth_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(e, StateDirection.North);
        }

        private void imageRightPreviewNorth_MouseUp(object sender, MouseButtonEventArgs e)
        {

            state_MouseUp(e, StateDirection.North);
        }

        #endregion Mouse Buttons - North Preview


        #region Mouse Buttons - East Preview

        private void imageRightPreviewEast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(e, StateDirection.East);
        }

        private void imageRightPreviewEast_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(e, StateDirection.East);
        }

        private void imageRightPreviewEast_MouseUp(object sender, MouseButtonEventArgs e)
        {

            state_MouseUp(e, StateDirection.East);
        }

        #endregion Mouse Buttons - East Preview


        #region Mouse Buttons - West Preview

        private void imageRightPreviewWest_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(e, StateDirection.West);
        }

        private void imageRightPreviewWest_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(e, StateDirection.West);
        }

        private void imageRightPreviewWest_MouseUp(object sender, MouseButtonEventArgs e)
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
            currentStateEditMode = StateEditMode.Single;
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            FillButton.Appearance = ControlAppearance.Primary;

            currentStateEditMode = StateEditMode.Fill;
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            PickButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Pick;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            DeleteButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Delete;
        }

        #endregion Buttons Edit Controller


        #region Buttons Move Controller

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            SelectButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Select;
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            MoveButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Move;
        }

        #endregion Buttons Move Controller


        #region Buttons States Controller

        private void ChooseSingleStateButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseSingleStateButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityMode.Single;
            ControllButtonsAvailability();
        }

        private void ChooseParallelStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseParallelStatesButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityMode.Parallel;
            ControllButtonsAvailability();
        }

        private void ChooseAllStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseAllStatesButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityMode.All;
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
            MirrorStatesButton.IsEnabled = currentStateQuantityMode != StateQuantityMode.Single;
            CentralizeStatesButton.IsEnabled = isMirroredState && (currentStateQuantityMode != StateQuantityMode.Single);

        }

        #endregion Buttons States Controller

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
    }
}
