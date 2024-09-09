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

            Dictionary<StateImageSideType, WriteableBitmap> backgroundDict = new Dictionary<StateImageSideType, WriteableBitmap>();

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
        }

        public void InitializeOverlayData(DMIState _state)
        {
            if(_state.Width != currentState.Width || _state.Height != currentState.Height)
                throw new Exception("Overlay DMIState has different dimensions than main DMIState");

            StateDirection[] stateDirections = StatesController.GetAllStateDirections(currentState.DirectionDepth);
            foreach (StateDirection direction in stateDirections)
            {
                WriteableBitmap bitmap = ImageEncoder.GetBMPFromDMIState(_state, direction);
                stateBMPdict[direction][StateImageType.Overlay].Add(StateImageSideType.Left, bitmap);
                stateBMPdict[direction][StateImageType.Overlay].Add(StateImageSideType.Right, bitmap.Clone());
            }
        }

        public void InitializeOverlayData()
        {
            StateDirection[] stateDirections = StatesController.GetAllStateDirections(currentState.DirectionDepth);
            foreach (StateDirection direction in stateDirections)
            {
                WriteableBitmap bitmap = stateBMPdict[direction][StateImageType.Preview][StateImageSideType.Left].Clone();
                stateBMPdict[direction][StateImageType.Overlay].Add(StateImageSideType.Left, bitmap);
                stateBMPdict[direction][StateImageType.Overlay].Add(StateImageSideType.Right, bitmap.Clone());
            }
        }


        #endregion Initializers

    }
}
