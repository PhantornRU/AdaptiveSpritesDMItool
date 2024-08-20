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
using AdaptiveSpritesDMItool.Controllers;

namespace AdaptiveSpritesDMItool.Models
{
    public class DataImageState
    {
        /// <summary>
        /// Main editable state.
        /// </summary>
        DMIState currentState;

        /// <summary>
        /// Additional overlay displayed on top.
        /// </summary>
        DMIState currentStateOverlay;

        private int widthCellsImage = 32;
        private int heightCellsImage = 32;

        /// <summary>
        /// Pixel cell array sizes.
        /// </summary>
        public System.Drawing.Size imageCellsSize => new System.Drawing.Size(widthCellsImage, heightCellsImage);

        private int widthBitmapUI = 257;
        private int heightBitmapUI = 257;

        /// <summary>
        /// The sizes of the bitmap on which the interface elements are drawn.
        /// </summary>
        public System.Drawing.Size bitmapUISize => new System.Drawing.Size(widthBitmapUI, heightBitmapUI);

        /// <summary>
        /// The size of the cell on the grid canvas and selection.
        /// </summary>
        public int pixelSize = 8;

        /// <summary>
        /// Dictionary where all references to edited bitmaps are stored.
        /// </summary>
        public Dictionary<StateDirection, Dictionary<StateImageType, Dictionary<StateImageSideType, WriteableBitmap>>> 
            stateBMPdict = new Dictionary<StateDirection, Dictionary<StateImageType, Dictionary<StateImageSideType, WriteableBitmap>>>();

        /// <summary>
        /// Types of images that are initialized in stateBMPdict.
        /// </summary>
        private StateImageType[] initializedStateImageTypes = new StateImageType[] { StateImageType.Preview, StateImageType.Overlay, StateImageType.Selection };

        #region Initializers

        public DataImageState(DMIState _state)
        {
            InitializeData(_state);
        }
        public DataImageState(DMIState _state, DMIState _stateOverlay)
        {
            InitializeData(_state);
            InitializeOverlayData(_stateOverlay);
        }

        private void InitializeData(DMIState _state)
        {
            currentState = _state;
            pixelSize = GetPixelSizeFromResolution(heightCellsImage);
            WriteableBitmap bitmapUI = new WriteableBitmap(bitmapUISize.Width, bitmapUISize.Height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

            Dictionary<StateImageSideType, WriteableBitmap> backgroundDict = new Dictionary<StateImageSideType, WriteableBitmap>();

            StateDirection[] stateDirections = StatesController.allStateDirections;
            foreach (StateDirection direction in stateDirections)
            {
                stateBMPdict.Add(direction, new Dictionary<StateImageType, Dictionary<StateImageSideType, WriteableBitmap>>());
                WriteableBitmap bitmap = GetBMPFromDMIState(currentState, direction);
                foreach (StateImageType imageType in initializedStateImageTypes)
                {
                    stateBMPdict[direction].Add(imageType, new Dictionary<StateImageSideType, WriteableBitmap>());
                    switch (imageType)
                    {
                        case StateImageType.Preview:
                            stateBMPdict[direction][imageType].Add(StateImageSideType.Left, bitmap);
                            stateBMPdict[direction][imageType].Add(StateImageSideType.Right, bitmap.Clone());
                            break;
                        case StateImageType.Selection:
                            foreach(StateImageSideType imageSideType in Enum.GetValues(typeof(StateImageSideType)))
                                stateBMPdict[direction][imageType].Add(imageSideType, bitmapUI.Clone());
                            break;
                    }
                }
            }

            widthCellsImage = currentState.Width;
            heightCellsImage = currentState.Height;

            Debug.WriteLine($"Image - imageCellsSize: {imageCellsSize}; sizeUI: {imageCellsSize}; pixelSize: {pixelSize}");
        }

        public void InitializeOverlayData(DMIState _state)
        {
            if(_state.Width != currentState.Width || _state.Height != currentState.Height)
                throw new Exception("Overlay DMIState has different dimensions than main DMIState");
            currentStateOverlay = _state;

            StateDirection[] stateDirections = StatesController.allStateDirections;
            foreach (StateDirection direction in stateDirections)
            {
                WriteableBitmap bitmap = GetBMPFromDMIState(currentStateOverlay, direction);
                foreach(StateImageSideType imageSideType in Enum.GetValues(typeof(StateImageSideType)))
                    stateBMPdict[direction][StateImageType.Overlay].Add(imageSideType, bitmap);
            }
        }

        #endregion Initializers


        #region Image Encoder

        /// <summary>
        /// Get the state bitmap
        /// </summary>
        /// <param name="_state"></param>
        /// <param name="_stateDirection"></param>
        /// <returns></returns>
        private WriteableBitmap GetBMPFromDMIState(DMIState _state, StateDirection _stateDirection)
        {
            using Image<Rgba32> imgState = _state.GetFrame(_stateDirection, 0);
            if (imgState is Image<Rgba32> valueOfImage)
                Console.WriteLine($"image state is {valueOfImage}");
            else
                Console.WriteLine("image state does not have a value");
            return GetBMPFromRGBA32(imgState);
        }

        /// <summary>
        /// Converting Image RGBA32 to bitmap.
        /// </summary>
        /// <param name="_imgState"></param>
        /// <returns></returns>
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


        #region Helpers

        /// <summary>
        /// Dependence of pixel sizes on the canvas for better display.
        /// </summary>
        /// <param name="_pixelResolution"></param>
        /// <returns></returns>
        private int GetPixelSizeFromResolution(int _pixelResolution)
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
