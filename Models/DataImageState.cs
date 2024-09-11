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
using System.IO;
using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using System.Diagnostics;

namespace AdaptiveSpritesDMItool.Models
{
    public class DataImageState
    {
        /// <summary>
        /// Main editable state.
        /// </summary>
        DMIState currentState;

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
            InitializeOverlayData();
        }
        public DataImageState(DMIState _state, DMIState _stateOverlay)
        {
            InitializeData(_state);
            InitializeOverlayData(_stateOverlay);
        }

        private void InitializeData(DMIState _state)
        {
            currentState = _state;
            pixelSize = ImageEncoder.GetPixelSizeFromResolution(heightCellsImage);
            WriteableBitmap bitmapUI = new WriteableBitmap(bitmapUISize.Width, bitmapUISize.Height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

            StateDirection[] stateDirections = StatesController.GetAllStateDirections(DirectionDepth.Four);
            foreach (StateDirection direction in stateDirections)
            {
                stateBMPdict.Add(direction, new Dictionary<StateImageType, Dictionary<StateImageSideType, WriteableBitmap>>());
                WriteableBitmap bitmap = ImageEncoder.GetBMPFromDMIState(currentState, direction);
                foreach (StateImageType imageType in initializedStateImageTypes)
                {
                    stateBMPdict[direction].Add(imageType, new Dictionary<StateImageSideType, WriteableBitmap>());
                    switch (imageType)
                    {
                        case StateImageType.Preview:
                            UpdateBitmaps(direction, imageType, bitmap);
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
        }

        public void InitializeOverlayData(DMIState _state)
        {
            if(_state.Width != currentState.Width || _state.Height != currentState.Height)
                throw new Exception("Overlay DMIState has different dimensions than main DMIState");

            ReplaceDMIState(_state, StateImageType.Overlay);
        }

        public void InitializeOverlayData()
        {
            StateDirection[] stateDirections = StatesController.GetAllStateDirections(currentState.DirectionDepth);
            foreach (StateDirection direction in stateDirections)
            {
                WriteableBitmap bitmap = stateBMPdict[direction][StateImageType.Preview][StateImageSideType.Left].Clone();
                UpdateBitmaps(direction, StateImageType.Overlay, bitmap);
            }
        }

        #endregion Initializers


        #region DMI State

        public void ReplaceDMIState(DMIState _state, StateImageType imageType)
        {
            StateDirection[] stateDirections = StatesController.GetAllStateDirections(currentState.DirectionDepth);
            foreach (StateDirection direction in stateDirections)
            {
                WriteableBitmap bitmap = ImageEncoder.GetBMPFromDMIState(_state, direction);
                UpdateBitmaps(direction, imageType, bitmap);
                //stateBMPdict[direction][imageType][StateImageSideType.Left] = bitmap;
                //stateBMPdict[direction][imageType][StateImageSideType.Right] = bitmap.Clone();

                // !!!!!!!!!!!! В АПДЕЙТ ЗАПИХНУТЬ ПРОВЕРКУ ПО КАЖДОМУ ПИКСЕЛЮ ???????

                string filename = "Test/Test1.png";
                using (FileStream stream5 = new FileStream(filename, FileMode.Create))
                {
                    PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                    encoder5.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder5.Save(stream5);
                }
            }


        }

        public void CombineDMIState(DMIState _state, StateImageType imageType)
        {

            StateDirection[] stateDirections = StatesController.GetAllStateDirections(currentState.DirectionDepth);
            foreach (StateDirection direction in stateDirections)
            {
                WriteableBitmap currentBitmap = stateBMPdict[direction][imageType][StateImageSideType.Left];
                WriteableBitmap bitmap = ImageEncoder.GetBMPFromDMIState(_state, direction);
                
                Rect destRect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                currentBitmap.Blit(destRect, bitmap, destRect);

                // !!!!!!!!!! Не совсем то что нужно соединение, но близко !!!!!!!!!
                stateBMPdict[direction][imageType][StateImageSideType.Right] = currentBitmap.Clone();


                //System.Drawing.Point[] points = ImageEncoder.GetPointsFromDMIState(_state);

                //foreach (System.Drawing.Point point in points)
                //{
                //    System.Windows.Media.Color color = DrawController.GetPointColor(bitmap, point);
                //    if (color.A == 0 || color == Colors.Transparent)
                //        continue;
                //    currentBitmap.SetPixel(point.X, point.Y, color);
                //}


                //var width = bitmap.PixelWidth;
                //var height = bitmap.PixelHeight;

                //var stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);
                //var bitmapData = new byte[height * stride];

                //Int32Rect sourceRect = new Int32Rect(0, 0, width, height);
                //int offset = 0;

                //currentBitmap.WritePixels(sourceRect, bitmapData, stride, offset);


                //stateBMPdict[direction][imageType][StateImageSideType.Right] = currentBitmap.Clone();
            }
        }

        #endregion DMI State


        #region Bitmap

        private void UpdateBitmaps(StateDirection direction, StateImageType imageType, WriteableBitmap bitmap)
        {
            stateBMPdict[direction][imageType][StateImageSideType.Left] = bitmap;
            stateBMPdict[direction][imageType][StateImageSideType.Right] = bitmap.Clone();
            var leftState = stateBMPdict[direction][imageType][StateImageSideType.Left];
            var rightState = stateBMPdict[direction][imageType][StateImageSideType.Right];

            // Updating the references so that we are "losing"
            StatesController.stateSourceDictionary[direction][imageType][StateImageSideType.Left].Source = leftState;
            StatesController.stateSourceDictionary[direction][imageType][StateImageSideType.Right].Source = rightState;
        }

        #endregion Bitmap
    }
}
