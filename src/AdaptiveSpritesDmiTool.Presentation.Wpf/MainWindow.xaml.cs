using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

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

    private void EditorSurfaceHost_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel.AdjustActiveEditorZoom(e.Delta))
        {
            e.Handled = true;
        }
    }

    private void SourceSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (TryCreateSurfaceCell(sender, e.GetPosition((IInputElement)sender), out var cell))
        {
            ViewModel.HandleSourceCellPointerDown(cell);
            e.Handled = true;
        }
    }

    private void SourceSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (!TryCreateSurfaceCell(sender, e.GetPosition((IInputElement)sender), out var cell))
        {
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ViewModel.HandleSourceCellPointerEnter(cell);
            e.Handled = true;
            return;
        }

        ViewModel.HandleSourceSurfaceHover(cell);
    }

    private void TargetSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (TryCreateSurfaceCell(sender, e.GetPosition((IInputElement)sender), out var cell))
        {
            ViewModel.HandleTargetSurfaceHover(cell);
        }
    }

    private void SourceSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (TryCreateSurfaceCell(sender, e.GetPosition((IInputElement)sender), out var cell))
        {
            ViewModel.HandleSourceCellPointerUp(cell);
            e.Handled = true;
        }
    }

    private void SourceSurface_MouseLeave(object sender, MouseEventArgs e) => ViewModel.HandleSourceSurfacePointerLeave();

    private void TargetSurface_MouseLeave(object sender, MouseEventArgs e) => ViewModel.HandleSourceSurfacePointerLeave();

    private void TargetSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (TryCreateSurfaceCell(sender, e.GetPosition((IInputElement)sender), out var cell))
        {
            ViewModel.HandleTargetCellPointerUp(cell);
            e.Handled = true;
        }
    }

    private static bool TryCreateSurfaceCell(object sender, Point position, out PixelCellViewModel cell)
    {
        if (sender is EditorSurfaceControl { Surface: { } surface } control && control.TryGetCoordinate(position, out var coordinate))
        {
            cell = new PixelCellViewModel(surface.Direction, coordinate.X, coordinate.Y);
            return true;
        }

        cell = null!;
        return false;
    }
}
