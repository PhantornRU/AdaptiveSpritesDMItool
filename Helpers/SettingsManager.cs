using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Models;
using Newtonsoft.Json;

namespace AdaptiveSpritesDMItool.Helpers
{
    static class SettingsManager
    {

        [Serializable]
        private class Settings
        {
            public StateEditType CurrentStateEditMode { get; set; }
            public StateQuantityType CurrentStateQuantityMode { get; set; }
            public DMISharp.StateDirection CurrentStateDirection { get; set; }
            public DMISharp.StateDirection SelectedStateDirection { get; set; }
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

        public static bool LoadSettings(EnvironmentItem item)
        {
            string path = item.FilePath;
            //string path = Path.Combine(defaultSavesPath, "settings.json");
            if (!File.Exists(path)) return false;

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

            Debug.WriteLine($"Settings loaded: {path}");
            return true;
        }

        public static void SaveSettings(string? path = null)
        {
            if(path == null || File.Exists(path))
            {
                var file = EnvironmentController.choosenSaveFile;
                if (file == null) return;
                path = file.FilePath;
                string? dir = Path.GetDirectoryName(path);
                if (dir == null) return;

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }

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
            Debug.WriteLine($"Settings saved: {path}");
        }
    }
}
