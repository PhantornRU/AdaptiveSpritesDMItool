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

        //public static Point[,] cellsData;// = new Point[dataImageState.imageCellsSize.Width, dataImageState.imageCellsSize.Height];

        public static DataPixelStorage dataPixelStorage;

        #region Loaders

        /// <summary>
        /// Load files and initialize Environment
        /// </summary>
        public static void InitializeEnvironment()
        {
            InitializeData();
            InitializeCellsData();
        }

        private static void InitializeData()
        {
            LoadDataImageFiles();
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

        private static void InitializeCellsData()
        {
            int width = dataImageState.imageCellsSize.Width;
            int height = dataImageState.imageCellsSize.Height;

            dataPixelStorage = new DataPixelStorage(GetPixelStoragePath(), width, height);

            //dataPixelStorage = new DataPixelStorage((width, height)[] points);

            //cellsData = new Point[width, height];
            //for (int i = 0; i < width; i++)
            //{
            //    for (int j = 0; j < height; j++)
            //    {
            //        cellsData[i, j] = new Point(i, j);
            //    }
            //}
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

        public static void SavePixelStorage()
        {
            dataPixelStorage.SavePixelStorage(GetPixelStoragePath());
        }

        #endregion Saves

        #region Paths

        public static string ChoosenStorageName = "Default";

        public static string GetGridPath() => System.IO.Path.Combine(Environment.CurrentDirectory, "Resources", $"grid{dataImageState.imageCellsSize.Height}.png");

        public static string GetPixelStoragePath() => System.IO.Path.Combine(Environment.CurrentDirectory, "Storage", $"{ChoosenStorageName}");

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
