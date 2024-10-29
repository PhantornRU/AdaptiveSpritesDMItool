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
using AdaptiveSpritesDMItool.Services;
using Microsoft.Extensions.Hosting;
using AdaptiveSpritesDMItool.Views.Windows;
using System;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public string FileNameNone { get; set; }
        public string FileNameHuman { get; set; }
        public string FileNameMonkey { get; set; }
        public string FileNameVox { get; set; }

        public DashboardViewModel ViewModel { get; }

        private string? choosenPresetFileName;

        private int defaultResolution = 32;

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;

            FileNameNone = "None";
            FileNameHuman = "Human";
            FileNameMonkey = "Monkey";
            FileNameVox = "Vox";
            choosenPresetFileName = FileNameNone;

            DataContext = this;

            InitializeComponent();

            TextBoxResolutionX.Text = defaultResolution.ToString();
            TextBoxResolutionY.Text = defaultResolution.ToString();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton pressed = (RadioButton)sender;
            if (pressed.Content == null) return;
            choosenPresetFileName = pressed.Content.ToString();
            Debug.WriteLine(choosenPresetFileName);
        }

        private void EnvironmentChanged(object sender, SelectionChangedEventArgs e)
        {
            Wpf.Ui.Controls.ListView? listView = sender as Wpf.Ui.Controls.ListView;
            if (listView == null) return;
            EnvironmentItem? environment = listView.SelectedItem as EnvironmentItem;
            if (environment == null)
            {
                Debug.WriteLine("Environment nullified.");
                listView.SelectedIndex = -1;
                return;
            }

            Debug.WriteLine($"Environment changed to {environment.FileName} - {environment.FilePath} - {environment.LastModifiedDate}");
            listView.SelectedIndex = -1;

            var window = Window.GetWindow(App.Current.MainWindow) as MainWindow;
            window.Navigate(typeof(Views.Pages.StatesEditorPage));

            bool loaded = EnvironmentController.LoadSettings(environment);
            if (!loaded) return;
            AllowToPressSaveButtons();
            EnvironmentController.SetSaveFile(environment);

        }

        private void NewEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            AllowToPressSaveButtons();
            EnvironmentController.SetCurrentResolution();
        }

        private void AllowToPressSaveButtons()
        {
            SaveButton.IsEnabled = true;
            SaveAsButton.IsEnabled = true;
        }


        #region Text Changed Event Handlers

        // TextChangedEventHandler delegate method for resolution changes.
        private void TextResolutionChangedEventHandler(object sender, TextChangedEventArgs args, Action<int> setResolution, Func<int> getDefaultResolution)
        {
            if (sender is not Wpf.Ui.Controls.TextBox textBox) return;

            if (int.TryParse(textBox.Text, out int value))
            {
                setResolution(value);
            }
            else
            {
                if(!string.IsNullOrEmpty(textBox.Text))
                    textBox.Text = getDefaultResolution().ToString();
                setResolution(getDefaultResolution());
                //textBox.Text = getDefaultResolution().ToString();
                Debug.WriteLine("Invalid resolution input, not an integer. Revert to default resolution value.");
            }
        }

        private void TextResolutionXChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            TextResolutionChangedEventHandler(sender, args, EnvironmentController.SetResolutionX, () => EnvironmentController.dataImageState?.imageCellsSize.Width ?? defaultResolution);
        }

        private void TextResolutionYChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            TextResolutionChangedEventHandler(sender, args, EnvironmentController.SetResolutionY, () => EnvironmentController.dataImageState?.imageCellsSize.Height ?? defaultResolution);
        }

        #endregion Text Changed Event Handlers
    }
}
