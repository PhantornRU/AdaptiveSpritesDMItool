using System.Windows;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}