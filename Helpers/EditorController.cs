using AdaptiveSpritesDMItool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;

namespace AdaptiveSpritesDMItool.Helpers
{
    internal static class EditorController
    {
        #region Grids

        public static WriteableBitmap MakeAndGetGrid(int _width = 257, int _height = 257, byte _alpha = 100, int _borderThickness = 2)
        {
            int pixelSize = 8;
            WriteableBitmap bitmap = new WriteableBitmap(_width, _height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

            System.Windows.Media.Color colorTemp = System.Windows.Media.Colors.Black;
            System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(_alpha, colorTemp.R, colorTemp.G, colorTemp.B);

            for (int i = 0; i < bitmap.PixelWidth; i += pixelSize)
            {
                for (int j = 0; j < bitmap.PixelHeight; j += pixelSize)
                {
                    Debug.WriteLine($"{i}, {j} (int)bitmap.Width == {bitmap.Width}, (int)bitmap.Height == {bitmap.Height}");
                    bitmap.DrawLine(i, j, i, bitmap.PixelHeight - j, color);
                    bitmap.DrawLine(j, i, bitmap.PixelWidth - j, i, color);
                }
            }

            for (int i = 0; i < _borderThickness; i++)
            {
                bitmap.DrawRectangle(0 + i, 0 + i, bitmap.PixelWidth - i - 1, bitmap.PixelHeight - i - 1, Colors.Black);
            }

            return bitmap;
        }

        #endregion Grids
    }
}
