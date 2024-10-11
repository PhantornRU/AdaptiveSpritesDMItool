using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Resources;
using DMISharp;
using Newtonsoft.Json;
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
        public static string lastImportPath = defaultImportPath;
        public static string lastExportPath = defaultExportPath;

        public static string defaultResourcesPath = "Assets\\Resources";
        public static string defaultSavesPath = "Assets\\Saves";
        public static string defaultStoragePath = "Assets\\Storage";
        public static string ChoosenStorageName = "Default";

        public static string fileImageFormat = ".dmi";
        public static string configFormat = ".csv";
        public static string savesFormat = ".json";

        public static string currentConfigFullPath = string.Empty;

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


        #region Loadders






        // Сделай сериализацию переменных в виде файла-сохранения в папку EnvironmentController.DefaultPath


        public static void LoadSettings(EnvironmentItem item)
        {
            string path = item.FilePath;
            //string path = Path.Combine(defaultSavesPath, "settings.json");
            if (!File.Exists(path)) return;

            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));

            StatesController.currentStateEditMode = settings.CurrentStateEditMode;
            StatesController.currentStateQuantityMode = settings.CurrentStateQuantityMode;
            StatesController.currentStateDirection = settings.CurrentStateDirection;
            StatesController.selectedStateDirection = settings.SelectedStateDirection;
            StatesController.currentStatePreviewMode = settings.CurrentStatePreviewMode;
            StatesController.isLandmarkEditable = settings.IsLandmarkEditable;
            StatesController.isCentralizedState = settings.IsCentralizedState;
            StatesController.isMirroredState = settings.IsMirroredState;
            StatesController.isShowGrid = settings.IsShowGrid;
            StatesController.isShowAboveGrid = settings.IsShowAboveGrid;
            StatesController.isShowOverlay = settings.IsShowOverlay;
            StatesController.isShowTextGrid = settings.IsShowTextGrid;
            StatesController.isOverrideToggle = settings.IsOverrideToggle;


        }


        public static void SaveSettings()
        {
            string path = Path.Combine(defaultSavesPath, "save.json");
            if (!Directory.Exists(defaultSavesPath)) 
                Directory.CreateDirectory(defaultSavesPath);

            var settings = new Settings
            {
                CurrentStateEditMode = StatesController.currentStateEditMode,
                CurrentStateQuantityMode = StatesController.currentStateQuantityMode,
                CurrentStateDirection = StatesController.currentStateDirection,
                SelectedStateDirection = StatesController.selectedStateDirection,
                CurrentStatePreviewMode = StatesController.currentStatePreviewMode,
                IsLandmarkEditable = StatesController.isLandmarkEditable,
                IsCentralizedState = StatesController.isCentralizedState,
                IsMirroredState = StatesController.isMirroredState,
                IsShowGrid = StatesController.isShowGrid,
                IsShowAboveGrid = StatesController.isShowAboveGrid,
                IsShowOverlay = StatesController.isShowOverlay,
                IsShowTextGrid = StatesController.isShowTextGrid,
                IsOverrideToggle = StatesController.isOverrideToggle,
            };

            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented));
        }

        [Serializable]
        private class Settings
        {
            public StateEditType CurrentStateEditMode { get; set; }
            public StateQuantityType CurrentStateQuantityMode { get; set; }
            public StateDirection CurrentStateDirection { get; set; }
            public StateDirection SelectedStateDirection { get; set; }
            public StatePreviewType CurrentStatePreviewMode { get; set; }
            public bool IsLandmarkEditable { get; set; }
            public bool IsCentralizedState { get; set; }
            public bool IsMirroredState { get; set; }
            public bool IsShowGrid { get; set; }
            public bool IsShowAboveGrid { get; set; }
            public bool IsShowOverlay { get; set; }
            public bool IsShowTextGrid { get; set; }
            public bool IsOverrideToggle { get; set; }
        }







        /*
         нужно сохранять и загружать файлы-сохранения, хранящие и перезаписывающие переменные в static StatesController:         public static StateEditType currentStateEditMode = StateEditType.Single;
                public static StateQuantityType currentStateQuantityMode = StateQuantityType.Single;
                public static StateDirection currentStateDirection = StateDirection.South;
                public static StateDirection selectedStateDirection = StateDirection.South;
                public static StatePreviewType currentStatePreviewMode = StatePreviewType.Overlay;

                /// <summary> Will the Landmark also be edited along with the overlay? </summary>
                public static bool isLandmarkEditable = false;

                /// <summary>
                /// StatesEditorPage - Determines whether the state is centralized - setting the pixel in the middle of the pixel
                /// </summary>
                public static bool isCentralizedState = true;
                public static bool isMirroredState = true;
                public static bool isShowGrid = true;
                public static bool isShowAboveGrid = true;
                public static bool isShowOverlay = true;
                public static bool isShowTextGrid = false;

                /// <summary> DataPage - Whether to overwrite existing processed files in folders during processing. </summary>
                public static bool isOverrideToggle = false;
        */



















        #endregion Loadders


        #region Paths


        public static string GetGridPath() => System.IO.Path.Combine(Environment.CurrentDirectory, defaultResourcesPath, $"grid{dataImageState.imageCellsSize.Height}.png");

        public static string GetPixelStoragePath() => System.IO.Path.Combine(Environment.CurrentDirectory, defaultStoragePath, $"{ChoosenStorageName}");

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
