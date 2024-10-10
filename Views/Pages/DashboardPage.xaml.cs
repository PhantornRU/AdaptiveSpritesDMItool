using AdaptiveSpritesDMItool.ViewModels.Pages;
using DMISharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Bmp;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using SixLabors.ImageSharp.Formats;
using static System.Formats.Asn1.AsnWriter;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Drawing;
using Point = System.Drawing.Point;
using System.Globalization;
using System.Windows.Media.Effects;
using System.Windows.Interop;
using System.Windows.Controls;
using AdaptiveSpritesDMItool.Models;
using AdaptiveSpritesDMItool.Controllers;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public string FileNameNone { get; set; }
        public string FileNameHuman { get; set; }
        public string FileNameMonkey { get; set; }
        public string FileNameVox { get; set; }

        public DashboardViewModel ViewModel { get; }

        private string? choosenFileName;

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;

            FileNameNone = "None";
            FileNameHuman = "Human";
            FileNameMonkey = "Monkey";
            FileNameVox = "Vox";
            choosenFileName = FileNameNone;

            DataContext = this;

            InitializeComponent();

        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton pressed = (RadioButton)sender;
            if (pressed.Content == null) return;
            choosenFileName = pressed.Content.ToString();
            Debug.WriteLine(choosenFileName);
        }

        private void EnvironmentChanged(object sender, SelectionChangedEventArgs e)
        {
            Wpf.Ui.Controls.ListView? listView = sender as Wpf.Ui.Controls.ListView;
            if (listView == null) return;
            EnvironmentItem? item = listView.SelectedItem as EnvironmentItem;
            if (item == null)
            {
                Debug.WriteLine("Environment nullified.");
                listView.SelectedIndex = -1;
                return;
            }
            Debug.WriteLine($"Environment changed to {item.FileName} - {item.FilePath} - {item.LastModifiedDate}");
            EnvironmentController.LoadSettings(item);
        }
    }
}
