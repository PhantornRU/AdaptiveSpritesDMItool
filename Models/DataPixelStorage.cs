using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdaptiveSpritesDMItool.Models
{
    public class DataPixelStorage
    {
        private ConcurrentDictionary<(int x, int y), (int x, int y)> pixelStorage = new ConcurrentDictionary<(int x, int y), (int x, int y)>();

        public DataPixelStorage((int x, int y)[] initialPoints)
        {
            foreach (var point in initialPoints)
            {
                pixelStorage[point] = point;
            }
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
