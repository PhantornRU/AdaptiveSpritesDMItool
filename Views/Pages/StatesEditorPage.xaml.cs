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

        public StatesEditorPage(StatesEditorViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();



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

        private void state_MouseDown(object sender, MouseButtonEventArgs e, StateDirection _stateDirection)
        {

        }

        private void state_MouseMove(object sender, MouseEventArgs e, StateDirection _stateDirection)
        {

        }

        private void state_MouseUp(object sender, MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            SetPixel(e, _stateDirection);

            switch (currentStateQuantityMode)
            {
                case StateQuantityMode.Single:
                    break;
                case StateQuantityMode.Parallel:
                    break;
                case StateQuantityMode.All:
                    break;
            }

            switch (currentStateEditMode)
            {
                case StateEditMode.Single:
                    break;
                case StateEditMode.Fill:
                    break;
                case StateEditMode.Pick:
                    break;
                case StateEditMode.Delete:
                    break;
                case StateEditMode.Select:
                    break;
                case StateEditMode.Move:
                    break;
            }


        }

        private void SetPixel(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            System.Windows.Media.Color color = System.Windows.Media.Colors.Red;
            System.Drawing.Point mousePos = GetMouseCoordinates(e, stateSourceEditDictionary[_stateDirection]);
            Debug.WriteLine(mousePos);
            dataImageState.GetBMPstate(_stateDirection, true).SetPixel(mousePos.X, mousePos.Y, color);
        }

        #endregion  User Controller

        #region Mouse Controller

        private System.Drawing.Point GetMouseCoordinates(System.Windows.Input.MouseButtonEventArgs _e, System.Windows.Controls.Image _img)
        {
            System.Windows.Point pos = _e.GetPosition(_img);
            int x = (int)Math.Floor(pos.X * _img.Source.Width / _img.ActualWidth);
            int y = (int)Math.Floor(pos.Y * _img.Source.Height / _img.ActualHeight);
            return new System.Drawing.Point(x, y);

        }

        #region Mouse Buttons - South Preview

        private void imageRightPreviewSouth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(sender, e, StateDirection.South);
        }

        private void imageRightPreviewSouth_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(sender, e, StateDirection.South);
        }

        private void imageRightPreviewSouth_MouseUp(object sender, MouseButtonEventArgs e)
        {
            state_MouseUp(sender, e, StateDirection.South);
        }

        #endregion Mouse Buttons - South Preview


        #region Mouse Buttons - North Preview

        private void imageRightPreviewNorth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(sender, e, StateDirection.North);
        }

        private void imageRightPreviewNorth_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(sender, e, StateDirection.North);
        }

        private void imageRightPreviewNorth_MouseUp(object sender, MouseButtonEventArgs e)
        {

            state_MouseUp(sender, e, StateDirection.North);
        }

        #endregion Mouse Buttons - North Preview


        #region Mouse Buttons - East Preview

        private void imageRightPreviewEast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(sender, e, StateDirection.East);
        }

        private void imageRightPreviewEast_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(sender, e, StateDirection.East);
        }

        private void imageRightPreviewEast_MouseUp(object sender, MouseButtonEventArgs e)
        {

            state_MouseUp(sender, e, StateDirection.East);
        }

        #endregion Mouse Buttons - East Preview


        #region Mouse Buttons - West Preview

        private void imageRightPreviewWest_MouseDown(object sender, MouseButtonEventArgs e)
        {
            state_MouseDown(sender, e, StateDirection.West);
        }

        private void imageRightPreviewWest_MouseMove(object sender, MouseEventArgs e)
        {
            state_MouseMove(sender, e, StateDirection.West);
        }

        private void imageRightPreviewWest_MouseUp(object sender, MouseButtonEventArgs e)
        {
            state_MouseUp(sender, e, StateDirection.West);
        }

        #endregion Mouse Buttons - West Preview

        #endregion Mouse Controller

        #region Buttons Controller

        #region Buttons Edit Controller

        private void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditButtons();
            SingleButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Single;
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditButtons();
            FillButton.Appearance = ControlAppearance.Primary;

            currentStateEditMode = StateEditMode.Fill;
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditButtons();
            PickButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Pick;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditButtons();
            DeleteButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Delete;
        }

        #endregion Buttons Edit Controller


        #region Buttons Move Controller

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditButtons();
            SelectButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Select;
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditButtons();
            MoveButton.Appearance = ControlAppearance.Primary;
            currentStateEditMode = StateEditMode.Move;
        }

        #endregion Buttons Move Controller


        #region Buttons States Controller

        private void ChooseSingleStateButton_Click(object sender, RoutedEventArgs e)
        {
            ClearStatesButtons();
            ChooseSingleStateButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityMode.Single;
        }

        private void ChooseParallelStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ClearStatesButtons();
            ChooseParallelStatesButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityMode.Parallel;
        }

        private void ChooseAllStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ClearStatesButtons();
            ChooseAllStatesButton.Appearance = ControlAppearance.Primary;
            currentStateQuantityMode = StateQuantityMode.All;
        }

        #endregion Buttons States Controller

        private void ClearEditButtons()
        {
            SingleButton.Appearance = ControlAppearance.Secondary;
            FillButton.Appearance = ControlAppearance.Secondary;
            PickButton.Appearance = ControlAppearance.Secondary;
            DeleteButton.Appearance = ControlAppearance.Secondary;
            SelectButton.Appearance = ControlAppearance.Secondary;
            MoveButton.Appearance = ControlAppearance.Secondary;
        }

        private void ClearStatesButtons()
        {
            ChooseSingleStateButton.Appearance = ControlAppearance.Secondary;
            ChooseParallelStatesButton.Appearance = ControlAppearance.Secondary;
            ChooseAllStatesButton.Appearance = ControlAppearance.Secondary;
        }

        #endregion Buttons Controller

    }
}
