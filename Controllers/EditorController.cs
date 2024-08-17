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
using DMISharp;
using System.Windows.Input;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class EditorController
    {
        public static StateDirection currentStateDirection;

        public static System.Drawing.Point currentMouseDownPosition;
        public static System.Drawing.Point currentMousePosition;
        public static System.Drawing.Point currentMouseUpPosition;


        #region Mouse Controller

        public static void state_MouseDown(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            UpdateCurrentStateDirection(_stateDirection);
            var stateImage = GetRightImage(_stateDirection);

            currentMouseDownPosition = MouseController.GetModifyMousePosition(e, stateImage);
            currentMousePosition = currentMouseDownPosition;

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Single:
                    EditSingleMode();
                    break;
                case StateEditType.Fill:
                    EditFillModeStart();
                    break;
                case StateEditType.Pick:
                    EditPickMode();
                    break;
                case StateEditType.Delete:
                    EditDeleteMode();
                    break;
                case StateEditType.Select:
                    EditSelectModeStart();
                    break;
                case StateEditType.Move:
                    EditMoveMode();
                    break;
            }
        }

        public static void state_MouseUp(MouseButtonEventArgs e, StateDirection _stateDirection)
        {
            UpdateCurrentStateDirection(_stateDirection);
            var stateImage = GetRightImage(_stateDirection);

            currentMouseUpPosition = MouseController.GetModifyMousePosition(e, stateImage);
            currentMousePosition = currentMouseUpPosition;

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Fill:
                    EditFillModeEnd();
                    break;
                case StateEditType.Select:
                    EditSelectModeEnd();
                    break;
            }

        }

        public static void state_MouseMove(MouseEventArgs e, StateDirection _stateDirection)
        {
            bool mouseIsDown = System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed;
            if (!mouseIsDown)
                return;
            UpdateCurrentStateDirection(_stateDirection);
            var stateImage = GetRightImage(_stateDirection);

            currentMousePosition = MouseController.GetModifyMousePosition(e, stateImage);

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Single:
                    EditSingleMode();
                    break;
                case StateEditType.Fill:
                    EditFillMode();
                    break;
                case StateEditType.Pick:
                    EditPickMode();
                    break;
                case StateEditType.Delete:
                    EditDeleteMode();
                    break;
                case StateEditType.Select:
                    EditSelectMode();
                    break;
                case StateEditType.Move:
                    EditMoveMode();
                    break;
            }
        }

        private static Image GetRightImage(StateDirection _stateDirection)
        {
            return StatesController.stateSourceDictionary[_stateDirection][StateImageType.Right];
        }

        private static void UpdateCurrentStateDirection(StateDirection _stateDirection)
        {
            currentStateDirection = _stateDirection;
        }

        #endregion Mouse Controller

        #region  User Controller

        public static void EditSingleMode()
        {
            SetPixel();
        }

        public static void EditFillModeStart()
        {

        }

        public static void EditFillMode()
        {

        }

        public static void EditFillModeEnd()
        {

        }


        public static void EditPickMode()
        {

        }

        public static void EditDeleteMode()
        {

        }

        public static void EditSelectModeStart()
        {

        }

        public static void EditSelectMode()
        {

        }
        public static void EditSelectModeEnd()
        {

        }

        public static void EditMoveMode()
        {

        }

        #endregion  User Controller

        #region Editor Modes


        #endregion Editor Modes

        #region Editor Functions

        private static void SetPixel()
        {
            System.Drawing.Point mousePos = currentMousePosition;
            Color color = GetSingleColorModify();

            var stateDirections = GetStateDirections();

            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmap = EnvironmentController.GetEnvironmentImage(stateDirectionToModify, true);
                WriteableBitmap bitmapOverlay = EnvironmentController.GetEnvironmentImageOverlay(stateDirectionToModify, true);
                int bitmapWidth = (int)bitmap.Width;
                //bitmapWidth = _stateDirection == stateDirectionToModify ? 0 : bitmapWidth;
                var mousePosXTemp = mousePos.X;
                mousePos.X = CorrectMousePositionX(stateDirectionToModify, mousePos.X, bitmapWidth);
                //Debug.WriteLine($"_stateDirection: {currentStateDirection}; stateDirectionToModify: {stateDirectionToModify}; MousePosX: [Orig: {mousePosXTemp} - Mod: {mousePos.X}]");
                bitmap.SetPixel(mousePos.X, mousePos.Y, color);
                bitmapOverlay.SetPixel(mousePos.X, mousePos.Y, color);
            }
        }

        public static Color GetSingleColorModify()
        {
            return Colors.Red;
        }

        #endregion Editor Functions

        #region States

        private static IEnumerable<StateDirection> GetStateDirections()
        {
            switch (StatesController.currentStateQuantityMode)
            {
                case StateQuantityType.Single:
                    return new[] { currentStateDirection };

                case StateQuantityType.Parallel:
                    return GetParallelStates(currentStateDirection);

                case StateQuantityType.All:
                    int parallValue = (int)currentStateDirection / 2 == 1 ? -2 : 2;
                    return GetParallelStates(currentStateDirection).Union(GetParallelStates((StateDirection)((int)currentStateDirection + parallValue)));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static StateDirection[] GetParallelStates(StateDirection _stateDirection)
        {
            int parallValue = isStateOpposite(_stateDirection) ? 1 : -1;
            StateDirection parallelState = _stateDirection + parallValue;
            return new[] { _stateDirection, parallelState };
        }

        private static int CorrectMousePositionX(StateDirection stateDirection, int mouseX, int bitmapWidth)
        {
            if (!StatesController.isMirroredState || currentStateDirection == stateDirection)
            {
                return mouseX;
            }
            //bitmapWidth = isStateOpposite(stateDirection) ? 0 : bitmapWidth;
            var additionValueX = StatesController.isCentralizedState ? -1 : 0;
            int result = bitmapWidth - mouseX - 1 + additionValueX;
            result = Math.Max(result, 0);
            result = Math.Min(result, bitmapWidth - 1);
            return result;

        }

        public static bool isStateOpposite(StateDirection _stateDirection)
        {
            return (int)_stateDirection % 2 == 0;
        }

        #endregion States

        #region Grids

        public static WriteableBitmap GetGridBackground()
        {
            int pixelResolution = EnvironmentController.heightImage;
            int pixelSize = GetPixelSizeFromResolution(pixelResolution);

            string gridBitmapPath = EnvironmentController.GetGridPath();
            WriteableBitmap gridBitmap;

            if (!File.Exists(gridBitmapPath))
            {
                gridBitmap = MakeAndGetGrid(pixelSize: pixelSize);
                EnvironmentController.SaveBitmapIntoFile(gridBitmapPath, gridBitmap);
            }
            else
            {
                BitmapImage bitmap = new BitmapImage(new Uri(gridBitmapPath, UriKind.Relative));
                gridBitmap = new WriteableBitmap(bitmap);
            }

            return gridBitmap;
        }

        private static WriteableBitmap MakeAndGetGrid(int width = 257, int height = 257, int pixelSize = 8, byte alpha = 100, int borderThickness = 2)
        {

            WriteableBitmap bitmap = new WriteableBitmap(width, height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

            Color colorTemp = Colors.Black;
            Color color = Color.FromArgb(alpha, colorTemp.R, colorTemp.G, colorTemp.B);

            for (int i = 0; i < bitmap.PixelWidth; i += pixelSize)
            {
                for (int j = 0; j < bitmap.PixelHeight; j += pixelSize)
                {
                    Debug.WriteLine($"{i}, {j} (int)bitmap.Width == {bitmap.Width}, (int)bitmap.Height == {bitmap.Height}");
                    bitmap.DrawLine(i, j, i, bitmap.PixelHeight - j, color);
                    bitmap.DrawLine(j, i, bitmap.PixelWidth - j, i, color);
                }
            }

            for (int i = 0; i < borderThickness; i++)
            {
                bitmap.DrawRectangle(0 + i, 0 + i, bitmap.PixelWidth - i - 1, bitmap.PixelHeight - i - 1, Colors.Black);
            }

            return bitmap;
        }

        private static int GetPixelSizeFromResolution(int _pixelResolution)
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

        #endregion Grids
    }
}
