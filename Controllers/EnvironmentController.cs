using AdaptiveSpritesDMItool.Models;
using DMISharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class EnvironmentController
    {
        public static DataImageState dataImageState;
        public static DataImageState dataImageStateOverlay;

        public static WriteableBitmap gridCell;
        public static WriteableBitmap gridCellSelect;

        public static int widthStateImage;
        public static int heightStateImage;

        //The dimensions of the bitmap on which the interface elements are drawn.
        public static int widthBitmapUI = 257;
        public static int heightBitmapUI = 257;
        public static int pixelSize = 8;

        #region Loaders

        /// <summary>
        /// Load files and initialize Environment
        /// </summary>
        public static void InitializeEnvironment()
        {
            LoadDataImageFiles();
            LoadGridCell();

        }

        private static void SetData()
        {
            pixelSize = GetPixelSizeFromResolution(heightStateImage);
        }

        private static void LoadDataImageFiles()
        {
            string path = "TestImages";

            string fullpath = $"{path}/testBodyHuman.dmi";
            using DMIFile file = new DMIFile(fullpath);

            DMIState currentState = file.States.First();
            dataImageState = new DataImageState(currentState);

            Debug.WriteLine($"Loaded {file}({file.States.Count}).");

            widthStateImage = currentState.Width;
            heightStateImage = currentState.Height;

            Debug.WriteLine($"Image - Width: {widthStateImage}; Height: {heightStateImage}");


            // Overlay Preview File
            string fullpathOverlay = $"{path}/testClothingOveralls.dmi";
            using DMIFile fileOverlay = new DMIFile(fullpathOverlay);

            DMIState currentStateOverlay = fileOverlay.States.First();
            dataImageStateOverlay = new DataImageState(currentStateOverlay);

            Debug.WriteLine($"Loaded {fileOverlay}({fileOverlay.States.Count}).");
        }

        private static void LoadGridCell()
        {
            string path = "Resources";
            string fullPath = $"{path}/grid1.png";
            string fullPathSelect = $"{path}/grid1_select.png";

            gridCell = new WriteableBitmap(new BitmapImage(new Uri(fullPath, UriKind.Relative)));
            gridCellSelect = new WriteableBitmap(new BitmapImage(new Uri(fullPathSelect, UriKind.Relative)));

            //Debug.WriteLine($"Loaded {fullPath} - gridCell {gridCell}, {gridCell.Width}, {gridCell.PixelWidth}");
        }

        #endregion Loaders

        #region Saves

        public static void SaveBitmapIntoFile(string path, WriteableBitmap _gridBitmap)
        {
            using (FileStream stream =
                new FileStream(path, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_gridBitmap));
                encoder.Save(stream);
            }
        }

        #endregion Saves

        #region Paths

        public static string GetGridPath() => System.IO.Path.Combine(Environment.CurrentDirectory, "Resources", $"grid{EnvironmentController.heightStateImage}.png");

        #endregion Paths


        #region Getters

        public static WriteableBitmap GetEnvironmentImage(StateDirection _stateDirection, bool _isEdited = false)
        {
            WriteableBitmap bitmap = dataImageState.GetBMPstate(_stateDirection, _isEdited);
            return bitmap;
        }

        public static WriteableBitmap GetEnvironmentImageOverlay(StateDirection _stateDirection, bool _isEdited = false)
        {
            WriteableBitmap bitmapOverlay = dataImageStateOverlay.GetBMPstate(_stateDirection, _isEdited);
            return bitmapOverlay;
        }

        private static int GetPixelSizeFromResolution(int _pixelResolution)
        {
            int pixelSize = 8;
            if (_pixelResolution == 64)
            {
                pixelSize = 4;
            }
            else if (_pixelResolution == 16)
            {
                pixelSize = 16;
            }
            return pixelSize;
        }


        #region Get Colors

        public static System.Windows.Media.Color GetSelectorColor() => Colors.Black;

        public static System.Windows.Media.Color GetGridColor()
        {
            Color colorTemp = Colors.Black;
            byte alpha = 100;
            Color color = System.Windows.Media.Color.FromArgb(alpha, colorTemp.R, colorTemp.G, colorTemp.B);
            return color;
        }

        public static System.Windows.Media.Color GetGridBorderColor()
        {
            return Colors.Black;
        }

        #endregion Get Colors

        #endregion Getters
    }
}
