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
using System.Windows.Media.Media3D;
using Point = System.Drawing.Point;
using Color = System.Windows.Media.Color;
using SixLabors.ImageSharp.ColorSpaces;

namespace AdaptiveSpritesDMItool.Resources
{
    public class DataPixelStorage
    {
        public ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>> pixelStorages =
            new ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>>();

        bool wasUpdated = false;

        public DataPixelStorage(string path, int width, int height)
        {
            string configFormat = EnvironmentController.configFormat;
            if (!File.Exists($"{path}.{configFormat}"))
            {
                FillDataSimilarPoints(width, height);
                return;
            }
            LoadPixelStorageToEnvironment(path);
        }


        #region Edit

        public void ChangePoint(StateDirection direction, (int x, int y) point, (int x, int y) pointMod)
        {
            pixelStorages[direction][point] = pointMod;
            wasUpdated = true;
        }

        private void FillDataSimilarPoints(int width, int height)
        {
            var initialPoints = Enumerable.Range(0, width * height)
                .Select(i => (x: i % width, y: i / width))
                .ToArray();

            foreach (var direction in StatesController.GetAllStateDirections(DirectionDepth.Four).Cast<StateDirection>())
            {
                pixelStorages[direction] = new ConcurrentDictionary<(int x, int y), (int x, int y)>(
                    initialPoints.Select(point => new KeyValuePair<(int x, int y), (int x, int y)>(point, point))
                );
            }
            wasUpdated = true;
        }



        public Point GetPointStorage(StateDirection direction, Point point)
        {
            var storagePoint = ConvertToStoragePoint(point);
            return ConvertFromStoragePoint(pixelStorages[direction][storagePoint]);
        }

        public (int x, int y) GetPointStorage(StateDirection direction, (int x, int y) point)
        {
            return pixelStorages[direction][point];
        }

        private Point ConvertFromStoragePoint((int x, int y) point)
        {
            return new Point(point.x, point.y);
        }

        private (int x, int y) ConvertToStoragePoint(Point point)
        {
            return (point.X, point.Y);
        }

        #endregion Edit


        #region Visualize

        public void UpdateAfterStorage()
        {
            if (!wasUpdated) return;

            var stateDirections = StatesController.GetStateDirections();
            DrawController.RenderTextGrids(stateDirections);
            wasUpdated = false;
        }

        #endregion Visualize


        #region File IO



        public void SavePixelStorage(string path)
        {
            ExportPixelStorage(path);
        }

        public void LoadPixelStorageToEnvironment(string path)
        {
            ImportPixelStorage(path);
            DrawPixelStorageAtBitmaps();
        }

        public void DrawPixelStorageAtBitmaps()
        {
            var points = pixelStorages.SelectMany(p => p.Value.Select(p2 => (p.Key, p2.Key, p2.Value)));
            DrawController.DrawPixelStorageAtBitmaps(points);
            wasUpdated = true;
            UpdateAfterStorage();
        }

        #region Data table

        private void ExportPixelStorage(string path)
        {
            var csv = string.Join(
                Environment.NewLine,
                StatesController.GetAllStateDirections(DirectionDepth.Four).Cast<StateDirection>().Select(direction =>
                {
                    var csvForDirection = string.Join(
                        Environment.NewLine,
                        pixelStorages[direction].Select(kvp => $"{direction.ToString()},{kvp.Key.x},{kvp.Key.y},{kvp.Value.x},{kvp.Value.y}")
                    );

                    return csvForDirection;
                })
            );

            path = CorrectPath(path);
            File.WriteAllText(path, csv);
        }

        private void ImportPixelStorage(string path)
        {
            path = CorrectPath(path);
            var lines = File.ReadAllLines(path);

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


        #region Helpers

        private string CorrectPath(string path)
        {
            string configFormat = EnvironmentController.configFormat;
            if (!path.Contains(configFormat))
                path += configFormat;
            return path;
        }

        #endregion Helpers

        #endregion File IO
    }
}

