using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Resources;
using DMISharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class EnvironmentController
    {
        public static DataImageState dataImageState;

        public static DataPixelStorage dataPixelStorage;

        public static string defaultPath = "TestImages";
        public static string defaultFileName = "testBodies"; // "testBodyHuman";
        public static string lastPath = "TestImages";

        public static string defaultFileFormat = ".dmi";
        public static string configFormat = ".csv";

        public static string currentConfigFullPath = string.Empty;

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
            DMIState currentState = LoadDMIState(defaultPath, defaultFileName);
            dataImageState = new DataImageState(currentState);
        }

        private static void InitializeCellsData()
        {
            int width = dataImageState.imageCellsSize.Width;
            int height = dataImageState.imageCellsSize.Height;

            dataPixelStorage = new DataPixelStorage(GetPixelStoragePath(), width, height);
        }

        public static DMIState LoadDMIState(string path, string fileName, int? index = null)
        {
            if (!Directory.Exists(path))
            {
                throw new Exception("Cant find path to dmi files.");
                //Directory.CreateDirectory(path);
            }

            string fullpath = $"{path}/{fileName}.dmi";
            if (!File.Exists(fullpath))
            {
                throw new Exception("Cant find file: " + fullpath);
            }

            DMIFile fileDmi = new DMIFile(fullpath);
            Debug.WriteLine($"Loaded {fileName}({fileDmi.States.Count}).");

            if (index == null || fileDmi.States.Count <= 1)
                return fileDmi.States.First();

            DMIState state = fileDmi.States.ElementAt((int)index);
            return state;
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
