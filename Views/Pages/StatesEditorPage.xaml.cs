using AdaptiveSpritesDMItool.ViewModels.Pages;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using DMISharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using System.Windows.Media.Imaging;
using System.IO;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Controllers;
using System.Windows.Controls;
using System.Windows.Media;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для StatesEditorPage.xaml
    /// </summary>
    public partial class StatesEditorPage : INavigableView<StatesEditorViewModel>
    {
        public StatesEditorViewModel ViewModel { get; }

        public StatesEditorPage(StatesEditorViewModel viewModel)
        {
            ViewModel = viewModel;

            // Pre Initializers
            InitializeComponent();
            EnvironmentController.InitializeEnvironment();

            // Initializers
            InitializeDictionaries();
            InitializeStatusBar();
            InitializeSources();
            InitializeGrids();

            // Post Initializers
            ControllButtonsAvailability();

            TestFunction();

            DataContext = this;
        }

        #region Initializers

        private void InitializeDictionaries()
        {
            StatesController.stateSourceDictionary = new Dictionary<StateDirection, Dictionary<StateImageType, Dictionary<StateImageSideType, System.Windows.Controls.Image>>>()
            {
                { StateDirection.South, new Dictionary<StateImageType, Dictionary<StateImageSideType, System.Windows.Controls.Image>>()
                    {
                        { StateImageType.Preview, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imagePreviewLeftSouth },
                                { StateImageSideType.Right, imagePreviewRightSouth }
                            }
                        },
                        { StateImageType.Background, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageBackgroundPreviewLeftSouth },
                                { StateImageSideType.Right, imageBackgroundPreviewRightSouth }
                            }
                        },
                        { StateImageType.Overlay, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageOverlayPreviewLeftSouth },
                                { StateImageSideType.Right, imageOverlayPreviewRightSouth }
                            }
                        },
                        { StateImageType.Selection, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageSelectionPreviewLeftSouth },
                                { StateImageSideType.Right, imageSelectionPreviewRightSouth }
                            }
                        },
                        { StateImageType.TextGrid, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageTextGridPreviewLeftSouth },
                                { StateImageSideType.Right, imageTextGridPreviewRightSouth }
                            }
                        }
                    }
                },

                { StateDirection.North, new Dictionary<StateImageType, Dictionary<StateImageSideType, System.Windows.Controls.Image>>()
                    {
                        { StateImageType.Preview, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imagePreviewLeftNorth },
                                { StateImageSideType.Right, imagePreviewRightNorth }
                            }
                        },
                        { StateImageType.Background, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageBackgroundPreviewLeftNorth },
                                { StateImageSideType.Right, imageBackgroundPreviewRightNorth }
                            }
                        },
                        { StateImageType.Overlay, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageOverlayPreviewLeftNorth },
                                { StateImageSideType.Right, imageOverlayPreviewRightNorth }
                            }
                        },
                        { StateImageType.Selection, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageSelectionPreviewLeftNorth },
                                { StateImageSideType.Right, imageSelectionPreviewRightNorth }
                            }
                        },
                        { StateImageType.TextGrid, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageTextGridPreviewLeftNorth },
                                { StateImageSideType.Right, imageTextGridPreviewRightNorth }
                            }
                        }
                    }
                },

                { StateDirection.East, new Dictionary<StateImageType, Dictionary<StateImageSideType, System.Windows.Controls.Image>>()
                    {
                        { StateImageType.Preview, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imagePreviewLeftEast },
                                { StateImageSideType.Right, imagePreviewRightEast }
                            }
                        },
                        { StateImageType.Background, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageBackgroundPreviewLeftEast },
                                { StateImageSideType.Right, imageBackgroundPreviewRightEast }
                            }
                        },
                        { StateImageType.Overlay, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageOverlayPreviewLeftEast },
                                { StateImageSideType.Right, imageOverlayPreviewRightEast }
                            }
                        },
                        { StateImageType.Selection, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageSelectionPreviewLeftEast },
                                { StateImageSideType.Right, imageSelectionPreviewRightEast }
                            }
                        },
                        { StateImageType.TextGrid, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageTextGridPreviewLeftEast },
                                { StateImageSideType.Right, imageTextGridPreviewRightEast }
                            }
                        }
                    }
                },

                { StateDirection.West, new Dictionary<StateImageType, Dictionary<StateImageSideType, System.Windows.Controls.Image>>()
                    {
                        { StateImageType.Preview, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imagePreviewLeftWest },
                                { StateImageSideType.Right, imagePreviewRightWest }
                            }
                        },
                        { StateImageType.Background, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageBackgroundPreviewLeftWest },
                                { StateImageSideType.Right, imageBackgroundPreviewRightWest }
                            }
                        },
                        { StateImageType.Overlay, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageOverlayPreviewLeftWest },
                                { StateImageSideType.Right, imageOverlayPreviewRightWest }
                            }
                        },
                        { StateImageType.Selection, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageSelectionPreviewLeftWest },
                                { StateImageSideType.Right, imageSelectionPreviewRightWest }
                            }
                        },
                        { StateImageType.TextGrid, new Dictionary<StateImageSideType, System.Windows.Controls.Image>()
                            {
                                { StateImageSideType.Left, imageTextGridPreviewLeftWest },
                                { StateImageSideType.Right, imageTextGridPreviewRightWest }
                            }
                        }
                    }
                }
            };

        }

        private void InitializeSources()
        {
            foreach (var (stateDirection, images) in StatesController.stateSourceDictionary)
            {
                images[StateImageType.Preview][StateImageSideType.Left].Source = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Left);
                images[StateImageType.Preview][StateImageSideType.Right].Source = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Right);

                images[StateImageType.Overlay][StateImageSideType.Left].Source = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Left);
                images[StateImageType.Overlay][StateImageSideType.Right].Source = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Right);
            }
        }

        private void InitializeGrids()
        {
            WriteableBitmap gridBitmap = EditorController.GetGridBackground();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                state[StateImageType.Background][StateImageSideType.Left].Source = gridBitmap;
                state[StateImageType.Background][StateImageSideType.Right].Source = gridBitmap;
            }

            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            DrawController.InitializeTextGrids(pixelsPerDip);
        }

        private void InitializeStatusBar()
        {
            StatesController.stateStatusBarDictionary[StatusBarType.State] = StatusBarStateText;
            StatesController.stateStatusBarDictionary[StatusBarType.SinglePoint] = StatusBarSinglePointText;
            StatesController.stateStatusBarDictionary[StatusBarType.MultiPoint] = StatusBarMultiPointText;
        }

        #endregion Initializers


        #region Mouse Controller

        #region Mouse Buttons - South Preview

        private void imagePreviewRightSouth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseDown(e, StateDirection.South, StateImageSideType.Right);
        }

        private void imagePreviewLeftSouth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseDown(e, StateDirection.South, StateImageSideType.Left);
        }

        private void imagePreviewRightSouth_MouseMove(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseMove(e, StateDirection.South, StateImageSideType.Right);
        }

        private void imagePreviewLeftSouth_MouseMove(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseMove(e, StateDirection.South, StateImageSideType.Left);
        }

        private void imagePreviewRightSouth_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseUp(e, StateDirection.South, StateImageSideType.Right);
        }

        private void imagePreviewLeftSouth_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseUp(e, StateDirection.South, StateImageSideType.Left);
        }

        private void imagePreviewRightSouth_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseEnter(e, StateDirection.South, StateImageSideType.Right);
        }

        private void imagePreviewLeftSouth_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseEnter(e, StateDirection.South, StateImageSideType.Left);
        }

        private void imagePreviewRightSouth_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseLeave(e, StateDirection.South, StateImageSideType.Right);
        }

        private void imagePreviewLeftSouth_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseLeave(e, StateDirection.South, StateImageSideType.Left);
        }

        #endregion Mouse Buttons - South Preview


        #region Mouse Buttons - North Preview

        private void imagePreviewRightNorth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseDown(e, StateDirection.North, StateImageSideType.Right);
        }

        private void imagePreviewLeftNorth_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseDown(e, StateDirection.North, StateImageSideType.Left);
        }

        private void imagePreviewRightNorth_MouseMove(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseMove(e, StateDirection.North, StateImageSideType.Right);
        }

        private void imagePreviewLeftNorth_MouseMove(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseMove(e, StateDirection.North, StateImageSideType.Left);
        }

        private void imagePreviewRightNorth_MouseUp(object sender, MouseButtonEventArgs e)
        {

            MouseController.state_MouseUp(e, StateDirection.North, StateImageSideType.Right);
        }

        private void imagePreviewLeftNorth_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseUp(e, StateDirection.North, StateImageSideType.Left);
        }

        private void imagePreviewRightNorth_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseEnter(e, StateDirection.North, StateImageSideType.Right);
        }

        private void imagePreviewLeftNorth_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseEnter(e, StateDirection.North, StateImageSideType.Left);
        }

        private void imagePreviewRightNorth_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseLeave(e, StateDirection.North, StateImageSideType.Right);
        }

        private void imagePreviewLeftNorth_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseLeave(e, StateDirection.North, StateImageSideType.Left);
        }


        #endregion Mouse Buttons - North Preview


        #region Mouse Buttons - East Preview

        private void imagePreviewRightEast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseDown(e, StateDirection.East, StateImageSideType.Right);
        }

        private void imagePreviewLeftEast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseDown(e, StateDirection.East, StateImageSideType.Left);
        }

        private void imagePreviewRightEast_MouseMove(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseMove(e, StateDirection.East, StateImageSideType.Right);
        }

        private void imagePreviewLeftEast_MouseMove(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseMove(e, StateDirection.East, StateImageSideType.Left);
        }

        private void imagePreviewRightEast_MouseUp(object sender, MouseButtonEventArgs e)
        {

            MouseController.state_MouseUp(e, StateDirection.East, StateImageSideType.Right);
        }

        private void imagePreviewLeftEast_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseUp(e, StateDirection.East, StateImageSideType.Left);
        }

        private void imagePreviewRightEast_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseEnter(e, StateDirection.East, StateImageSideType.Right);
        }

        private void imagePreviewLeftEast_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseEnter(e, StateDirection.East, StateImageSideType.Left);
        }

        private void imagePreviewRightEast_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseLeave(e, StateDirection.East, StateImageSideType.Right);
        }

        private void imagePreviewLeftEast_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseLeave(e, StateDirection.East, StateImageSideType.Left);
        }

        #endregion Mouse Buttons - East Preview


        #region Mouse Buttons - West Preview

        private void imagePreviewRightWest_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseDown(e, StateDirection.West, StateImageSideType.Right);
        }

        private void imagePreviewLeftWest_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseDown(e, StateDirection.West, StateImageSideType.Left);
        }

        private void imagePreviewRightWest_MouseMove(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseMove(e, StateDirection.West, StateImageSideType.Right);
        }

        private void imagePreviewLeftWest_MouseMove(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseMove(e, StateDirection.West, StateImageSideType.Left);
        }

        private void imagePreviewRightWest_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseUp(e, StateDirection.West, StateImageSideType.Right);
        }

        private void imagePreviewLeftWest_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseController.state_MouseUp(e, StateDirection.West, StateImageSideType.Left);
        }

        private void imagePreviewRightWest_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseEnter(e, StateDirection.West, StateImageSideType.Right);
        }

        private void imagePreviewLeftWest_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseEnter(e, StateDirection.West, StateImageSideType.Left);
        }

        private void imagePreviewRightWest_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseLeave(e, StateDirection.West, StateImageSideType.Right);
        }

        private void imagePreviewLeftWest_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseController.state_MouseLeave(e, StateDirection.West, StateImageSideType.Left);
        }

        #endregion Mouse Buttons - West Preview

        #endregion Mouse Controller


        #region Buttons Toolbar Controller

        private void ControllButtonsAvailability()
        {
            MirrorStatesButton.IsEnabled = StatesController.GetEnableStateMirrorButton();
            CentralizeStatesButton.IsEnabled = StatesController.GetEnableStateCentralizeButton();
            GridZIndexEnvironmentButton.IsEnabled = StatesController.GetEnableStateGridZIndexButton();
            GridZIndexEnvironmentUpdate();
            GridEnvironmentUpdate();
        }

        #region Buttons Edit Controller

        private void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            SingleButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Single);
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            FillButton.Appearance = StatesController.GetPressedButtonAppearance();

            StatesController.SetCurrentStateEditMode(StateEditType.Fill);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            DeleteButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Delete);
        }
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            UndoButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Undo);
        }

        private void UndoAreaButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            UndoAreaButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.UndoArea);
        }

        #endregion Buttons Edit Controller


        #region Buttons Move Controller

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            MoveButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Move);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            ResetEditButtons();
            SelectButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateEditMode(StateEditType.Select);
        }

        #endregion Buttons Move Controller


        #region Buttons States Controller

        private void ChooseSingleStateButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseSingleStateButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.Single);
            ControllButtonsAvailability();
        }

        private void ChooseParallelStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseParallelStatesButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.Parallel);
            ControllButtonsAvailability();
        }

        private void ChooseAllStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ResetStatesButtons();
            ChooseAllStatesButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStateQuantityMode(StateQuantityType.All);
            ControllButtonsAvailability();
        }

        private void CentralizeStatesButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleCentralizedState();
            CentralizeStatesButton.Appearance = StatesController.GetControlAppearanceCentralize();
        }

        private void MirrorStatesButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleMirroredState();
            MirrorStatesButton.Appearance = StatesController.GetControlAppearanceMirror();
            ControllButtonsAvailability();
        }

        #endregion Buttons States Controller


        #region Buttons Environment Controller

        private void GridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleShowGrid();
            GridEnvironmentUpdate();
        }

        private void GridEnvironmentUpdate()
        {
            GridEnvironmentButton.Appearance = StatesController.GetControlAppearanceGrid();
            GridZIndexEnvironmentButton.IsEnabled = StatesController.GetEnableStateGridZIndexButton();
            TextGridEnvironmentButton.IsEnabled = StatesController.GetEnableStateTextGridButton();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                state[StateImageType.Background][StateImageSideType.Left].Visibility = StatesController.GetVisibilityGrid();
                state[StateImageType.Background][StateImageSideType.Right].Visibility = StatesController.GetVisibilityGrid();

                state[StateImageType.TextGrid][StateImageSideType.Left].Visibility = StatesController.GetVisibilityTextGrid();
                state[StateImageType.TextGrid][StateImageSideType.Right].Visibility = StatesController.GetVisibilityTextGrid();
            }
        }

        private void GridZIndexEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleShowAboveGrid();
            GridZIndexEnvironmentUpdate();
        }

        private void GridZIndexEnvironmentUpdate()
        {
            GridZIndexEnvironmentButton.Appearance = StatesController.GetControlAppearanceGridZIndex();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                Panel.SetZIndex(state[StateImageType.Background][StateImageSideType.Left], StatesController.GetBackgroundZIndex());
                Panel.SetZIndex(state[StateImageType.Background][StateImageSideType.Right], StatesController.GetBackgroundZIndex());
            }
        }

        private void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            StatesController.ToggleShowOverlay();
            OverlayButton.Appearance = StatesController.GetControlAppearanceOverlay();

            foreach (var state in StatesController.stateSourceDictionary.Values)
            {
                state[StateImageType.Overlay][StateImageSideType.Left].Visibility = StatesController.GetVisibilityOverlay();
                state[StateImageType.Overlay][StateImageSideType.Right].Visibility = StatesController.GetVisibilityOverlay();
            }
        }

        private void TextGridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
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

        private void ResetEditButtons()
        {
            SingleButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            FillButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            SelectButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            MoveButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            DeleteButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            UndoButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            UndoAreaButton.Appearance = StatesController.GetUnPressedButtonAppearance();
        }

        private void ResetStatesButtons()
        {
            ChooseSingleStateButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            ChooseParallelStatesButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            ChooseAllStatesButton.Appearance = StatesController.GetUnPressedButtonAppearance();
        }

        #endregion Buttons Helpers

        #endregion Buttons Toolbar Controller


        #region Buttons Preview Toolbar Controller

        private void LeftPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPreviewButtons();
            LeftPreviewButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStatePreviewMode(StatePreviewType.Left);
        }

        private void RightPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPreviewButtons();
            RightPreviewButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStatePreviewMode(StatePreviewType.Right);
        }

        private void OverlayPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPreviewButtons();
            OverlayPreviewButton.Appearance = StatesController.GetPressedButtonAppearance();
            StatesController.SetCurrentStatePreviewMode(StatePreviewType.Overlay);
        }

        #region Buttons Helpers

        private void ResetPreviewButtons()
        {
            LeftPreviewButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            RightPreviewButton.Appearance = StatesController.GetUnPressedButtonAppearance();
            OverlayPreviewButton.Appearance = StatesController.GetUnPressedButtonAppearance();
        }

        #endregion Buttons Helpers


        #region List View

        private int lastIndex = 0;
        private void ConfigChanged(object sender, SelectionChangedEventArgs e)
        {
            Wpf.Ui.Controls.ListView? listView = sender as Wpf.Ui.Controls.ListView;
            if (listView == null) return;
            ConfigItem? config = listView.SelectedItem as ConfigItem;

            Debug.WriteLine(listView.SelectedIndex);
            int index = listView.SelectedIndex;
            if (config == null)
                listView.SelectedIndex = lastIndex;
            else
                lastIndex = index;

            ViewModel.ConfigChanged(config);
        }

        private void ClearConfigButton_Click(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Controls.ListView? listView = sender as Wpf.Ui.Controls.ListView;
            if (listView == null) return;
            listView.SelectedIndex = -1;
        }

        #endregion List View

        #endregion Buttons Preview Toolbar Controller


        #region Hotkeys

        public void ToolAdd1_Shortcut(Object sender, ExecutedRoutedEventArgs e)
        {
            SingleButton_Click(sender, e);
        }

        public void ToolAdd2_Shortcut(Object sender, ExecutedRoutedEventArgs e)
        {
            FillButton_Click(sender, e);
        }

        public void ToolSelect1_Shortcut(Object sender, ExecutedRoutedEventArgs e)
        {
            MoveButton_Click(sender, e);
        }

        public void ToolSelect2_Shortcut(Object sender, ExecutedRoutedEventArgs e)
        {
            SelectButton_Click(sender, e);
        }

        public void ToolUndo1_Shortcut(Object sender, ExecutedRoutedEventArgs e)
        {
            DeleteButton_Click(sender, e);
        }

        public void ToolUndo2_Shortcut(Object sender, ExecutedRoutedEventArgs e)
        {
            UndoButton_Click(sender, e);
        }

        public void ToolUndo3_Shortcut(Object sender, ExecutedRoutedEventArgs e)
        {
            UndoAreaButton_Click(sender, e);
        }

        #endregion Hotkeys


        #region Testing

        private void TestFunction()
        {

        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentController.dataPixelStorage.SavePixelStorage(EnvironmentController.GetPixelStoragePath() + "Test");
        }

        private void TestButton2_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentController.dataPixelStorage.LoadPixelStorageToEnvironment(EnvironmentController.GetPixelStoragePath() + "Test");
        }

        #endregion Testing

    }
}
