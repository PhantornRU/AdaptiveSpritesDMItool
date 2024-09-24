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
using Button = Wpf.Ui.Controls.Button;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    /// <summary>
    /// Interaction logic for StatesEditorPage.xaml
    /// </summary>
    public partial class StatesEditorPage : INavigableView<StatesEditorViewModel>
    {

        public StatesEditorViewModel ViewModel { get; }

        public StatesEditorPage(StatesEditorViewModel viewModel)
        {
            ViewModel = viewModel;

            // Pre Initializers
            InitializeComponent();
            InitializeComponentPaths();
            InitializeButtons();
            EnvironmentController.InitializeEnvironment();

            // Initializers
            InitializeStatusBar();
            InitializeSources();
            InitializeGrids();

            // Post Initializers
            ControllButtonsAvailability();

            TestFunction();

            DataContext = this;
        }

        #region Initializers

        private void InitializeComponentPaths()
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

        private void InitializeButtons()
        {
            ButtonsController.SingleButton = SingleButton;
            ButtonsController.FillButton = FillButton;

            ButtonsController.SelectButton = SelectButton;
            ButtonsController.MoveButton = MoveButton;

            ButtonsController.DeleteButton = DeleteButton;
            ButtonsController.UndoButton = UndoButton;
            ButtonsController.UndoAreaButton = UndoAreaButton;
            ButtonsController.UndoLastButton = UndoLastButton;

            ButtonsController.ChooseSingleStateButton = ChooseSingleStateButton;
            ButtonsController.ChooseParallelStatesButton = ChooseParallelStatesButton;
            ButtonsController.ChooseAllStatesButton = ChooseAllStatesButton;

            ButtonsController.CentralizeStatesButton = CentralizeStatesButton;
            ButtonsController.MirrorStatesButton = MirrorStatesButton;

            ButtonsController.GridEnvironmentButton = GridEnvironmentButton;
            ButtonsController.GridZIndexEnvironmentButton = GridZIndexEnvironmentButton;
            ButtonsController.TextGridEnvironmentButton = TextGridEnvironmentButton;

            ButtonsController.InitializeButtons();
            UpdateTooltipButtons();
        }

        private void UpdateTooltipButtons()
        {
            UpdateTooltip(SingleButton, ButtonsController.SingleGesture);
            UpdateTooltip(FillButton, ButtonsController.FillGesture);

            UpdateTooltip(SelectButton, ButtonsController.SelectGesture);
            UpdateTooltip(MoveButton, ButtonsController.MoveGesture);

            UpdateTooltip(DeleteButton, ButtonsController.DeleteGesture);
            UpdateTooltip(UndoButton, ButtonsController.UndoGesture);
            UpdateTooltip(UndoAreaButton, ButtonsController.UndoAreaGesture);
            UpdateTooltip(UndoLastButton, ButtonsController.UndoLastGesture);

            UpdateTooltip(ChooseSingleStateButton, ButtonsController.ChooseSingleStateGesture);
            UpdateTooltip(ChooseParallelStatesButton, ButtonsController.ChooseParallelStatesGesture);
            UpdateTooltip(ChooseAllStatesButton, ButtonsController.ChooseAllStatesGesture);

            UpdateTooltip(CentralizeStatesButton, ButtonsController.CentralizeStatesGesture);
            UpdateTooltip(MirrorStatesButton, ButtonsController.MirrorStatesGesture);

            UpdateTooltip(GridEnvironmentButton, ButtonsController.GridEnvironmentGesture);
            UpdateTooltip(GridZIndexEnvironmentButton, ButtonsController.GridZIndexEnvironmentGesture);
            UpdateTooltip(TextGridEnvironmentButton, ButtonsController.TextGridEnvironmentGesture);
        }

        private void UpdateTooltip(Button button, KeyGesture gesture)
        {
            // &#x0a; == \n
            string splitKey = " Hotkey: ";
            string? tooltip = button.ToolTip.ToString();
            if (tooltip == null) return;
            button.ToolTip = tooltip.Split(splitKey)[0]; // Remove last change
            button.ToolTip += $"{splitKey}{gesture.Modifiers.ToString().Replace(", ", "+")}+{gesture.Key}";
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
            ButtonsController.ControllButtonsAvailability();
        }

        #region Buttons Edit Controller

        private void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateTooltip(SingleButton, ButtonsController.SingleGesture);
            ButtonsController.SingleButton_Click(sender, e);
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.FillButton_Click(sender, e);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.DeleteButton_Click(sender, e);
        }
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.UndoButton_Click(sender, e);
        }

        private void UndoAreaButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.UndoAreaButton_Click(sender, e);
        }

        private void UndoLastButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.UndoLastButton_Click(sender, e);
        }

        #endregion Buttons Edit Controller


        #region Buttons Move Controller

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.MoveButton_Click(sender, e);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.SelectButton_Click(sender, e);
        }

        #endregion Buttons Move Controller


        #region Buttons States Controller

        private void ChooseSingleStateButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.ChooseSingleStateButton_Click(sender, e);
        }

        private void ChooseParallelStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.ChooseParallelStatesButton_Click(sender, e);
        }

        private void ChooseAllStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.ChooseAllStatesButton_Click(sender, e);
        }

        private void CentralizeStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.CentralizeStatesButton_Click(sender, e);
        }

        private void MirrorStatesButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.MirrorStatesButton_Click(sender, e);
        }

        #endregion Buttons States Controller


        #region Buttons Environment Controller

        private void GridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.GridEnvironmentButton_Click(sender, e);
        }

        private void GridZIndexEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.GridZIndexEnvironmentButton_Click(sender, e);
        }

        private void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.OverlayButton_Click(sender, e);
        }

        private void TextGridEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsController.TextGridEnvironmentButton_Click(sender, e);
        }


        #endregion Buttons Environment Controller

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

        private void StateChanged(object sender, SelectionChangedEventArgs e)
        {
            Wpf.Ui.Controls.ListView? listView = sender as Wpf.Ui.Controls.ListView;
            if (listView == null) return;
            StateItem? state = listView.SelectedItem as StateItem;

            ViewModel.StateChanged(state);
        }

        private void StateRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            StateItem state = button.DataContext as StateItem;
            ViewModel.RemoveState(state);
        }

        private int lastIndexConfig = 0;
        private void ConfigChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"Config Changed {lastIndexConfig}");
            Wpf.Ui.Controls.ListView? listView = sender as Wpf.Ui.Controls.ListView;
            if (listView == null) return;
            ConfigItem? config = listView.SelectedItem as ConfigItem;

            int index = listView.SelectedIndex;
            if (config == null)
                listView.SelectedIndex = lastIndexConfig;
            else
                lastIndexConfig = index;

            ViewModel.ConfigChanged(config, lastIndexConfig);
        }

        private void ClearConfigButton_Click(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Controls.ListView? listView = sender as Wpf.Ui.Controls.ListView;
            if (listView == null) return;
            listView.SelectedIndex = -1;
        }

        private void ConfigRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ConfigItem config = button.DataContext as ConfigItem;
            ViewModel.RemoveConfig(config);
        }

        #endregion List View

        #endregion Buttons Preview Toolbar Controller


        #region Testing

        private void TestFunction()
        {

        }

        #endregion Testing

    }
}
