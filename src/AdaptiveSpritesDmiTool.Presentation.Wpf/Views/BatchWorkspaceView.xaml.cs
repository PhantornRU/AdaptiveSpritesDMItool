using System.Diagnostics;
using System.Windows;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf.Views;

public partial class BatchWorkspaceView : WpfUserControl
{
    public BatchWorkspaceView()
    {
        InitializeComponent();
    }

    private async void BatchSourceTreeView_SelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not BatchWorkspaceViewModel viewModel)
        {
            return;
        }

        try
        {
            await viewModel.SelectSourceItemAsync(e.NewValue as BatchSourceTreeItemViewModel);
        }
        catch (OperationCanceledException)
        {
            // Selection preview refresh was superseded by another selection.
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[BatchWorkspaceView] Source selection failed: {exception}");
        }
    }
}
