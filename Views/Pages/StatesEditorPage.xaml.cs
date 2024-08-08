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

            InitializeComponent();

            TestFunction();
        }





        private void TestFunction()
        {

            string path = "TestImages";
            string fullpath = $"{path}/testBodyHuman.dmi";
            using DMIFile file = new DMIFile(fullpath);

            Debug.WriteLine($"Loaded {file}({file.States.Count}).");

            DMIState stateFirst = file.States.First();

            imageLeftPreview.Source = GetBMPFromDMIState(stateFirst, StateDirection.South);
            imageState1.Source = GetBMPFromDMIState(stateFirst, StateDirection.North);
            imageState2.Source = GetBMPFromDMIState(stateFirst, StateDirection.East);
            imageState3.Source = GetBMPFromDMIState(stateFirst, StateDirection.West);
        }

        private WriteableBitmap GetBMPFromDMIState(DMIState _state, StateDirection _stateDirection)
        {
            using Image<Rgba32>? imgState = _state.GetFrame(_stateDirection, 0);
            if (imgState is Image<Rgba32> valueOfImage)
                Console.WriteLine($"image state is {valueOfImage}");
            else
                Console.WriteLine("image state does not have a value");
            return GetBMPFromRGBA32(imgState);
        }

        private WriteableBitmap GetBMPFromRGBA32(Image<Rgba32> _imgState)
        {
            var bmp = new WriteableBitmap(_imgState.Width, _imgState.Height, _imgState.Metadata.HorizontalResolution, _imgState.Metadata.VerticalResolution, PixelFormats.Bgra32, null);

            bmp.Lock();
            try
            {

                using Image<Rgba32> _image = _imgState;
                _image.ProcessPixelRows(accessor =>
                {
                    var backBuffer = bmp.BackBuffer;

                    for (var y = 0; y < _imgState.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                        for (var x = 0; x < _imgState.Width; x++)
                        {
                            var backBufferPos = backBuffer + (y * _imgState.Width + x) * 4;
                            var rgba = pixelRow[x];
                            var color = rgba.A << 24 | rgba.R << 16 | rgba.G << 8 | rgba.B;

                            System.Runtime.InteropServices.Marshal.WriteInt32(backBufferPos, color);
                        }
                    }
                });

                bmp.AddDirtyRect(new Int32Rect(0, 0, _imgState.Width, _imgState.Height));
            }
            finally
            {
                bmp.Unlock();
            }
            return bmp;
        }

    }
}
