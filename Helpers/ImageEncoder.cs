using DMISharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;

namespace AdaptiveSpritesDMItool.Helpers
{
    internal static class ImageEncoder
    {
        #region DMI

        /// <summary>
        /// Get the state bitmap
        /// </summary>
        /// <param name="_state"></param>
        /// <param name="_stateDirection"></param>
        /// <returns></returns>
        public static WriteableBitmap GetBMPFromDMIState(DMIState _state, StateDirection _stateDirection)
        {
            using Image<Rgba32>? imgState = _state.GetFrame(_stateDirection, 0);
            if (imgState != null && 
                (imgState is Image<Rgba32> valueOfImage))
            {
                Debug.WriteLine($"image state is {valueOfImage}");
                return GetBMPFromRGBA32(imgState);
            }
            throw new Exception("image state is null");
            //Console.WriteLine("image state does not have a value");
            //return new WriteableBitmap(1, 1, 1, 1, PixelFormats.Bgra32, null);
        }

        #endregion DMI


        #region RGBA32

        /// <summary>
        /// Converting Image RGBA32 to bitmap.
        /// </summary>
        /// <param name="_imgState"></param>
        /// <returns></returns>
        private static WriteableBitmap GetBMPFromRGBA32(Image<Rgba32> _imgState)
        {
            if (_imgState == null) throw new ArgumentNullException(nameof(_imgState));
            var bmp = new WriteableBitmap(_imgState.Width, _imgState.Height, _imgState.Metadata.HorizontalResolution, _imgState.Metadata.VerticalResolution, PixelFormats.Bgra32, null);

            bmp.Lock();
            try
            {

                using Image<Rgba32> _image = _imgState;
                _image.ProcessPixelRows(accessor =>
                {
                    var backBuffer = bmp.BackBuffer;

                    for (var y = 0; y < _imgState.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                        for (var x = 0; x < _imgState.Width; x++)
                        {
                            var backBufferPos = backBuffer + (y * _imgState.Width + x) * 4;
                            var rgba = pixelRow[x];
                            var color = rgba.A << 24 | rgba.R << 16 | rgba.G << 8 | rgba.B;

                            System.Runtime.InteropServices.Marshal.WriteInt32(backBufferPos, color);
                        }
                    }
                });

                bmp.AddDirtyRect(new Int32Rect(0, 0, _imgState.Width, _imgState.Height));
            }
            finally
            {
                bmp.Unlock();
            }
            return bmp;
        }

        #endregion RGBA32


        #region Helpers

        /// <summary>
        /// Dependence of pixel sizes on the canvas for better display.
        /// </summary>
        /// <param name="_pixelResolution"></param>
        /// <returns></returns>
        public static int GetPixelSizeFromResolution(int _pixelResolution)
        {
            int pixelSize = 8;
            if (_pixelResolution == 64)
            {
                pixelSize = 4;
            }
            else if (_pixelResolution == 16)
            {
                pixelSize = 16;
            }
            return pixelSize;
        }

        #endregion Helpers

    }
}
