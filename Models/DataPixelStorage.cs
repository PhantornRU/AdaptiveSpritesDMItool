using AdaptiveSpritesDMItool.Controllers;
using DMISharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace AdaptiveSpritesDMItool.Models
{
    public class DataPixelStorage
    {
        public ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>> pixelStorages = 
            new ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>>();

        public DataPixelStorage(string path, int width, int height)
        {
            if (!File.Exists($"{path}.csv"))
            {
                var initialPoints = Enumerable.Range(0, width * height)
                    .Select(i => (x: i % width, y: i / width))
                    .ToArray();

                foreach (var direction in StatesController.allStateDirection(DirectionDepth.Four).Cast<StateDirection>())
                {
                    pixelStorages[direction] = new ConcurrentDictionary<(int x, int y), (int x, int y)>(
                        initialPoints.Select(point => new KeyValuePair<(int x, int y), (int x, int y)>(point, point))
                    );
                }

                // !!!!!!!TEST!!!!!!!!!!!!!!!
                //ChangePoint(StateDirection.South, new(15, 15), new(22, 22));
                //ExportPixelStorageToCSV(path + "Test");
                return;
            }
            LoadPixelStorageToEnvironment(path);
        }


        #region Edit

        public void ChangePoint(StateDirection direction, (int x, int y) point, (int x, int y) pointMod)
        {
            pixelStorages[direction][point] = pointMod;
        }

        #endregion Edit


        #region File IO



        public void SavePixelStorage(string path)
        {
            ExportPixelStorageToCSV(path);
        }

        public void LoadPixelStorageToEnvironment(string path)
        {
            ImportPixelStorageFromCSV(path);
            DrawPixelStorageAtBitmaps();
        }

        public void DrawPixelStorageAtBitmaps()
        {
            var points = pixelStorages.SelectMany(p => p.Value.Select(p2 => (p.Key, p2.Key, p2.Value)));
            EditorController.DrawPixelStorageAtBitmaps(points);
        }

        #region Data table

        private void ExportPixelStorageToCSV(string path)
        {
            var csv = string.Join(
                Environment.NewLine,
                StatesController.allStateDirection(DirectionDepth.Four).Cast<StateDirection>().Select(direction =>
                {
                    var csvForDirection = string.Join(
                        Environment.NewLine,
                        pixelStorages[direction].Select(kvp => $"{direction.ToString()},{kvp.Key.x},{kvp.Key.y},{kvp.Value.x},{kvp.Value.y}")
                    );

                    return csvForDirection;
                })
            );

            File.WriteAllText($"{path}.csv", csv);
        }

        private void ImportPixelStorageFromCSV(string path)
        {
            var lines = File.ReadAllLines($"{path}.csv");

            foreach (var line in lines)
            {
                var splitted = line.Split(',');
                var direction = (StateDirection)Enum.Parse(typeof(StateDirection), splitted[0]);
                var x1 = int.Parse(splitted[1]);
                var y1 = int.Parse(splitted[2]);
                var x2 = int.Parse(splitted[3]);
                var y2 = int.Parse(splitted[4]);

                pixelStorages[direction][(x1, y1)] = (x2, y2);
            }
        }

        #endregion Data table

        #endregion File IO
    }
}

