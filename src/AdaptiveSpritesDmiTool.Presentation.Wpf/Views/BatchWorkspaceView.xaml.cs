using System.Windows;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf.Views;

public partial class BatchWorkspaceView : WpfUserControl
{
    public BatchWorkspaceView()
    {
        InitializeComponent();
    }

    private void BatchFolderTreeView_SelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not BatchWorkspaceViewModel viewModel)
        {
            return;
        }

        viewModel.SelectFolder(e.NewValue as BatchFolderTreeItemViewModel);
    }
}
