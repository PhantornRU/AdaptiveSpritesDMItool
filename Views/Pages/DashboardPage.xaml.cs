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
using System.Globalization;
using System.Windows.Media.Effects;
using System.Windows.Interop;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();

            TestFunction();

            DataContext = this;
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



            //// Display the drawing using an image control.
            //System.Windows.Controls.Image theImage = new System.Windows.Controls.Image();
            //DrawingImage dImageSource = new DrawingImage(dGroup);
            //theImage.Source = dImageSource;

            ////this.Content = theImage;
            //imgTestText = theImage;

            InitializeTextGrid();
            InitializeListTemplate();
        }


        //private static Dictionary<ushort, double> _glyphWidths = new Dictionary<ushort, double>();
        //private static GlyphTypeface _glyphTypeface;
        //public static GlyphRun CreateGlyphRun(string text, double size, Point position)
        //{
        //    if (_glyphTypeface == null)
        //    {
        //        Typeface typeface = new Typeface("Arial");
        //        if (!typeface.TryGetGlyphTypeface(out _glyphTypeface))
        //            throw new InvalidOperationException("No glyphtypeface found");
        //    }

        //    ushort[] glyphIndexes = new ushort[text.Length];
        //    double[] advanceWidths = new double[text.Length];

        //    var totalWidth = 0d;
        //    double glyphWidth;

        //    for (int n = 0; n < text.Length; n++)
        //    {
        //        ushort glyphIndex = (ushort)(text[n] - 29);
        //        glyphIndexes[n] = glyphIndex;

        //        if (!_glyphWidths.TryGetValue(glyphIndex, out glyphWidth))
        //        {
        //            glyphWidth = _glyphTypeface.AdvanceWidths[glyphIndex] * size;
        //            _glyphWidths.Add(glyphIndex, glyphWidth);
        //        }
        //        advanceWidths[n] = glyphWidth;
        //        totalWidth += glyphWidth;
        //    }

        //    var offsetPosition = new System.Windows.Point(position.X - (totalWidth / 2), position.Y - 10 - size);

        //    GlyphRun glyphRun = new GlyphRun(_glyphTypeface, 0, false, size, glyphIndexes, offsetPosition, advanceWidths, null, null, null, null, null, null);

        //    return glyphRun;
        //}






        System.Windows.Point currentMousePosition = new System.Windows.Point(0, 0);

        private void imgTest2_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            currentMousePosition = e.GetPosition(imgTest2);
            RenderTextGrid();
        }



        DrawingGroup dGroup = new DrawingGroup();

        System.Windows.Size sizeCells;
        System.Windows.Size pointSize;
        SolidColorBrush brush;
        double fontSize;
        Typeface typeface;
        double pixelsPerDip;
        FlowDirection flowDirection;
        CultureInfo cultureInfo;

        private void InitializeTextGrid()
        {

            System.Windows.Media.Pen shapeOutlinePen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2);
            shapeOutlinePen.Freeze();

            DrawingImage dImageSource = new DrawingImage(dGroup);
            imgTestText.Source = dImageSource;

            sizeCells = new System.Windows.Size(32, 32);
            pointSize = new System.Windows.Size(
                127 / sizeCells.Width,
                127 / sizeCells.Height);
            pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            typeface = new Typeface("Arial Narrow");
            fontSize = 1.8;
            brush = System.Windows.Media.Brushes.Black;
            flowDirection = FlowDirection.LeftToRight;
            cultureInfo = CultureInfo.GetCultureInfo("en-us");


            // Obtain a DrawingContext from 
            // the DrawingGroup.
            using (DrawingContext dc = dGroup.Open())
            {
                //Debug.WriteLine($"pointSize: {pointSize}, sizeCells: {sizeCells}, pixelsPerDip: {pixelsPerDip}, currentMousePosition: {currentMousePosition}");

                for (int i = 0; i < sizeCells.Width; i++)
                {
                    for (int j = 0; j < sizeCells.Height; j++)
                    {
                        //Debug.WriteLine($"i: {i}, j: {j}");
                        System.Windows.Point point = new System.Windows.Point(i * pointSize.Width, j * pointSize.Height);
                        string text = $"{i}:{j}";

                        FormattedText formattedText = new FormattedText(
                            text,
                            cultureInfo,
                            flowDirection,
                            typeface,
                            fontSize,
                            brush,
                            pixelsPerDip);

                        dc.DrawText(formattedText, point);
                    }
                }

                //dc.PushOpacity(0.5);



                // Draw a rectangle at full opacity.
                //dc.DrawRectangle(System.Windows.Media.Brushes.Blue, shapeOutlinePen, new Rect(0, 0, 25, 25));

                //// Push an opacity change of 0.5. 
                //// The opacity of each subsequent drawing will
                //// will be multiplied by 0.5.
                //dc.PushOpacity(0.5);

                //// This rectangle is drawn at 50% opacity.
                //dc.DrawRectangle(System.Windows.Media.Brushes.Blue, shapeOutlinePen, new Rect(25, 25, 25, 25));

                //// Blurs subsquent drawings. 
                //dc.PushEffect(new BlurBitmapEffect(), null);

                //// This rectangle is blurred and drawn at 50% opacity (0.5 x 0.5). 
                //dc.DrawRectangle(System.Windows.Media.Brushes.Blue, shapeOutlinePen, new Rect(50, 50, 25, 25));

                //// This rectangle is also blurred and drawn at 50% opacity.
                //dc.DrawRectangle(System.Windows.Media.Brushes.Blue, shapeOutlinePen, new Rect(75, 75, 25, 25));

                // Stop applying the blur to subsquent drawings.
                //dc.Pop();




                //var rect = new Rect(0, 0, 127, 127);
                //var cb = new CombinedGeometry(GeometryCombineMode.Xor,
                //                              new RectangleGeometry(rect),
                //                             new EllipseGeometry(new System.Windows.Point(32, 16), 32, 16));

                //var mask = new DrawingBrush(new GeometryDrawing(System.Windows.Media.Brushes.Blue, null, cb));
                //dc.PushOpacityMask(mask);
                //dc.DrawImage(imgTestText.Source, rect);




                //// This rectangle is drawn at 50% opacity with no blur effect.
                //dc.DrawRectangle(System.Windows.Media.Brushes.Blue, shapeOutlinePen, new Rect(100, 100, 25, 25));



                //prepare the geometry, which can be considered as the puncher.
                //var rect = new Rect(0, 0, 127, 127);
                //var cb = new CombinedGeometry(GeometryCombineMode.Xor,
                //                              new RectangleGeometry(rect),
                //                             new EllipseGeometry(new System.Windows.Point(16, 2), 6, 8));
                //punch the DrawingVisual
                //yourDrawingVisual.Clip = cb;

            }



            //using (DrawingContext dc = dGroup.Open())
            //{
            //    currentMousePosition = new System.Windows.Point(8, 2);
            //    System.Windows.Point point = new System.Windows.Point((int)(currentMousePosition.X / pointSize.Width), (int)(currentMousePosition.Y / pointSize.Height));
            //    string text = $"{point.X}:{point.Y}";

            //    var rect = new Rect(0, 0, 127, 127);
            //    var cb = new CombinedGeometry(GeometryCombineMode.Xor,
            //                                  new RectangleGeometry(rect),
            //                                 new EllipseGeometry(point, pointSize.Width, pointSize.Height));

            //    var mask = new DrawingBrush(new GeometryDrawing(System.Windows.Media.Brushes.Blue, null, cb));
            //    dc.PushOpacityMask(mask);
            //    dc.DrawImage(imgTestText.Source, rect);

            //    FormattedText formattedText = new FormattedText(
            //        text,
            //        cultureInfo,
            //        flowDirection,
            //        typeface,
            //        fontSize,
            //        brush,
            //        pixelsPerDip);

            //    dc.DrawText(formattedText, point);
            //}
            //Debug.WriteLine($"Width: {dImageSource.Width}, Height: {dImageSource.Height}");




        }



        private void RenderTextGrid()
        {

            // Obtain a DrawingContext from 
            // the DrawingGroup.
            using (DrawingContext dc = dGroup.Open())
            {

                for (int i = 0; i < sizeCells.Width; i++)
                {
                    for (int j = 0; j < sizeCells.Height; j++)
                    {
                        //Debug.WriteLine($"i: {i}, j: {j}");
                        System.Windows.Point point = new System.Windows.Point(i * pointSize.Width, j * pointSize.Height);
                        string text = $"{i}:{j}";

                        System.Windows.Point pointCurrent = new System.Windows.Point((int)(currentMousePosition.X / pointSize.Width), (int)(currentMousePosition.Y / pointSize.Height));
                        //System.Windows.Point pointCurrent = new System.Windows.Point((int)(currentMousePosition.X), (int)(currentMousePosition.Y));
                        if(pointCurrent.X == i && pointCurrent.Y == j)
                        {
                            //text = $"{pointCurrent.X}:{pointCurrent.Y}";
                            text = $"???";

                        }

                        FormattedText formattedText = new FormattedText(
                            text,
                            cultureInfo,
                            flowDirection,
                            typeface,
                            fontSize,
                            brush,
                            pixelsPerDip);

                        dc.DrawText(formattedText, point);
                    }
                }
            }
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








        private List<Employee> employees;

        private void InitializeListTemplate()
        {

            employees = new List<Employee>();
            employees.Add(new Employee { Id = 1, Name = "Kapil Malhotra", Age = 30 });
            employees.Add(new Employee { Id = 2, Name = "Raj Kundra", Age = 34 });
            employees.Add(new Employee { Id = 3, Name = "Amitabh Bachan", Age = 80 });
            employees.Add(new Employee { Id = 4, Name = "Deepak Khanna", Age = 72 });

        }

        public List<Employee> Employees
        {
            get
            {
                return employees;
            }
        }

        public class Employee
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }






    }
}
