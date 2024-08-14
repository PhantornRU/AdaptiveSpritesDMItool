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
using AdaptiveSpritesDMItool.Helpers;
using System.Drawing;
using CommunityToolkit.HighPerformance.Helpers;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для StatesEditorPage.xaml
    /// </summary>
    public partial class StatesEditorPage : INavigableView<StatesEditorViewModel>
    {
        public StatesEditorViewModel ViewModel { get; }
        DataImageState dataImageState;

        StateEditType currentStateEditMode = StateEditType.Single;
        StateQuantityType currentStateQuantityMode = StateQuantityType.Single;

        /// <summary>
        /// Determines whether the state is centralized - setting the pixel in the middle of the pixel
        /// </summary>
        bool isCentralizedState = true;
        bool isMirroredState = true;
        bool isShowGrid = true;
        int borderThickness = 2;

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

            InitializeDictionaries();

            TestFunction();
        }


        private Dictionary<StateDirection, System.Windows.Controls.Image> stateSourceOrigDictionary = new Dictionary<StateDirection, System.Windows.Controls.Image>();
        private Dictionary<StateDirection, System.Windows.Controls.Image> stateSourceEditDictionary = new Dictionary<StateDirection, System.Windows.Controls.Image>();

        private void InitializeDictionaries()
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

        private void TestFunction()
        {
            MakeGrid(backgroundImg);
        }

        #region User Controller


        System.Drawing.Point currentMouseDownPosition;
        System.Drawing.Point currentMousePosition;
        System.Drawing.Point currentMouseUpPosition;

        StateDirection currentStateDirection;

        private void state_MouseDown(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            currentMouseDownPosition = MouseController.GetModifyMousePosition(e, stateSourceEditDictionary[_stateDirection]);
            currentMousePosition = currentMouseDownPosition;
            currentStateDirection = _stateDirection;

            switch (currentStateEditMode)
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
            currentMouseUpPosition = MouseController.GetModifyMousePosition(e, stateSourceEditDictionary[_stateDirection]);
            currentMousePosition = currentMouseUpPosition;
            currentStateDirection = _stateDirection;

            switch (currentStateEditMode)
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
            currentMousePosition = MouseController.GetModifyMousePosition(e, stateSourceEditDictionary[_stateDirection]);
            currentStateDirection = _stateDirection;

            switch (currentStateEditMode)
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
            SetPixel();
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


        private void SetPixel()
        {
            System.Drawing.Point mousePos = currentMousePosition;
            System.Windows.Media.Color color = GetColorModify();

            var stateDirections = GetStateDirections(mousePos);

            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmap = dataImageState.GetBMPstate(stateDirectionToModify, true);
                int bitmapWidth = (int)bitmap.Width;
                //bitmapWidth = _stateDirection == stateDirectionToModify ? 0 : bitmapWidth;
                var mousePosXTemp = mousePos.X;
                mousePos.X = CorrectMousePositionX(stateDirectionToModify, mousePos.X, bitmapWidth);
                Debug.WriteLine($"_stateDirection: {currentStateDirection}; stateDirectionToModify: {stateDirectionToModify}; MousePosX: [Orig: {mousePosXTemp} - Mod: {mousePos.X}]");
                bitmap.SetPixel(mousePos.X, mousePos.Y, color);
            }
        }

        private IEnumerable<StateDirection> GetStateDirections(System.Drawing.Point _mousePos)
        {
            switch (currentStateQuantityMode)
            {
                case StateQuantityType.Single:
                    return new[] { currentStateDirection };

                case StateQuantityType.Parallel:
                    return GetParallelStates(currentStateDirection);

                case StateQuantityType.All:
                    int parallValue = ((int)currentStateDirection / 2 == 1) ? -2 : 2;
                    return GetParallelStates(currentStateDirection).Union(GetParallelStates((StateDirection)((int)currentStateDirection + parallValue)));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private StateDirection[] GetParallelStates(StateDirection _stateDirection)
        {
            int parallValue = isStateOpposite(_stateDirection) ? 1 : -1;
            StateDirection parallelState = _stateDirection + parallValue;
            return new[] { _stateDirection, parallelState };
        }

        private int CorrectMousePositionX(StateDirection _stateDirection, int _mouseX, int _bitmapWidth)
        {
            if (!isMirroredState || currentStateDirection == _stateDirection)
            {
                return _mouseX;
            }
            //_bitmapWidth = isStateOpposite(_stateDirection) ? 0 : _bitmapWidth;
            var additionValueX = isCentralizedState ? -1 : 0;
            int result = _bitmapWidth - _mouseX - 1 + additionValueX; 
            result = Math.Max(result, 0);
            result = Math.Min(result, _bitmapWidth - 1);
            return result;

        }

        private bool isStateOpposite(StateDirection _stateDirection)
        {
            return ((int)_stateDirection % 2 == 0);
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
            currentStateEditMode = StateEditType.Single;
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            FillButton.Appearance = ControlAppearance.Primary;

            currentStateEditMode = StateEditType.Fill;
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            PickButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditType.Pick;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            DeleteButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditType.Delete;
        }

        #endregion Buttons Edit Controller


        #region Buttons Move Controller

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            SelectButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditType.Select;
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            MoveButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditType.Move;
        }

        #endregion Buttons Move Controller


        #region Buttons States Controller

        private void ChooseSingleStateButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseSingleStateButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityType.Single;
            ControllButtonsAvailability();
        }

        private void ChooseParallelStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseParallelStatesButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityType.Parallel;
            ControllButtonsAvailability();
        }

        private void ChooseAllStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseAllStatesButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityType.All;
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
            MirrorStatesButton.IsEnabled = currentStateQuantityMode != StateQuantityType.Single;
            CentralizeStatesButton.IsEnabled = isMirroredState && (currentStateQuantityMode != StateQuantityType.Single);

        }

        #endregion Buttons States Controller

        #region Buttons Environment Controller

        private void GridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            isShowGrid = !isShowGrid;
            GridEnvironmentButton.Appearance = isShowGrid ? ControlAppearance.Primary : ControlAppearance.Secondary;

            backgroundImg.Visibility = isShowGrid ? Visibility.Visible : Visibility.Collapsed;
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



        #region Editor

        private void MakeGrid(System.Windows.Controls.Image _img)
        {
            int pixelSize = 8;
            WriteableBitmap bitmap = new WriteableBitmap(257, 257, pixelSize, pixelSize, PixelFormats.Bgra32, null);

            System.Windows.Media.Color colorTemp = System.Windows.Media.Colors.Black;
            byte A = 100;
            System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(A, colorTemp.R, colorTemp.G, colorTemp.B);

            for (int i = 0; i < bitmap.PixelWidth; i += pixelSize)
            {
                for (int j = 0; j < bitmap.PixelHeight; j += pixelSize)
                {
                    Debug.WriteLine($"{i}, {j} (int)bitmap.Width == {bitmap.Width}, (int)bitmap.Height == {bitmap.Height}");
                    bitmap.DrawLine(i, j, i, bitmap.PixelHeight - j, color);
                    bitmap.DrawLine(j, i, bitmap.PixelWidth - j, i, color);
                }
            }

            for (int i = 0; i < borderThickness; i++)
            {
                bitmap.DrawRectangle(0+i, 0+i, bitmap.PixelWidth - i - 1, bitmap.PixelHeight - i - 1, Colors.Black);
            }

            _img.Source = bitmap;
        }

        #endregion Editor
    }
}
