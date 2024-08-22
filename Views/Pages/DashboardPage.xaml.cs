using AdaptiveSpritesDMItool.ViewModels.Pages;
using DMISharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Bmp;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using SixLabors.ImageSharp.Formats;
using static System.Formats.Asn1.AsnWriter;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Drawing;
using Point = System.Drawing.Point;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
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

            DMIState currentState = file.States.First();

            //stateCanvasLeftPreview.Background = Brushes.Black;

            //ImageBrush brush = new ImageBrush();
            //brush.ImageSource = new BitmapImage(new Uri(fullpath, UriKind.Relative));
            //stateCanvasLeftPreview.Background = brush;

            using Image<Rgba32>? imgState = currentState.GetFrame(StateDirection.South, 0);
            if (imgState is Image<Rgba32> valueOfImage)
                Console.WriteLine($"image is {valueOfImage}");
            else
                Console.WriteLine("image does not have a value");

            //imgState.SaveAsPng($"{path}/export/test1.png");

            //var bytes = imgState.Configuration.ImageFormatsManager.GetEncoder(BmpFormat.Instance);
            //stateCanvasLeftPreview.DraBackground = bytes;
            //testImg.Source = imgState;


            //byte[] bitmap = new byte[imgState.Width * imgState.Height * Unsafe.SizeOf<Rgba32>()];
            //imgState.CopyPixelDataTo(bitmap);

            int width = imgState.Width;
            int height = imgState.Height;

            byte[] pixelBytes = new byte[width * height * Unsafe.SizeOf<Rgba32>()];
            imgState.CopyPixelDataTo(pixelBytes);

            Rgba32[] pixelArray = new Rgba32[imgState.Width * imgState.Height];
            imgState.CopyPixelDataTo(pixelArray);



            // получаем
            WriteableBitmap testBMPOriginal = GetBMP(imgState);
            WriteableBitmap testBMP = testBMPOriginal.Clone();

            //устанавливаем
            imgTest1.Source = testBMPOriginal;
            imgTest2.Source = testBMP;

            // стереть/нарисовать 1 пиксель на 16:16
            testBMP.SetPixel(16, 16, Colors.White);

            // копируем и вставляем
            testBMP.SetPixel(25, 25, testBMPOriginal.GetPixel(13, 13)); 

            // Квадрат
            testBMP.DrawRectangle(2, 4, 8, 10, Colors.Red);


            //imgTest.Source = ToImage(pixelBytes, width);


            //BitmapImage bm = GetConvertedImage(pixelBytes);


            //BitmapSource bitmapSource = BitmapSource.Create(1, 1, 32, 32, PixelFormats.Indexed8 BitmapPalettes.Gray256, pixelBytes, 2);

            //imgTest.Source = bitmapSource;


            // !!! ТЕСТОВЫЙ ВЫВОД
            //using (var image = SixLabors.ImageSharp.Image.LoadPixelData(pixelBytes, width, height))
            //{
            //    // Work with the image
            //}




            //imgTest.Source = GetConvertedImage(pixelBytes);
            //stateCanvasLeftPreview.Background = GetConvertedImage(pixelBytes);

            //File.WriteAllBytes("test.png", bitmap);



            //using SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(bitmap);
            //image.Save("output.jpg");
        }

        private void imgTest2_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }

        private void imgTest2_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //Debug.WriteLine(e.GetPosition(imgTest2));

            Debug.WriteLine(GetMouseCoordinates(e, imgTest2));
        }

        private Point GetMouseCoordinates(System.Windows.Input.MouseButtonEventArgs _e, System.Windows.Controls.Image _img)
        {
            System.Windows.Point pos = _e.GetPosition(_img);
            int x = (int)Math.Floor(pos.X * _img.Source.Width / _img.ActualWidth);
            int y = (int)Math.Floor(pos.Y * _img.Source.Height / _img.ActualHeight);
            return new Point(x, y);
            
        }

        private WriteableBitmap GetBMP(Image<Rgba32> _imgState)
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

        public BitmapImage ToImage(byte[] bytes, int width)
        {
            MemoryStream stream = new MemoryStream(bytes);
            stream.Position = 0;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.DecodePixelWidth = width; // Width of the image
            bi.StreamSource = stream;
            bi.EndInit();
            return bi;
        }

        private static BitmapImage GetConvertedImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
    }
}
