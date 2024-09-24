using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.ViewModels.Windows;
using AdaptiveSpritesDMItool.Views.Pages;
using System.Diagnostics;
using System.Windows.Input;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AdaptiveSpritesDMItool.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            IPageService pageService,
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();

            SetPageService(pageService);

            navigationService.SetNavigationControl(RootNavigation);

            InitializeInputGestures();
        }


        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.SetPageService(pageService);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods


        #region Navigation

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        #endregion Navigation


        #region Input Gestures

        private void InitializeInputGestures()
        {
            SaveCommand.InputGestures.Add(ButtonsController.SaveGesture);

            SingleCommand.InputGestures.Add(ButtonsController.SingleGesture);
            FillCommand.InputGestures.Add(ButtonsController.FillGesture);

            MoveCommand.InputGestures.Add(ButtonsController.MoveGesture);
            SelectCommand.InputGestures.Add(ButtonsController.SelectGesture);

            DeleteCommand.InputGestures.Add(ButtonsController.DeleteGesture);
            UndoCommand.InputGestures.Add(ButtonsController.UndoGesture);
            UndoAreaCommand.InputGestures.Add(ButtonsController.UndoAreaGesture);
            UndoLastCommand.InputGestures.Add(ButtonsController.UndoLastGesture);
            //RedoCommand.InputGestures.Add(ButtonsController.RedoGesture);

            ChooseSingleStateCommand.InputGestures.Add(ButtonsController.ChooseSingleStateGesture);
            ChooseParallelStatesCommand.InputGestures.Add(ButtonsController.ChooseParallelStatesGesture);
            ChooseAllStatesCommand.InputGestures.Add(ButtonsController.ChooseAllStatesGesture);

            MirrorStatesCommand.InputGestures.Add(ButtonsController.MirrorStatesGesture);
            CentralizeStatesCommand.InputGestures.Add(ButtonsController.CentralizeStatesGesture);
            GridEnvironmentCommand.InputGestures.Add(ButtonsController.GridEnvironmentGesture);
            GridZIndexEnvironmentCommand.InputGestures.Add(ButtonsController.GridZIndexEnvironmentGesture);
            TextGridEnvironmentCommand.InputGestures.Add(ButtonsController.TextGridEnvironmentGesture);
            OverlayCommand.InputGestures.Add(ButtonsController.OverlayGesture);
        }

        #endregion Input Gestures


        #region Hotkey Commands

        public static RoutedCommand SaveCommand = new RoutedCommand();
        private void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.SaveUpdate(sender, e);

        }


        public static RoutedCommand SingleCommand = new RoutedCommand();
        private void SingleCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.SingleButton_Click(sender, e);
        }

        public static RoutedCommand FillCommand = new RoutedCommand();
        private void FillCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.FillButton_Click(sender, e);
        }


        public static RoutedCommand MoveCommand = new RoutedCommand();
        private void MoveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.MoveButton_Click(sender, e);
        }

        public static RoutedCommand SelectCommand = new RoutedCommand();
        private void SelectCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.SelectButton_Click(sender, e);
        }


        public static RoutedCommand DeleteCommand = new RoutedCommand();
        private void DeleteCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.DeleteButton_Click(sender, e);
        }


        public static RoutedCommand UndoCommand = new RoutedCommand();
        private void UndoCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.UndoButton_Click(sender, e);
        }

        public static RoutedCommand UndoAreaCommand = new RoutedCommand();
        private void UndoAreaCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.UndoAreaButton_Click(sender, e);
        }

        public static RoutedCommand UndoLastCommand = new RoutedCommand();
        private void UndoLastCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.UndoLastButton_Click(sender, e);
        }


        public static RoutedCommand ChooseSingleStateCommand = new RoutedCommand();
        private void ChooseSingleStateCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.ChooseSingleStateButton_Click(sender, e);
        }

        public static RoutedCommand ChooseParallelStatesCommand = new RoutedCommand();
        private void ChooseParallelStatesCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.ChooseParallelStatesButton_Click(sender, e);
        }

        public static RoutedCommand ChooseAllStatesCommand = new RoutedCommand();
        private void ChooseAllStatesCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.ChooseAllStatesButton_Click(sender, e);
        }


        public static RoutedCommand MirrorStatesCommand = new RoutedCommand();
        private void MirrorStatesCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.MirrorStatesButton_Click(sender, e);
        }

        public static RoutedCommand CentralizeStatesCommand = new RoutedCommand();
        private void CentralizeStatesCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.CentralizeStatesButton_Click(sender, e);
        }

        public static RoutedCommand GridEnvironmentCommand = new RoutedCommand();
        private void GridEnvironmentCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.GridEnvironmentButton_Click(sender, e);
        }

        public static RoutedCommand GridZIndexEnvironmentCommand = new RoutedCommand();
        private void GridZIndexEnvironmentCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.GridZIndexEnvironmentButton_Click(sender, e);
        }

        public static RoutedCommand TextGridEnvironmentCommand = new RoutedCommand();
        private void TextGridEnvironmentCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.TextGridEnvironmentButton_Click(sender, e);
        }

        public static RoutedCommand OverlayCommand = new RoutedCommand();
        private void OverlayCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonsController.OverlayButton_Click(sender, e);
        }


        #endregion Hotkey Commands
    }
}
