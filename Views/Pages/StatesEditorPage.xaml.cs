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




        private void TestFunction()
        {

            // Preview
            imageLeftPreview.Source = dataImageState.GetBMPstate(StateDirection.South);
            imageState1.Source = dataImageState.GetBMPstate(StateDirection.North);
            imageState2.Source = dataImageState.GetBMPstate(StateDirection.East);
            imageState3.Source = dataImageState.GetBMPstate(StateDirection.West);


            // Edit
            imageRightPreview.Source = dataImageState.GetBMPstate(StateDirection.South, true);
            imageState4.Source = dataImageState.GetBMPstate(StateDirection.North, true);
            imageState5.Source = dataImageState.GetBMPstate(StateDirection.East, true);
            imageState6.Source = dataImageState.GetBMPstate(StateDirection.West, true);
        }

        #region Mouse Controller

        private void imageRightPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void imageRightPreview_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void imageRightPreview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            System.Drawing.Point mousePos = GetMouseCoordinates(e, imageRightPreview);
            Debug.WriteLine(mousePos);

            // Test
            dataImageState.GetBMPstate(StateDirection.South, true).SetPixel(mousePos.X, mousePos.Y, Colors.Red);
            dataImageState.GetBMPstate(StateDirection.North, true).SetPixel(mousePos.X, mousePos.Y, Colors.Red);
            dataImageState.GetBMPstate(StateDirection.East, true).SetPixel(mousePos.X, mousePos.Y, Colors.Red);
            dataImageState.GetBMPstate(StateDirection.West, true).SetPixel(mousePos.X, mousePos.Y, Colors.Red);
        }

        private System.Drawing.Point GetMouseCoordinates(System.Windows.Input.MouseButtonEventArgs _e, System.Windows.Controls.Image _img)
        {
            System.Windows.Point pos = _e.GetPosition(_img);
            int x = (int)Math.Floor(pos.X * _img.Source.Width / _img.ActualWidth);
            int y = (int)Math.Floor(pos.Y * _img.Source.Height / _img.ActualHeight);
            return new System.Drawing.Point(x, y);

        }
        #endregion


        #region User Controller


        #endregion



        

    }
}
