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

namespace AdaptiveSpritesDMItool.Helpers
{
    internal static class EditorController
    {
        public static StateEditType currentStateEditMode = StateEditType.Single;
        public static StateQuantityType currentStateQuantityMode = StateQuantityType.Single;
        public static StateDirection currentStateDirection;

        public static System.Drawing.Point currentMouseDownPosition;
        public static System.Drawing.Point currentMousePosition;
        public static System.Drawing.Point currentMouseUpPosition;

        #region Editor Instruments

        public static void SetPixel(bool isCentralizedState, bool isMirroredState)
        {
            System.Drawing.Point mousePos = currentMousePosition;
            System.Windows.Media.Color color = EditorController.GetColorModify();

            var stateDirections = EditorController.GetStateDirections();

            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmap = EnvironmentController.GetEnvironmentImage(stateDirectionToModify, true);
                WriteableBitmap bitmapOverlay = EnvironmentController.GetEnvironmentImage(stateDirectionToModify, true);
                int bitmapWidth = (int)bitmap.Width;
                //bitmapWidth = _stateDirection == stateDirectionToModify ? 0 : bitmapWidth;
                var mousePosXTemp = mousePos.X;
                mousePos.X = EditorController.CorrectMousePositionX(stateDirectionToModify, currentStateDirection, mousePos.X, bitmapWidth, isCentralizedState, isMirroredState);
                //Debug.WriteLine($"_stateDirection: {currentStateDirection}; stateDirectionToModify: {stateDirectionToModify}; MousePosX: [Orig: {mousePosXTemp} - Mod: {mousePos.X}]");
                bitmap.SetPixel(mousePos.X, mousePos.Y, color);
                bitmapOverlay.SetPixel(mousePos.X, mousePos.Y, color);
            }
        }

        #endregion Editor Instruments

        #region States

        private static IEnumerable<StateDirection> GetStateDirections()
        {
            switch (currentStateQuantityMode)
            {
                case StateQuantityType.Single:
                    return new[] { currentStateDirection };

                case StateQuantityType.Parallel:
                    return GetParallelStates(currentStateDirection);

                case StateQuantityType.All:
                    int parallValue = ((int)currentStateDirection / 2 == 1) ? -2 : 2;
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

        private static int CorrectMousePositionX(StateDirection stateDirection, StateDirection currentStateDirection, int mouseX, int bitmapWidth, bool isCentralizedState, bool isMirroredState)
        {
            if (!isMirroredState || currentStateDirection == stateDirection)
            {
                return mouseX;
            }
            //bitmapWidth = isStateOpposite(stateDirection) ? 0 : bitmapWidth;
            var additionValueX = isCentralizedState ? -1 : 0;
            int result = bitmapWidth - mouseX - 1 + additionValueX;
            result = Math.Max(result, 0);
            result = Math.Min(result, bitmapWidth - 1);
            return result;

        }

        public static bool isStateOpposite(StateDirection _stateDirection)
        {
            return ((int)_stateDirection % 2 == 0);
        }

        public static System.Windows.Media.Color GetColorModify()
        {
            return System.Windows.Media.Colors.Red;
        }

        #endregion States


        #region Grids

        public static WriteableBitmap MakeAndGetGrid(int width = 257, int height = 257, int pixelSize = 8, byte alpha = 100, int borderThickness = 2)
        {
            
            WriteableBitmap bitmap = new WriteableBitmap(width, height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

            System.Windows.Media.Color colorTemp = System.Windows.Media.Colors.Black;
            System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(alpha, colorTemp.R, colorTemp.G, colorTemp.B);

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

        #endregion Grids
    }
}
