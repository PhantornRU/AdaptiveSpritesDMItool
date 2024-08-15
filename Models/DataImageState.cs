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
using System.IO;

namespace AdaptiveSpritesDMItool.Models
{
    public class DataImageState
    {
        DMIState currentState;

        Dictionary<StateDirection, WriteableBitmap> stateBMPdictOriginal = new Dictionary<StateDirection, WriteableBitmap>();
        Dictionary<StateDirection, WriteableBitmap> stateBMPdictEdit = new Dictionary<StateDirection, WriteableBitmap>();

        public DataImageState(DMIState _state)
        {
            this.currentState = _state;

            // Preview
            stateBMPdictOriginal.Add(StateDirection.South, GetBMPFromDMIState(currentState, StateDirection.South));
            stateBMPdictOriginal.Add(StateDirection.North, GetBMPFromDMIState(currentState, StateDirection.North));
            stateBMPdictOriginal.Add(StateDirection.East, GetBMPFromDMIState(currentState, StateDirection.East));
            stateBMPdictOriginal.Add(StateDirection.West, GetBMPFromDMIState(currentState, StateDirection.West));

            // Edit
            stateBMPdictEdit = stateBMPdictOriginal.ToDictionary(entry => entry.Key,
                                               entry => (WriteableBitmap)entry.Value.Clone());
        }

        public WriteableBitmap GetBMPstate(StateDirection _stateDirection, bool _isEdited = false)
        {
            if (_isEdited)
                return stateBMPdictEdit[_stateDirection];
            else
                return stateBMPdictOriginal[_stateDirection];
        }


        #region Image Encoder

        private WriteableBitmap GetBMPFromDMIState(DMIState _state, StateDirection _stateDirection)
        {
            using Image<Rgba32>? imgState = _state.GetFrame(_stateDirection, 0);
            if (imgState is Image<Rgba32> valueOfImage)
                Console.WriteLine($"image state is {valueOfImage}");
            else
                Console.WriteLine("image state does not have a value");
            return GetBMPFromRGBA32(imgState);
        }

        private WriteableBitmap GetBMPFromRGBA32(Image<Rgba32> _imgState)
        {
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

        #endregion Image Encoder

    }
}
