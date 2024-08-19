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
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using System.Windows.Media.Media3D;
using System.Windows;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class EditorController
    {
        #region  User Controller

        #endregion  User Controller

        #region Editor Modes

        public static void EditSingleMode()
        {
            //SetPixel();
            ViewSingleSelectorAtCurrentPosition();
        }

        public static void EditFillModeStart()
        {
            ViewMultSelectorAtCurrentToMDownPosition();
        }

        public static void EditFillMode()
        {
            ViewMultSelectorAtCurrentToMDownPosition();
        }

        public static void EditFillModeEnd()
        {
            ClearSelectors();
        }


        public static void EditPickMode()
        {
            ViewSingleSelectorAtCurrentPosition();
        }

        public static void EditDeleteMode()
        {
            ClearSelectors();
        }

        public static void EditSelectModeStart()
        {
            ViewSingleSelectorAtCurrentPosition();
        }

        public static void EditSelectMode()
        {
            ViewMultSelectorAtCurrentToMDownPosition();
        }

        public static void EditSelectModeEnd()
        {
            //ClearSelectors();
        }

        public static void EditMoveMode()
        {
            ViewSingleSelectorAtCurrentPosition();
        }

        #endregion Editor Modes


        #region Editor View

        private static void ViewMultSelectorAtCurrentToMDownPosition()
        {
            if (MouseController.isMouseInImage)
                SetSelectors(MouseController.currentMouseDownPosition, MouseController.currentMousePosition);
        }

        private static void ViewSingleSelectorAtCurrentPosition()
        {
            if (MouseController.isMouseInImage)
                SetSelectors(MouseController.currentMousePosition);
        }

        #endregion Editor View


        #region Editor Functions

        private static void SetPixel()
        {
            System.Drawing.Point mousePos = MouseController.GetCurrentMousePosition();
            System.Windows.Media.Color color = GetSingleColorModify();

            var stateDirections = StatesController.GetStateDirections();

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

        public static System.Windows.Media.Color GetSingleColorModify()
        {
            return Colors.Red;
        }

        private static int CorrectMousePositionX(StateDirection stateDirection, int mouseX, int bitmapWidth)
        {
            if (!StatesController.isMirroredState || StatesController.currentStateDirection == stateDirection)
            {
                return mouseX;
            }
            //bitmapWidth = isStateOpposite(stateDirection) ? 0 : bitmapWidth;
            var additionValueX = StatesController.isCentralizedState ? -1 : 0;
            int result = bitmapWidth - mouseX - 1 + additionValueX;
            result = Math.Max(result, 0);
            result = Math.Min(result, bitmapWidth - 1);
            Debug.WriteLine($"[Orig: {mouseX} - Mod: {result}]");
            return result;

        }

        #endregion Editor Functions


        #region Grids

        public static WriteableBitmap GetGridBackground()
        {
            string gridBitmapPath = EnvironmentController.GetGridPath();
            WriteableBitmap bitmap;

            if (!File.Exists(gridBitmapPath))
            {
                bitmap = MakeAndGetGrid();
                EnvironmentController.SaveBitmapIntoFile(gridBitmapPath, bitmap);
            }
            else
            {
                BitmapImage bitmapTemp = new BitmapImage(new Uri(gridBitmapPath, UriKind.Relative));
                bitmap = new WriteableBitmap(bitmapTemp);
            }

            return bitmap;
        }

        private static WriteableBitmap MakeAndGetGrid(int borderThickness = 2)
        {
            int pixelSize = EnvironmentController.pixelSize;
            System.Windows.Media.Color color = EnvironmentController.GetGridColor();
            int width = EnvironmentController.widthBitmapUI;
            int height = EnvironmentController.heightBitmapUI;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

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


        #region Selector
        public static void SetSelectors(System.Drawing.Point mousePos1) => SetSelectors(mousePos1, mousePos1);
        public static void SetSelectors(System.Drawing.Point mousePos1, System.Drawing.Point mousePos2)
        {
            int pixelSize = EnvironmentController.pixelSize;
            System.Windows.Media.Color color = EnvironmentController.GetGridColor();
            int width = EnvironmentController.widthBitmapUI;
            int height = EnvironmentController.heightBitmapUI;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

            int bitmapWidth = (int)bitmap.Width;
            var stateDirections = StatesController.GetStateDirections();

            foreach (var stateDirectionToModify in stateDirections)
            {
                mousePos1.X = CorrectMousePositionX(stateDirectionToModify, mousePos1.X, bitmapWidth);
                mousePos2.X = CorrectMousePositionX(stateDirectionToModify, mousePos2.X, bitmapWidth);
                DrawSelectionRect(bitmap, mousePos1, mousePos2, pixelSize);
                StatesController.stateSourceDictionary[stateDirectionToModify][StateImageType.SelectionLeft].Source = bitmap;
                StatesController.stateSourceDictionary[stateDirectionToModify][StateImageType.SelectionRight].Source = bitmap;

                Debug.WriteLine($"stateDirectionToModify: {stateDirectionToModify}; mousePos1: {mousePos1}; mousePos2: {mousePos2}");
            }
        }
        public static void ClearSelectors()
        {
            int pixelSize = EnvironmentController.pixelSize;
            System.Windows.Media.Color color = EnvironmentController.GetGridColor();
            int width = EnvironmentController.widthBitmapUI;
            int height = EnvironmentController.heightBitmapUI;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, pixelSize, pixelSize, PixelFormats.Bgra32, null);
            var stateDirections = StatesController.GetAllStates();
            foreach (var stateDirectionToModify in stateDirections)
            {
                StatesController.stateSourceDictionary[stateDirectionToModify][StateImageType.SelectionLeft].Source = bitmap;
                StatesController.stateSourceDictionary[stateDirectionToModify][StateImageType.SelectionRight].Source = bitmap;
            }
        }

        static void DrawSelectionRect(WriteableBitmap bitmap, System.Drawing.Point point, int pixelSize) => DrawSelectionRect(bitmap, point, point, pixelSize);
        static void DrawSelectionRect(WriteableBitmap bitmap, System.Drawing.Point point1, System.Drawing.Point point2, int pixelSize)
        {
            int lineLength = 3;
            int lineThickness = 2;

            int minX = Math.Min(point1.X, point2.X) * pixelSize;
            int minY = Math.Min(point1.Y, point2.Y) * pixelSize;
            int maxX = Math.Max(point1.X + 1, point2.X + 1) * pixelSize;
            int maxY = Math.Max(point1.Y + 1, point2.Y + 1) * pixelSize;

            for (int thickness = 0; thickness < lineThickness; thickness++)
            {
                for (int i = minX; i <= maxX; i += pixelSize)
                {
                    for (int lengthHorizontal = 0; lengthHorizontal < lineLength; lengthHorizontal++)
                    {
                        int tAdd = i + lengthHorizontal;
                        if (tAdd <= maxX)
                        {
                            if(minY - thickness >= 0)
                                bitmap.SetPixel(tAdd, minY - thickness, EnvironmentController.GetSelectorColor());
                            if(maxY + thickness <= bitmap.Height)
                                bitmap.SetPixel(tAdd, maxY + thickness, EnvironmentController.GetSelectorColor());
                        }
                        int tMin = i - lengthHorizontal;
                        if (tMin >= minX)
                        {
                            if(minY - thickness >= 0)
                                bitmap.SetPixel(tMin, minY - thickness, EnvironmentController.GetSelectorColor());
                            if(maxY + thickness <= bitmap.Height)
                                bitmap.SetPixel(tMin, maxY + thickness, EnvironmentController.GetSelectorColor());
                        }
                    }
                }

                for (int j = minY; j <= maxY; j += pixelSize)
                {
                    for (int lengthVertical = 0; lengthVertical < lineLength; lengthVertical++)
                    {
                        int tAdd = j + lengthVertical;
                        if (tAdd <= maxY)
                        {
                            if(minX - thickness >= 0)
                                bitmap.SetPixel(minX - thickness, tAdd, EnvironmentController.GetSelectorColor());
                            if(maxX + thickness <= bitmap.Width)
                                bitmap.SetPixel(maxX + thickness, tAdd, EnvironmentController.GetSelectorColor());
                        }
                        int tMin = j - lengthVertical;
                        if (tMin >= minY)
                        {
                            if(minX - thickness >= 0)
                                bitmap.SetPixel(minX - thickness, tMin, EnvironmentController.GetSelectorColor());
                            if(maxX + thickness <= bitmap.Width)
                                bitmap.SetPixel(maxX + thickness, tMin, EnvironmentController.GetSelectorColor());
                        }
                    }
                }
            }

        }

        #endregion Selector
    }
}
