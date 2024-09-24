using AdaptiveSpritesDMItool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class ButtonsController
    {

        #region Initialized at StatesEditorPage.xaml.ca

        public static Wpf.Ui.Controls.Button? SingleButton;
        public static Wpf.Ui.Controls.Button? FillButton;
        public static Wpf.Ui.Controls.Button? SelectButton;
        public static Wpf.Ui.Controls.Button? MoveButton;
        public static Wpf.Ui.Controls.Button? DeleteButton;
        public static Wpf.Ui.Controls.Button? UndoButton;
        public static Wpf.Ui.Controls.Button? UndoAreaButton;
        public static Wpf.Ui.Controls.Button? UndoLastButton;

        public static Wpf.Ui.Controls.Button? ChooseSingleStateButton;
        public static Wpf.Ui.Controls.Button? ChooseParallelStatesButton;
        public static Wpf.Ui.Controls.Button? ChooseAllStatesButton;

        public static Wpf.Ui.Controls.Button? CentralizeStatesButton;
        public static Wpf.Ui.Controls.Button? MirrorStatesButton;

        public static Wpf.Ui.Controls.Button? GridEnvironmentButton;
        public static Wpf.Ui.Controls.Button? GridZIndexEnvironmentButton;
        public static Wpf.Ui.Controls.Button? TextGridEnvironmentButton;

        public static Wpf.Ui.Controls.Button? OverlayButton;

        #endregion Initialized at StatesEditorPage.xaml.ca

        #region Gestures

        public static KeyGesture SaveGesture = new KeyGesture(Key.S, ModifierKeys.Control);

        public static KeyGesture SingleGesture = new KeyGesture(Key.D1, ModifierKeys.Control);
        public static KeyGesture FillGesture = new KeyGesture(Key.D2, ModifierKeys.Control);

        public static KeyGesture MoveGesture = new KeyGesture(Key.D3, ModifierKeys.Control);
        public static KeyGesture SelectGesture = new KeyGesture(Key.D4, ModifierKeys.Control);

        public static KeyGesture DeleteGesture = new KeyGesture(Key.F1, ModifierKeys.Control);
        public static KeyGesture UndoGesture = new KeyGesture(Key.F2, ModifierKeys.Control);
        public static KeyGesture UndoAreaGesture = new KeyGesture(Key.F3, ModifierKeys.Control);
        public static KeyGesture UndoLastGesture = new KeyGesture(Key.Z, ModifierKeys.Control);
        //public static KeyGesture RedoGesture = new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift);

        public static KeyGesture ChooseSingleStateGesture = new KeyGesture(Key.D1, ModifierKeys.Alt);
        public static KeyGesture ChooseParallelStatesGesture = new KeyGesture(Key.D2, ModifierKeys.Alt);
        public static KeyGesture ChooseAllStatesGesture = new KeyGesture(Key.D3, ModifierKeys.Alt);

        public static KeyGesture MirrorStatesGesture = new KeyGesture(Key.D1, ModifierKeys.Windows);
        public static KeyGesture CentralizeStatesGesture = new KeyGesture(Key.D2, ModifierKeys.Windows);
        public static KeyGesture GridEnvironmentGesture = new KeyGesture(Key.D3, ModifierKeys.Windows);
        public static KeyGesture GridZIndexEnvironmentGesture = new KeyGesture(Key.D4, ModifierKeys.Windows);
        public static KeyGesture TextGridEnvironmentGesture = new KeyGesture(Key.D5, ModifierKeys.Windows);
        public static KeyGesture OverlayGesture = new KeyGesture(Key.D6, ModifierKeys.Windows);

        #endregion Gestures

        public static void InitializeButtons()
        {

        }

        #region Buttons Toolbar Controller

        public static void ControllButtonsAvailability()
        {
            if (MirrorStatesButton != null)
                MirrorStatesButton.IsEnabled = StatesController.GetEnableStateMirrorButton();
            if (CentralizeStatesButton != null)
                CentralizeStatesButton.IsEnabled = StatesController.GetEnableStateCentralizeButton();
            if (GridZIndexEnvironmentButton != null)
                GridZIndexEnvironmentButton.IsEnabled = StatesController.GetEnableStateGridZIndexButton();
            GridZIndexEnvironmentUpdate();
            GridEnvironmentUpdate();
        }

        #region Buttons Edit Controller

        public static void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SingleButton == null) return;
            Debug.WriteLine("Single Button Click");
            ResetEditButtons();
            SingleButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Single);
        }

        public static void FillButton_Click(object sender, RoutedEventArgs e)
        {
            if (FillButton == null) return;
            ResetEditButtons();
            FillButton.Appearance = StatesController.GetPressedButtonAppearance();

            StatesController.SetCurrentStateEditMode(StateEditType.Fill);
        }

        public static void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeleteButton == null) return;
            ResetEditButtons();
            DeleteButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Delete);
        }
        public static void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (UndoButton == null) return;
            ResetEditButtons();
            UndoButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Undo);
        }

        public static void UndoAreaButton_Click(object sender, RoutedEventArgs e)
        {
            if (UndoAreaButton == null) return;
            ResetEditButtons();
            UndoAreaButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.UndoArea);
        }

        public static void UndoLastButton_Click(object sender, RoutedEventArgs e)
        {
            if (UndoLastButton == null) return;
            EnvironmentController.dataPixelStorage.UndoLastChange();
            Debug.WriteLine("Save Undo last change");
        }

        #endregion Buttons Edit Controller


        #region Buttons Move Controller

        public static void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MoveButton == null) return;
            ResetEditButtons();
            MoveButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Move);
        }

        public static void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectButton == null) return;
            ResetEditButtons();
            SelectButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Select);
        }

        #endregion Buttons Move Controller


        #region Buttons States Controller

        public static void ChooseSingleStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChooseSingleStateButton == null) return;
            ResetStatesButtons();
            ChooseSingleStateButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.Single);
            ControllButtonsAvailability();
        }

        public static void ChooseParallelStatesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChooseParallelStatesButton == null) return;
            ResetStatesButtons();
            ChooseParallelStatesButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.Parallel);
            ControllButtonsAvailability();
        }

        public static void ChooseAllStatesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChooseAllStatesButton == null) return;
            ResetStatesButtons();
            ChooseAllStatesButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.All);
            ControllButtonsAvailability();
        }

        public static void CentralizeStatesButton_Click(object sender, RoutedEventArgs e)
        {
            if (CentralizeStatesButton == null) return;
            StatesController.ToggleCentralizedState();
            CentralizeStatesButton.Appearance = StatesController.GetControlAppearanceCentralize();
        }

        public static void MirrorStatesButton_Click(object sender, RoutedEventArgs e)
        {
            if (MirrorStatesButton == null) return;
            StatesController.ToggleMirroredState();
            MirrorStatesButton.Appearance = StatesController.GetControlAppearanceMirror();
            ControllButtonsAvailability();
        }

        #endregion Buttons States Controller


        #region Buttons Environment Controller

        public static void GridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (GridEnvironmentButton == null) return;
            StatesController.ToggleShowGrid();
            GridEnvironmentUpdate();
        }

        public static void GridEnvironmentUpdate()
        {
            if (GridEnvironmentButton != null)
                GridEnvironmentButton.Appearance = StatesController.GetControlAppearanceGrid();
            if (GridZIndexEnvironmentButton != null) 
                GridZIndexEnvironmentButton.IsEnabled = StatesController.GetEnableStateGridZIndexButton();
            if (TextGridEnvironmentButton != null) 
                TextGridEnvironmentButton.IsEnabled = StatesController.GetEnableStateTextGridButton();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                state[StateImageType.Background][StateImageSideType.Left].Visibility = StatesController.GetVisibilityGrid();
                state[StateImageType.Background][StateImageSideType.Right].Visibility = StatesController.GetVisibilityGrid();

                state[StateImageType.TextGrid][StateImageSideType.Left].Visibility = StatesController.GetVisibilityTextGrid();
                state[StateImageType.TextGrid][StateImageSideType.Right].Visibility = StatesController.GetVisibilityTextGrid();
            }
        }

        public static void GridZIndexEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleShowAboveGrid();
            GridZIndexEnvironmentUpdate();
        }

        public static void GridZIndexEnvironmentUpdate()
        {
            if (GridZIndexEnvironmentButton != null)
                GridZIndexEnvironmentButton.Appearance = StatesController.GetControlAppearanceGridZIndex();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                Panel.SetZIndex(state[StateImageType.Background][StateImageSideType.Left], StatesController.GetBackgroundZIndex());
                Panel.SetZIndex(state[StateImageType.Background][StateImageSideType.Right], StatesController.GetBackgroundZIndex());
            }
        }

        public static void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (OverlayButton == null) return;
            StatesController.ToggleShowOverlay();
            OverlayButton.Appearance = StatesController.GetControlAppearanceOverlay();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                state[StateImageType.Overlay][StateImageSideType.Left].Visibility = StatesController.GetVisibilityOverlay();
                state[StateImageType.Overlay][StateImageSideType.Right].Visibility = StatesController.GetVisibilityOverlay();
            }
        }

        public static void TextGridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (TextGridEnvironmentButton == null) return;
            StatesController.ToggleShowTextGrid();
            TextGridEnvironmentButton.Appearance = StatesController.GetControlAppearanceTextGrid();

            EnvironmentController.dataPixelStorage.UpdateAfterStorage();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                state[StateImageType.TextGrid][StateImageSideType.Left].Visibility = StatesController.GetVisibilityTextGrid();
                state[StateImageType.TextGrid][StateImageSideType.Right].Visibility = StatesController.GetVisibilityTextGrid();
            }
        }

        #endregion Buttons Environment Controller


        #region Buttons Helpers

        public static void ResetEditButtons()
        {
            if (SingleButton != null)
                SingleButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            if (FillButton != null)
                FillButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            if (SelectButton != null)
                SelectButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            if (MoveButton != null)
                MoveButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            if (DeleteButton != null)
                DeleteButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            if (UndoButton != null)
                UndoButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            if (UndoAreaButton != null)
                UndoAreaButton.Appearance = StatesController.GetUnPressedButtonAppearance();
        }

        public static void ResetStatesButtons()
        {
            if (ChooseSingleStateButton != null)
                ChooseSingleStateButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            if (ChooseParallelStatesButton != null)
                ChooseParallelStatesButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            if (ChooseAllStatesButton != null)
                ChooseAllStatesButton.Appearance = StatesController.GetUnPressedButtonAppearance();
        }

        #endregion Buttons Helpers

        #endregion Buttons Toolbar Controller


        #region Updaters

        public static void SaveUpdate(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Save Update Hotkey");
        }

        #endregion Updaters

    }
}
