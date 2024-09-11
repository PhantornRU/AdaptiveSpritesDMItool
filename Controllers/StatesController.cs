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
        public static StateDirection currentStateDirection = StateDirection.South;
        public static StateDirection selectedStateDirection = StateDirection.South;
        public static StatePreviewType currentStatePreviewMode = StatePreviewType.Left;

        public static StateImageType[] editableImages = new StateImageType[] { StateImageType.Overlay /*, StateImageType.Preview*/ };

        /// <summary>
        /// Determines whether the state is centralized - setting the pixel in the middle of the pixel
        /// </summary>
        public static bool isCentralizedState = true;
        public static bool isMirroredState = true;
        public static bool isShowGrid = true;
        public static bool isShowAboveGrid = true;
        public static bool isShowOverlay = true;
        public static bool isShowTextGrid = false;


        // Display Z indices background grid on top of overlays.
        public static int backgroundZIndexUnder = 0;
        public static int backgroundZIndexAbove = 2;

        // Initialize in StatesEditorPage.xaml.cs InitializeDictionaries()
        /// <summary>
        /// Stores all references to images for their quick calls depending on the state.
        /// </summary>
        public static Dictionary<StateDirection, Dictionary<StateImageType, Dictionary<StateImageSideType, System.Windows.Controls.Image>>> stateSourceDictionary{ get; set; } = new();

        /// <summary>
        /// Links to status bar text blocks for editing and displaying them.
        /// </summary>
        public static Dictionary<StatusBarType, System.Windows.Controls.TextBlock> stateStatusBarDictionary { get; set; } = new();

        #region Helpers

        public static bool isStateOpposite(StateDirection _stateDirection) => ((int)_stateDirection % 2 != 0) && !(_stateDirection == currentStateDirection);

        public static bool isStateVerticalOpposite(StateDirection _stateDirection) => ((int)_stateDirection % 2 == 0) && !(_stateDirection == currentStateDirection);

        #endregion Helpers


        #region Updaters

        public static void UpdateCurrentStateDirection(StateDirection _stateDirection) => currentStateDirection = _stateDirection;

        public static void UpdateSelectedStateDirection(StateDirection _stateDirection) => selectedStateDirection = _stateDirection;

        #endregion Updaters


        #region Getters

        #region States

        public static IEnumerable<StateDirection> GetStateDirections() => GetStateDirections(currentStateQuantityMode, currentStateDirection);
        public static IEnumerable<StateDirection> GetStateDirections(StateQuantityType _stateQuantityMode, StateDirection _stateDirection)
        {
            switch (_stateQuantityMode)
            {
                case StateQuantityType.Single:
                    return new[] { _stateDirection };

                case StateQuantityType.Parallel:
                    return GetParallelStates(_stateDirection);

                case StateQuantityType.All:

                    StateDirection verticalOppositeState = GetVerticalOppositeState(_stateDirection);

                    var parallelStates = GetParallelStates(_stateDirection);
                    var parallelStatesOpposite = GetParallelStates(verticalOppositeState);

                    return parallelStates.Concat(parallelStatesOpposite);

                    // int parallValue = (int)_stateDirection / 2 == 1 ? -2 : 2;
                    // StateDirection verticalOppositeState = GetVerticalOppositeState(_stateDirection);
                    // return GetParallelStates(_stateDirection).Union(GetParallelStates(verticalOppositeState));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static StateDirection[] GetParallelStates() => GetParallelStates(currentStateDirection);
        public static StateDirection[] GetParallelStates(StateDirection _stateDirection)
        {
            //int parallValue = isStateOpposite(_stateDirection) ? -1 : 1;
            //StateDirection parallelState = _stateDirection + parallValue;
            StateDirection parallelState = GetHorizontalOppositeState(_stateDirection);
            return new[] { _stateDirection, parallelState };
        }


        public static StateDirection GetVerticalOppositeState() => GetVerticalOppositeState(currentStateDirection);
        public static StateDirection GetVerticalOppositeState(StateDirection _stateDirection)
        {
            switch (_stateDirection)
            {
                case StateDirection.South:
                    return StateDirection.East;
                case StateDirection.North:
                    return StateDirection.West;
                case StateDirection.East:
                    return StateDirection.South;
                case StateDirection.West:
                    return StateDirection.North;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static StateDirection GetHorizontalOppositeState() => GetHorizontalOppositeState(currentStateDirection);
        public static StateDirection GetHorizontalOppositeState(StateDirection _stateDirection)
        {
            switch (_stateDirection)
            {
                case StateDirection.South:
                    return StateDirection.North;
                case StateDirection.North:
                    return StateDirection.South;
                case StateDirection.East:
                    return StateDirection.West;
                case StateDirection.West:
                    return StateDirection.East;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// The main states directions we work with
        /// </summary>
        /// <returns></returns>
        public static StateDirection[] GetAllStateDirections(DirectionDepth directionDepth = DirectionDepth.Four)
        {
            switch (directionDepth)
            {
                case DirectionDepth.Four:
                    return new StateDirection[] { StateDirection.South, StateDirection.North, StateDirection.East, StateDirection.West };
                case DirectionDepth.Eight:
                    return new StateDirection[] { StateDirection.South, StateDirection.North, StateDirection.East, StateDirection.West, 
                                StateDirection.SouthEast, StateDirection.SouthWest, StateDirection.NorthEast, StateDirection.NorthWest };
            }
            return new StateDirection[] { StateDirection.South };
        }

        #endregion States


        #region Images

        public static System.Windows.Controls.Image GetImage(StateDirection _stateDirection, StateImageSideType _stateImageSideType) => stateSourceDictionary[_stateDirection][StateImageType.Preview][_stateImageSideType];

        #endregion Images


        #region Set Modes

        public static void SetCurrentStateQuantityMode(StateQuantityType _currentStateQuantityMode) => currentStateQuantityMode = _currentStateQuantityMode;

        public static void SetCurrentStateEditMode(StateEditType _currentStateEditMode) => currentStateEditMode = _currentStateEditMode;

        public static void SetCurrentStatePreviewMode(StatePreviewType _currentStatePreviewMode) => currentStatePreviewMode = _currentStatePreviewMode;

        #endregion Set Modes


        #region Toggles

        public static void ToggleCentralizedState() => isCentralizedState = !isCentralizedState;

        public static void ToggleMirroredState() => isMirroredState = !isMirroredState;

        public static void ToggleShowGrid() => isShowGrid = !isShowGrid;

        public static void ToggleShowAboveGrid() => isShowAboveGrid = !isShowAboveGrid;

        public static void ToggleShowOverlay() => isShowOverlay = !isShowOverlay;

        public static void ToggleShowTextGrid() => isShowTextGrid = !isShowTextGrid;

        #endregion Toggles


        #region Enabled Buttons

        public static bool GetEnableStateMirrorButton() => currentStateQuantityMode != StateQuantityType.Single;

        public static bool GetEnableStateCentralizeButton() => isMirroredState && (currentStateQuantityMode != StateQuantityType.Single);

        public static bool GetEnableStateGridZIndexButton() => isShowGrid;

        public static bool GetEnableStateTextGridButton() => isShowGrid;

        #endregion Enabled Buttons


        #region Z Indexes

        public static int GetBackgroundZIndex() => isShowAboveGrid ? backgroundZIndexAbove : backgroundZIndexUnder;

        #endregion Z Indexes


        #region Visibility

        public static Visibility GetVisibilityOverlay() => GetVisibility(isShowOverlay);


        public static Visibility GetVisibilityGrid() => GetVisibility(isShowGrid);

        public static Visibility GetVisibilityTextGrid() => GetVisibility(isShowTextGrid && isShowGrid);

        private static Visibility GetVisibility(bool isValue) => isValue ? Visibility.Visible : Visibility.Collapsed;

        #endregion Visibility


        #region ControlAppearance

        public static ControlAppearance GetControlAppearanceOverlay() => GetControlAppearance(isShowOverlay);

        public static ControlAppearance GetControlAppearanceGrid() => GetControlAppearance(isShowGrid);

        public static ControlAppearance GetControlAppearanceGridZIndex() => GetControlAppearance(isShowAboveGrid);

        public static ControlAppearance GetControlAppearanceCentralize() => GetControlAppearance(isCentralizedState);

        public static ControlAppearance GetControlAppearanceMirror() => GetControlAppearance(isMirroredState);

        public static ControlAppearance GetControlAppearanceTextGrid() => GetControlAppearance(isShowTextGrid);

        private static ControlAppearance GetControlAppearance(bool isValue) => isValue ? GetPressedButtonAppearance() : GetUnPressedButtonAppearance();

        public static ControlAppearance GetPressedButtonAppearance() => ControlAppearance.Primary;

        public static ControlAppearance GetUnPressedButtonAppearance() => ControlAppearance.Secondary;

        #endregion ControlAppearance

        #endregion Getters

    }
}
