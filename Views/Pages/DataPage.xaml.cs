using AdaptiveSpritesDMItool.ViewModels.Pages;
using System.Collections.Concurrent;
using System.IO;
using Wpf.Ui.Controls;

namespace AdaptiveSpritesDMItool.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }
        private ConcurrentDictionary<(int x, int y), (int x, int y)> pixelStorage = new ConcurrentDictionary<(int x, int y), (int x, int y)>();

        public DataPage(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();


            //pixelStorage.TryAdd((x, y), (x, y));
        }


        public void ExportPixelStorageToCSV()
        {
            var csv = pixelStorage
                .Select(kvp => $"{kvp.Key.x},{kvp.Key.y},{kvp.Value.x},{kvp.Value.y}")
                .Aggregate((a, b) => $"{a}\n{b}");

            File.WriteAllText("pixel_storage.csv", csv);
        }

        public void ImportPixelStorageFromCSV()
        {
            pixelStorage.Clear();

            var lines = File.ReadAllLines("pixel_storage.csv");

            foreach (var line in lines)
            {
                var splitted = line.Split(',');
                var x1 = int.Parse(splitted[0]);
                var y1 = int.Parse(splitted[1]);
                var x2 = int.Parse(splitted[2]);
                var y2 = int.Parse(splitted[3]);

                pixelStorage[(x1, y1)] = (x2, y2);
            }
        }

        //public void ExportPixelStorageToJson()
        //{
        //    File.WriteAllText("pixel_storage.json", JsonConvert.SerializeObject(pixelStorage));
        //}

        //public void ImportPixelStorageFromJson()
        //{
        //    pixelStorage = JsonConvert.DeserializeObject<ConcurrentDictionary<(int x, int y), (int x, int y)>>("pixel_storage.json");
        //}
    }
}
