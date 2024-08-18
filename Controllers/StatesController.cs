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
        public static StateDirection currentStateDirection;

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

        // Initialize in StatesEditorPage.xaml.cs InitializeDictionaries()
        /// <summary>
        /// Stores all references to images for their quick calls depending on the state.
        /// </summary>
        public static Dictionary<StateDirection, Dictionary<StateImageType, System.Windows.Controls.Image>> stateSourceDictionary;

        #region Helpers

        public static bool isStateOpposite(StateDirection _stateDirection) => (int)_stateDirection % 2 == 0;

        #endregion Helpers


        #region Updaters

        public static void UpdateCurrentStateDirection(StateDirection _stateDirection) => currentStateDirection = _stateDirection;

        #endregion Updaters


        #region Getters

        #region States

        public static IEnumerable<StateDirection> GetStateDirections()
        {
            switch (currentStateQuantityMode)
            {
                case StateQuantityType.Single:
                    return new[] { currentStateDirection };

                case StateQuantityType.Parallel:
                    return GetParallelStates(currentStateDirection);

                case StateQuantityType.All:
                    int parallValue = (int)currentStateDirection / 2 == 1 ? -2 : 2;
                    return GetParallelStates(currentStateDirection).Union(GetParallelStates((StateDirection)((int)currentStateDirection + parallValue)));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static StateDirection[] GetParallelStates(StateDirection _stateDirection)
        {
            int parallValue = isStateOpposite(_stateDirection) ? 1 : -1;
            StateDirection parallelState = _stateDirection + parallValue;
            return new[] { _stateDirection, parallelState };
        }

        #endregion States


        #region Images

        public static System.Windows.Controls.Image GetRightImage(StateDirection _stateDirection) => stateSourceDictionary[_stateDirection][StateImageType.Right];

        #endregion Images


        #region Set Modes

        public static void SetCurrentStateQuantityMode(StateQuantityType _currentStateQuantityMode) => currentStateQuantityMode = _currentStateQuantityMode;

        public static void SetCurrentStateEditMode(StateEditType _currentStateEditMode) => currentStateEditMode = _currentStateEditMode;

        #endregion Set Modes


        #region Toggles

        public static void ToggleCentralizedState() => isCentralizedState = !isCentralizedState;

        public static void ToggleMirroredState() => isMirroredState = !isMirroredState;

        public static void ToggleShowGrid() => isShowGrid = !isShowGrid;

        public static void ToggleShowAboveGrid() => isShowAboveGrid = !isShowAboveGrid;

        public static void ToggleShowOverlay() => isShowOverlay = !isShowOverlay;

        #endregion Toggles


        #region Enabled Buttons

        public static bool GetEnableStateMirrorButton() => currentStateQuantityMode != StateQuantityType.Single;

        public static bool GetEnableStateCentralizeButton() => isMirroredState && (currentStateQuantityMode != StateQuantityType.Single);

        public static bool GetEnableStateGridZIndexButton() => isShowGrid;

        #endregion Enabled Buttons


        #region Z Indexes

        public static int GetBackgroundZIndex() => isShowAboveGrid ? backgroundZIndexAbove : backgroundZIndexUnder;

        #endregion Z Indexes


        #region Visibility

        public static Visibility GetVisibilityOverlay() => GetVisibility(isShowOverlay);

        public static Visibility GetVisibilityGrid() => GetVisibility(isShowGrid);

        private static Visibility GetVisibility(bool isValue) => isValue ? Visibility.Visible : Visibility.Collapsed;

        #endregion Visibility


        #region ControlAppearance

        public static ControlAppearance GetControlAppearanceOverlay() => GetControlAppearance(isShowOverlay);

        public static ControlAppearance GetControlAppearanceGrid() => GetControlAppearance(isShowGrid);

        public static ControlAppearance GetControlAppearanceGridZIndex() => GetControlAppearance(isShowAboveGrid);

        public static ControlAppearance GetControlAppearanceCentralize() => GetControlAppearance(isCentralizedState);

        public static ControlAppearance GetControlAppearanceMirror() => GetControlAppearance(isMirroredState);

        private static ControlAppearance GetControlAppearance(bool isValue) => isValue ? GetPressedButtonAppearance() : GetUnPressedButtonAppearance();

        public static ControlAppearance GetPressedButtonAppearance() => ControlAppearance.Primary;

        public static ControlAppearance GetUnPressedButtonAppearance() => ControlAppearance.Secondary;

        #endregion ControlAppearance

        #endregion Getters
    }
}
