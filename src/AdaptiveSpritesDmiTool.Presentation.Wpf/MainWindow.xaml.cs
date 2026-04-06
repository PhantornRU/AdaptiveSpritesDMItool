using System.Windows;
using System.Windows.Input;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    private void SourceCellButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PixelCellViewModel cell })
        {
            ViewModel.HandleSourceCellPointerDown(cell);
            e.Handled = true;
        }
    }

    private void SourceCellButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: PixelCellViewModel cell })
        {
            ViewModel.HandleSourceCellPointerEnter(cell);
        }
    }

    private void SourceCellButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PixelCellViewModel cell })
        {
            ViewModel.HandleSourceCellPointerUp(cell);
            e.Handled = true;
        }
    }

    private void TargetCellButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PixelCellViewModel cell })
        {
            ViewModel.HandleTargetCellPointerUp(cell);
            e.Handled = true;
        }
    }

    private void BatchSourceTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        ViewModel.HandleBatchSourceSelection(e.NewValue as BatchSourceTreeItemViewModel);
    }
}
