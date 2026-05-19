using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class MainWindow : Window
{
    private bool _isSynchronizingViewportScroll;
    private bool _isApplyingZoomAnchor;
    private bool _isViewportSyncSuspended;
    private int _zoomAnchorRequestVersion;
    private int _viewportSyncSuspensionVersion;
    private ScrollViewer? _activePanScrollViewer;
    private Point _viewportPanStartPoint;
    private double _viewportPanStartHorizontalOffset;
    private double _viewportPanStartVerticalOffset;

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

    private void TargetCellButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PixelCellViewModel cell })
        {
            ViewModel.HandleTargetCellPointerDown(cell);
            e.Handled = true;
        }
    }

    private void TargetCellButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: PixelCellViewModel cell })
        {
            ViewModel.HandleTargetCellPointerEnter(cell);
        }
    }

    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ComboBox { SelectedItem: WorkspaceThemeMode mode })
        {
            ViewModel.SettingsTab.SelectedThemeMode = mode;
            App.ApplyThemeMode(mode);
        }
    }

    private void ViewportModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ComboBox { SelectedItem: EditorViewportMode mode } &&
            ViewModel.SettingsTab.SelectedViewportMode != mode)
        {
            ViewModel.SettingsTab.SelectedViewportMode = mode;
        }
    }

    private void EditorSurfaceHost_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var previousZoom = Math.Max(ViewModel.ActiveEditorZoom, 0.001d);
        var cursorPosition = e.GetPosition(scrollViewer);
        var previousSurfaceOrigin = GetSurfaceOrigin(scrollViewer);
        var worldX = (scrollViewer.HorizontalOffset + cursorPosition.X - previousSurfaceOrigin.X) / previousZoom;
        var worldY = (scrollViewer.VerticalOffset + cursorPosition.Y - previousSurfaceOrigin.Y) / previousZoom;

        if (ViewModel.AdjustActiveEditorZoom(e.Delta))
        {
            var requestVersion = ++_zoomAnchorRequestVersion;
            _isViewportSyncSuspended = true;
            _viewportSyncSuspensionVersion = requestVersion;

            if (!scrollViewer.IsLoaded || requestVersion != _zoomAnchorRequestVersion)
            {
                return;
            }

            SourceViewportScrollViewer?.UpdateLayout();
            EditableViewportScrollViewer?.UpdateLayout();

            var currentZoom = Math.Max(ViewModel.ActiveEditorZoom, 0.001d);
            ApplyZoomAnchor(scrollViewer, cursorPosition, worldX, worldY, currentZoom);
            Dispatcher.BeginInvoke(() => ReleaseViewportSyncAfterZoom(scrollViewer, requestVersion), DispatcherPriority.ContextIdle);
            e.Handled = true;
        }
    }

    private void EditorViewportScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        _activePanScrollViewer = scrollViewer;
        _viewportPanStartPoint = e.GetPosition(scrollViewer);
        _viewportPanStartHorizontalOffset = scrollViewer.HorizontalOffset;
        _viewportPanStartVerticalOffset = scrollViewer.VerticalOffset;
        Mouse.OverrideCursor = System.Windows.Input.Cursors.ScrollAll;
        scrollViewer.CaptureMouse();
        e.Handled = true;
    }

    private void EditorViewportScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_activePanScrollViewer is null ||
            sender is not ScrollViewer scrollViewer ||
            !ReferenceEquals(scrollViewer, _activePanScrollViewer) ||
            e.MiddleButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentPoint = e.GetPosition(scrollViewer);
        var deltaX = currentPoint.X - _viewportPanStartPoint.X;
        var deltaY = currentPoint.Y - _viewportPanStartPoint.Y;

        scrollViewer.ScrollToHorizontalOffset(ClampViewportOffset(_viewportPanStartHorizontalOffset - deltaX, scrollViewer.ExtentWidth, scrollViewer.ViewportWidth));
        scrollViewer.ScrollToVerticalOffset(ClampViewportOffset(_viewportPanStartVerticalOffset - deltaY, scrollViewer.ExtentHeight, scrollViewer.ViewportHeight));
        e.Handled = true;
    }

    private void EditorViewportScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && sender is ScrollViewer scrollViewer && ReferenceEquals(scrollViewer, _activePanScrollViewer))
        {
            EndViewportPan();
            e.Handled = true;
        }
    }

    private void EditorViewportScrollViewer_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer && ReferenceEquals(scrollViewer, _activePanScrollViewer))
        {
            EndViewportPan();
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
        if (!TryCreateSurfaceCell(sender, e.GetPosition((IInputElement)sender), out var cell))
        {
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ViewModel.HandleTargetCellPointerEnter(cell);
            e.Handled = true;
            return;
        }

        ViewModel.HandleTargetSurfaceHover(cell);
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

    private void TargetSurface_MouseLeave(object sender, MouseEventArgs e) => ViewModel.HandleTargetSurfacePointerLeave();

    private void SourceViewportScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        => SynchronizeViewportScroll(SourceViewportScrollViewer, EditableViewportScrollViewer, e);

    private void EditableViewportScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        => SynchronizeViewportScroll(EditableViewportScrollViewer, SourceViewportScrollViewer, e);

    private void TargetSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not EditorSurfaceControl control)
        {
            return;
        }

        try
        {
            if (TryCreateSurfaceCell(sender, e.GetPosition(control), out var cell))
            {
                ViewModel.HandleTargetCellPointerUp(cell);
                e.Handled = true;
                return;
            }

            // Mouse released outside pixel grid during an active drag.
            // Complete the drag using the last hovered coordinate from the ViewModel.
            if (control.Surface is { } surface
                && ViewModel.EditableHoveredCoordinate is { } lastCoord)
            {
                ViewModel.HandleTargetCellPointerUp(new PixelCellViewModel(surface.Direction, lastCoord.X, lastCoord.Y));
                e.Handled = true;
            }
        }
        finally
        {
            control.ReleaseMouseCapture();
        }
    }

    private void TargetSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (TryCreateSurfaceCell(sender, e.GetPosition((IInputElement)sender), out var cell))
        {
            ViewModel.HandleTargetCellPointerDown(cell);
            ((EditorSurfaceControl)sender).CaptureMouse();
            e.Handled = true;
        }
    }

    private void TargetSurface_LostMouseCapture(object sender, MouseEventArgs e)
    {
        ViewModel.HandleTargetSurfacePointerLeave();
    }

    private void DirectionNavigatorItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedDirectionScope != DirectionScope.All)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: DirectionNavigatorItemViewModel item } &&
            ViewModel.ToggleDisplayedDirectionCommand.CanExecute(item.Direction))
        {
            ViewModel.ToggleDisplayedDirectionCommand.Execute(item.Direction);
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
            _isApplyingZoomAnchor ||
            _isViewportSyncSuspended ||
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

    private void ReleaseViewportSyncAfterZoom(ScrollViewer primary, int requestVersion)
    {
        if (requestVersion != _zoomAnchorRequestVersion || requestVersion != _viewportSyncSuspensionVersion)
        {
            return;
        }

        var secondary = ReferenceEquals(primary, SourceViewportScrollViewer)
            ? EditableViewportScrollViewer
            : SourceViewportScrollViewer;

        try
        {
            _isSynchronizingViewportScroll = true;
            if (secondary is not null)
            {
                secondary.ScrollToHorizontalOffset(ClampViewportOffset(primary.HorizontalOffset, secondary.ExtentWidth, secondary.ViewportWidth));
                secondary.ScrollToVerticalOffset(ClampViewportOffset(primary.VerticalOffset, secondary.ExtentHeight, secondary.ViewportHeight));
            }
        }
        finally
        {
            _isSynchronizingViewportScroll = false;
            _isViewportSyncSuspended = false;
        }
    }

    private void EndViewportPan()
    {
        var scrollViewer = _activePanScrollViewer;
        _activePanScrollViewer = null;

        if (scrollViewer is not null && scrollViewer.IsMouseCaptured)
        {
            scrollViewer.ReleaseMouseCapture();
        }

        Mouse.OverrideCursor = null;
    }

    private void ApplyZoomAnchor(ScrollViewer primary, Point cursorPosition, double worldX, double worldY, double zoom)
    {
        var secondary = ReferenceEquals(primary, SourceViewportScrollViewer)
            ? EditableViewportScrollViewer
            : SourceViewportScrollViewer;

        _isApplyingZoomAnchor = true;
        try
        {
            var primarySurfaceOrigin = GetSurfaceOrigin(primary);
            var horizontalOffset = ClampViewportOffset((worldX * zoom) + primarySurfaceOrigin.X - cursorPosition.X, primary.ExtentWidth, primary.ViewportWidth);
            var verticalOffset = ClampViewportOffset((worldY * zoom) + primarySurfaceOrigin.Y - cursorPosition.Y, primary.ExtentHeight, primary.ViewportHeight);

            primary.ScrollToHorizontalOffset(horizontalOffset);
            primary.ScrollToVerticalOffset(verticalOffset);

            if (secondary is not null)
            {
                var secondarySurfaceOrigin = GetSurfaceOrigin(secondary);
                secondary.ScrollToHorizontalOffset(
                    ClampViewportOffset((worldX * zoom) + secondarySurfaceOrigin.X - cursorPosition.X, secondary.ExtentWidth, secondary.ViewportWidth));
                secondary.ScrollToVerticalOffset(
                    ClampViewportOffset((worldY * zoom) + secondarySurfaceOrigin.Y - cursorPosition.Y, secondary.ExtentHeight, secondary.ViewportHeight));
            }
        }
        finally
        {
            _isApplyingZoomAnchor = false;
        }
    }

    private static Point GetSurfaceOrigin(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Content is not FrameworkElement contentHost)
        {
            return default;
        }

        var surface = FindDescendant<EditorSurfaceControl>(contentHost);
        if (surface is null)
        {
            return default;
        }

        return new Point(
            Math.Max(0d, (contentHost.ActualWidth - surface.ActualWidth) / 2d),
            Math.Max(0d, (contentHost.ActualHeight - surface.ActualHeight) / 2d));
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                return match;
            }

            var nestedMatch = FindDescendant<T>(child);
            if (nestedMatch is not null)
            {
                return nestedMatch;
            }
        }

        return null;
    }

    private static double ClampViewportOffset(double value, double extent, double viewport)
        => Math.Clamp(value, 0d, Math.Max(0d, extent - viewport));
}
