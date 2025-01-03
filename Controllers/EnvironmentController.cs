﻿using AdaptiveSpritesDMItool.Helpers;
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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Color = System.Windows.Media.Color;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class EnvironmentController
    {
        public static DataImageState dataImageState;

        public static DataPixelStorage dataPixelStorage;

        public static string defaultImportPath = "Assets\\Import";
        public static string defaultExportPath = "Assets\\Export";
        public static string defaultFileName = "default";
        public static string defaultSaveName = "defaultSave";
        public static string lastImportPath = defaultImportPath;
        public static string lastExportPath = defaultExportPath;

        public static string defaultResourcesPath = "Assets\\Resources";
        public static string defaultSavesPath = "Assets\\Saves";
        public static string defaultStoragePath = "Assets\\Storage";
        public static string choosenStorageName = "Default";

        public static string fileImageFormat = ".dmi";
        public static string configFormat = ".csv";
        public static string savesFormat = ".json";

        public static string currentConfigFullPath = string.Empty;
        public static System.Drawing.Size currentResolutionSize = new System.Drawing.Size(32, 32);

        public static EnvironmentItem? choosenSaveFile;


        #region Loaders

        /// <summary>
        /// Load files and initialize Environment
        /// </summary>
        public static void InitializeEnvironment()
        {
            InitializeFolders();
            InitializeData();
            InitializeCellsData();
        }

        private static void InitializeFolders()
        {
            if (!Directory.Exists(defaultImportPath))
                Directory.CreateDirectory(defaultImportPath);
            if (!Directory.Exists(defaultExportPath))
                Directory.CreateDirectory(defaultExportPath);
            if (!Directory.Exists(defaultStoragePath))
                Directory.CreateDirectory(defaultStoragePath);
            if (!Directory.Exists(defaultSavesPath))
                Directory.CreateDirectory(defaultSavesPath);
        }

        private static void InitializeData()
        {
            LoadDataImageFiles();
        }

        private static void LoadDataImageFiles()
        {
            DMIState currentState = LoadDMIState(defaultImportPath, defaultFileName);
            DMIState landmarkState = LoadDMIState(defaultImportPath, "testBodyMonkey");
            DMIState overlayState = LoadDMIState(defaultImportPath, "testClothingDefaultCoat");
            dataImageState = new DataImageState(currentState, landmarkState, overlayState);
        }

        private static void InitializeCellsData()
        {
            int width = dataImageState.imageCellsSize.Width;
            int height = dataImageState.imageCellsSize.Height;

            dataPixelStorage = new DataPixelStorage(GetPixelStoragePath(), width, height, true);
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

        public static bool LoadSettings(EnvironmentItem item)
        {
            return SettingsManager.LoadSettings(item);
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

        public static void SaveSettings(string fullpath)
        {
            SettingsManager.SaveSettings(fullpath);
        }

        #endregion Saves


        #region Paths


        public static string GetGridPath() => System.IO.Path.Combine(Environment.CurrentDirectory, defaultResourcesPath, $"grid{dataImageState.imageCellsSize.Height}.png");

        public static string GetPixelStoragePath() => System.IO.Path.Combine(Environment.CurrentDirectory, defaultStoragePath, $"{choosenStorageName}");

        #endregion Paths

        #region Setters

        public static void SetSaveFile(EnvironmentItem? saveFile) => choosenSaveFile = saveFile;

        public static void ResetSaveFile() => choosenSaveFile = null;

        public static void SetResolutionX(int _resolutionX) => currentResolutionSize.Width = _resolutionX;

        public static void SetResolutionY(int _resolutionY) => currentResolutionSize.Height = _resolutionY;

        public static void SetCurrentResolution() => dataImageState.SetImageCellsSize(currentResolutionSize);

        

        #endregion Setters


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
