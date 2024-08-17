using AdaptiveSpritesDMItool.Models;
using DMISharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class StatesController
    {
        public static StateEditType currentStateEditMode = StateEditType.Single;
        public static StateQuantityType currentStateQuantityMode = StateQuantityType.Single;

        /// <summary>
        /// Determines whether the state is centralized - setting the pixel in the middle of the pixel
        /// </summary>
        public static bool isCentralizedState = true;
        public static bool isMirroredState = true;
        public static bool isShowGrid = true;
        public static bool isShowAboveGrid = true;
        public static bool isShowOverlay = true;


        // Display Z indices background grid on top of overlays.
        public static int backgroundZIndexUnder = 0;
        public static int backgroundZIndexAbove = 2;

        public static Dictionary<StateDirection, Dictionary<StateImageType, System.Windows.Controls.Image>> stateSourceDictionary;

        public static void InitializeSources()
        {
            foreach (var (stateDirection, images) in stateSourceDictionary)
            {
                images[StateImageType.Left].Source = EnvironmentController.GetEnvironmentImage(stateDirection, false);
                images[StateImageType.Right].Source = EnvironmentController.GetEnvironmentImage(stateDirection, true);
            }

            foreach (var (stateDirection, images) in stateSourceDictionary)
            {
                images[StateImageType.OverlayLeft].Source = EnvironmentController.GetEnvironmentImageOverlay(stateDirection, false);
                images[StateImageType.OverlayRight].Source = EnvironmentController.GetEnvironmentImageOverlay(stateDirection, true);
            }
        }

        public static void InitializeGrids()
        {
            WriteableBitmap gridBitmap = EditorController.GetGridBackground();

            foreach (var state in stateSourceDictionary.Values)
            {
                state[StateImageType.BackgroundLeft].Source = gridBitmap;
                state[StateImageType.BackgroundRight].Source = gridBitmap;
            }
        }

        // Set Modes

        public static void SetCurrentStateQuantityMode(StateQuantityType _currentStateQuantityMode) => currentStateQuantityMode = _currentStateQuantityMode;

        public static void SetCurrentStateEditMode(StateEditType _currentStateEditMode) => currentStateEditMode = _currentStateEditMode;


        // Toggles

        public static void ToggleCentralizedState() => isCentralizedState = !isCentralizedState;

        public static void ToggleMirroredState() => isMirroredState = !isMirroredState;

        public static void ToggleShowGrid() => isShowGrid = !isShowGrid;

        public static void ToggleShowAboveGrid() => isShowAboveGrid = !isShowAboveGrid;

        public static void ToggleShowOverlay() => isShowOverlay = !isShowOverlay;


        // Enabled Buttons

        public static bool GetEnableStateMirrorButton() => currentStateQuantityMode != StateQuantityType.Single;

        public static bool GetEnableStateCentralizeButton() => isMirroredState && (currentStateQuantityMode != StateQuantityType.Single);

        public static bool GetEnableStateGridZIndexButton() => isShowGrid;

        // Z Indexes

        public static int GetBackgroundZIndex() => isShowAboveGrid ? backgroundZIndexAbove : backgroundZIndexUnder;

        
        // Visibility

        public static Visibility GetVisibilityOverlay() => GetVisibility(isShowOverlay);

        public static Visibility GetVisibilityGrid() => GetVisibility(isShowGrid);

        private static Visibility GetVisibility(bool isValue) => isValue ? Visibility.Visible : Visibility.Collapsed;


        // ControlAppearance

        public static ControlAppearance GetControlAppearanceOverlay() => GetControlAppearance(isShowOverlay);

        public static ControlAppearance GetControlAppearanceGrid() => GetControlAppearance(isShowGrid);

        public static ControlAppearance GetControlAppearanceGridZIndex() => GetControlAppearance(isShowAboveGrid);

        public static ControlAppearance GetControlAppearanceCentralize() => GetControlAppearance(isCentralizedState);

        public static ControlAppearance GetControlAppearanceMirror() => GetControlAppearance(isMirroredState);

        private static ControlAppearance GetControlAppearance(bool isValue) => isValue ? GetPressedButtonAppearance() : GetUnPressedButtonAppearance();

        public static ControlAppearance GetPressedButtonAppearance() => ControlAppearance.Primary;

        public static ControlAppearance GetUnPressedButtonAppearance() => ControlAppearance.Secondary;

    }
}
