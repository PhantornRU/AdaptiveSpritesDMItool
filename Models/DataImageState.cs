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
using System.Data;
using Size = System.Drawing.Size;

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
        public Size imageCellsSize => new Size(widthCellsImage, heightCellsImage);

        private int widthBitmapUI = 257;
        private int heightBitmapUI = 257;

        /// <summary>
        /// The sizes of the bitmap on which the interface elements are drawn.
        /// </summary>
        public Size bitmapUISize => new Size(widthBitmapUI, heightBitmapUI);

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

        public DataImageState(DMIState _state, DMIState _landmarkState = null, DMIState _overlayState = null)
        {
            InitializeData(_state);
            InitializeLandmarkData(_landmarkState);
            InitializeOverlayData(_overlayState);
        }

        private void InitializeData(DMIState _state)
        {
            Debug.WriteLine($"Initialize Data: {_state.Name}");
            currentState = _state;
            UpdateWidthCellsImage(currentState.Width, currentState.Height);

            WriteableBitmap bitmapUI = new WriteableBitmap(
                bitmapUISize.Width, 
                bitmapUISize.Height, 
                pixelSize, pixelSize, 
                PixelFormats.Bgra32, null);

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
                            UpdateBitmaps(direction, StatePreviewType.Left, bitmap);
                            break;
                        case StateImageType.Selection:
                            foreach(StateImageSideType imageSideType in Enum.GetValues(typeof(StateImageSideType)))
                                stateBMPdict[direction][imageType].Add(imageSideType, bitmapUI.Clone());
                            break;
                    }
                }
            }

        }

        public void InitializeLandmarkData(DMIState _state)
        {
            if (_state.Width != currentState.Width || _state.Height != currentState.Height)
                throw new Exception("Overlay DMIState has different dimensions than main DMIState");

            Debug.WriteLine($"Initialize Landmark Data: {_state.Name}");
            ReplaceDMIState(_state, StatePreviewType.Right);
        }

        public void InitializeOverlayData(DMIState _state)
        {
            if(_state.Width != currentState.Width || _state.Height != currentState.Height)
                throw new Exception("Overlay DMIState has different dimensions than main DMIState");

            Debug.WriteLine($"Initialize Overlay Data: {_state.Name}");
            ReplaceDMIState(_state, StatePreviewType.Overlay);
        }

        #endregion Initializers


        #region DMI State

        public void ReplaceDMIState(DMIState _state, StatePreviewType previewType)
        {
            StateDirection[] stateDirections = StatesController.GetAllStateDirections(currentState.DirectionDepth);
            foreach (StateDirection direction in stateDirections)
            {
                WriteableBitmap bitmap = ImageEncoder.GetBMPFromDMIState(_state, direction);
                UpdateBitmaps(direction, previewType, bitmap);
            }
        }

        public void CombineDMIState(DMIState _state, StatePreviewType previewType)
        {
            StateImageType imageType = (previewType == StatePreviewType.Overlay) ? StateImageType.Overlay : StateImageType.Preview;

            StateDirection[] stateDirections = StatesController.GetAllStateDirections(currentState.DirectionDepth);
            foreach (StateDirection direction in stateDirections)
            {
                WriteableBitmap currentBitmap = stateBMPdict[direction][imageType][StateImageSideType.Left];
                WriteableBitmap bitmap = ImageEncoder.GetBMPFromDMIState(_state, direction);

                Rect destRect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                currentBitmap.Blit(destRect, bitmap, destRect);

                // todo: queue bitmap for update
                stateBMPdict[direction][imageType][StateImageSideType.Right] = currentBitmap.Clone();
                //UpdateBitmaps(direction, previewType, currentBitmap);
            }
        }

        #endregion DMI State


        #region Bitmap

        private void UpdateBitmaps(StateDirection direction, StatePreviewType previewType, WriteableBitmap bitmap)
        {
            StateImageType imageType = (previewType == StatePreviewType.Overlay) ? StateImageType.Overlay : StateImageType.Preview;
            WriteableBitmap? leftState = null;
            WriteableBitmap? rightState = null;

            switch (previewType)
            {
                case StatePreviewType.Left:
                    stateBMPdict[direction][imageType][StateImageSideType.Left] = bitmap;
                    leftState = stateBMPdict[direction][imageType][StateImageSideType.Left];
                    break;

                case StatePreviewType.Right:
                    stateBMPdict[direction][imageType][StateImageSideType.Right] = bitmap;
                    rightState = stateBMPdict[direction][imageType][StateImageSideType.Right];
                    break;

                case StatePreviewType.Overlay:
                    stateBMPdict[direction][imageType][StateImageSideType.Left] = bitmap;
                    stateBMPdict[direction][imageType][StateImageSideType.Right] = bitmap.Clone();
                    leftState = stateBMPdict[direction][imageType][StateImageSideType.Left];
                    rightState = stateBMPdict[direction][imageType][StateImageSideType.Right];
                    break;

                default:
                    throw new Exception("Invalid preview type");
            }

            // Updating the references so that we are "losing"
            if (leftState != null)
                StatesController.stateSourceDictionary[direction][imageType][StateImageSideType.Left].Source = leftState;
            if (rightState != null)
                StatesController.stateSourceDictionary[direction][imageType][StateImageSideType.Right].Source = rightState;
            if(EnvironmentController.dataPixelStorage != null)
                EnvironmentController.dataPixelStorage.DrawPixelStorageAtBitmaps();
        }

        #endregion Bitmap

        public void UpdateWidthCellsImage(int _width, int _height)
        {
            widthCellsImage = _width;
            heightCellsImage = _height;

            pixelSize = ImageEncoder.GetPixelSizeFromResolution(heightCellsImage);
        }

        public bool CheckCorrectDMIFile(DMIFile fileDmi)
        {
            if (fileDmi == null)
                return false;

            if (fileDmi.Metadata.FrameWidth != imageCellsSize.Width || fileDmi.Metadata.FrameHeight != imageCellsSize.Height)
            {
                Debug.WriteLine($"Skipped DMI File with first state {fileDmi.States.First().Name}: {fileDmi.Metadata.FrameWidth}x{fileDmi.Metadata.FrameHeight} != {imageCellsSize.Width}x{imageCellsSize.Height}");
                return false;
            }

            return true;
        }

        public void SetImageCellsSize(Size _imageCellsSize)
        {
            widthCellsImage = _imageCellsSize.Width;
            heightCellsImage = _imageCellsSize.Height;
        }

    }
}
