using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class MainWindow : Window
{
    private bool _isSynchronizingViewportScroll;

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

    private void SourceViewportScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        => SynchronizeViewportScroll(SourceViewportScrollViewer, EditableViewportScrollViewer, e);

    private void EditableViewportScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        => SynchronizeViewportScroll(EditableViewportScrollViewer, SourceViewportScrollViewer, e);

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

    private void SynchronizeViewportScroll(ScrollViewer source, ScrollViewer target, ScrollChangedEventArgs e)
    {
        if (_isSynchronizingViewportScroll ||
            (Math.Abs(e.HorizontalChange) < 0.01d && Math.Abs(e.VerticalChange) < 0.01d) ||
            target is null)
        {
            return;
        }

        _isSynchronizingViewportScroll = true;
        try
        {
            if (Math.Abs(target.HorizontalOffset - source.HorizontalOffset) >= 0.1d)
            {
                target.ScrollToHorizontalOffset(source.HorizontalOffset);
            }

            if (Math.Abs(target.VerticalOffset - source.VerticalOffset) >= 0.1d)
            {
                target.ScrollToVerticalOffset(source.VerticalOffset);
            }
        }
        finally
        {
            _isSynchronizingViewportScroll = false;
        }
    }
}
