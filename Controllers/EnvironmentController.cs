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

        public static WriteableBitmap gridCell;
        public static WriteableBitmap gridCellSelect;

        #region Loaders

        /// <summary>
        /// Load files and initialize Environment
        /// </summary>
        public static void InitializeEnvironment()
        {
            InitializeData();
            LoadDataImageFiles();
        }

        private static void InitializeData()
        {
        }

        private static void LoadDataImageFiles()
        {
            string path = "TestImages";

            // Main Preview File
            string fullpath = $"{path}/testBodyHuman.dmi";
            using DMIFile file = new DMIFile(fullpath);
            DMIState currentState = file.States.First();

            // Overlay Preview File
            string fullpathOverlay = $"{path}/testClothingOveralls.dmi";
            using DMIFile fileOverlay = new DMIFile(fullpathOverlay);

            if(fileOverlay != null)
            {
                DMIState currentStateOverlay = fileOverlay.States.First();
                dataImageState = new DataImageState(currentState, currentStateOverlay);
            }
            else
            {
                dataImageState = new DataImageState(currentState);
            }

            Debug.WriteLine($"Loaded {file}({file.States.Count}).");
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

        public static string GetGridPath() => System.IO.Path.Combine(Environment.CurrentDirectory, "Resources", $"grid{dataImageState.imageCellsSize.Height}.png");

        #endregion Paths


        #region Getters

        public static WriteableBitmap GetPreviewBMP(StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            WriteableBitmap bitmap = dataImageState.stateBMPdict[_stateDirection][StateImageType.Preview][_stateImageSideType];
            return bitmap;
        }

        public static WriteableBitmap GetOverlayBMP(StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            WriteableBitmap bitmap = dataImageState.stateBMPdict[_stateDirection][StateImageType.Overlay][_stateImageSideType];
            return bitmap;
        }

        public static WriteableBitmap GetSelectorBMP(StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            WriteableBitmap bitmap = dataImageState.stateBMPdict[_stateDirection][StateImageType.Selection][_stateImageSideType];
            return bitmap;
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
