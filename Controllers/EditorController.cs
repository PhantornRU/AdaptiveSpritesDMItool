﻿using AdaptiveSpritesDMItool.Models;
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
            ViewSingleSelectorAtCurrentPosition(StateImageSideType.Left);
        }

        public static void EditFillModeStart()
        {
            ClearSelectors();
            ViewMultSelectorAtCurrentToMDownPosition(StateImageSideType.Left);
        }

        public static void EditFillMode()
        {
            ViewMultSelectorAtCurrentToMDownPosition(StateImageSideType.Left);
        }

        public static void EditFillModeEnd()
        {
            ClearSelectors();
        }

        public static void EditDeleteMode()
        {
            ClearSelectors();
        }

        public static void EditUndoMode()
        {
            ClearSelectors();
        }

        public static void EditUndoAreaModeStart()
        {
            ClearSelectors();
        }

        public static void EditUndoAreaMode()
        {
            //ClearSelectors();
        }

        public static void EditUndoAreaModeEnd()
        {
            ClearSelectors();
        }

        public static void EditSelectModeStart()
        {
            ClearSelectors();
            ViewSingleSelectorAtCurrentPosition(StateImageSideType.Right);
        }

        public static void EditSelectMode()
        {
            ViewMultSelectorAtCurrentToMDownPosition(StateImageSideType.Right);
        }

        public static void EditSelectModeEnd()
        {
            //ClearSelectors();
        }

        public static void EditMoveMode()
        {
            ViewSingleSelectorAtCurrentPosition(StateImageSideType.Right);
        }

        public static void EditWhenEnterImage()
        {
            ClearSelectors();
        }

        #endregion Editor Modes


        #region Editor View

        private static void ViewMultSelectorAtCurrentToMDownPosition(StateImageSideType stateImageSideType)
        {
            if (MouseController.isMouseInImage)
                SetSelectors(stateImageSideType, MouseController.currentMouseDownPosition, MouseController.currentMousePosition);
        }

        private static void ViewSingleSelectorAtCurrentPosition(StateImageSideType stateImageSideType)
        {
            if (MouseController.isMouseInImage)
                SetSelectors(stateImageSideType, MouseController.currentMousePosition);
        }

        #endregion Editor View


        #region Editor Functions


        #region Helpers

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


        private static System.Drawing.Point CorrectMousePositionPoint(StateDirection stateDirection, System.Drawing.Point mousePoint, System.Drawing.Size bitmapSize)
        {
            if (!StatesController.isMirroredState || StatesController.currentStateDirection == stateDirection)
            {
                return mousePoint;
            }
            //bitmapWidth = isStateOpposite(stateDirection) ? 0 : bitmapWidth;
            var additionValueX = StatesController.isCentralizedState ? -1 : 0;
            int result = bitmapSize.Width - mousePoint.X - 1 + additionValueX;
            result = Math.Max(result, 0);
            result = Math.Min(result, bitmapSize.Width - 1);
            System.Drawing.Point newMousePoint = new System.Drawing.Point(result, mousePoint.Y);

            Debug.WriteLine($"stateDirection: {stateDirection} - [Orig: {mousePoint} - Mod: {newMousePoint}]");


            return newMousePoint;

        }

        #endregion Helpers

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
                bitmap.DrawRectangle(0 + i, 0 + i, bitmap.PixelWidth - i - 1, bitmap.PixelHeight - i - 1, EnvironmentController.GetGridBorderColor());
            }

            return bitmap;
        }

        #endregion Grids

        #region

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
                mousePos.X = CorrectMousePositionX(stateDirectionToModify, mousePos.X, bitmapWidth);
                bitmap.SetPixel(mousePos.X, mousePos.Y, color);
                bitmapOverlay.SetPixel(mousePos.X, mousePos.Y, color);
            }
        }

        public static System.Windows.Media.Color GetSingleColorModify()
        {
            return Colors.Red;
        }

        #endregion Draw

        #region Selector
        public static void SetSelectors(StateImageSideType stateImageSideType, System.Drawing.Point mousePos1) => SetSelectors(stateImageSideType, mousePos1, mousePos1);
        public static void SetSelectors(StateImageSideType stateImageSideType, System.Drawing.Point mousePoint1, System.Drawing.Point mousePoint2)
        {
            int pixelSize = EnvironmentController.pixelSize;
            System.Windows.Media.Color color = EnvironmentController.GetGridColor();

            System.Drawing.Size bitmapSize = EnvironmentController.bitmapUISize;
            System.Drawing.Size imageSize = EnvironmentController.imageStateSize;
            var stateDirections = StatesController.GetStateDirections();
            //bitmapWidth = _stateDirection == stateDirectionToModify ? 0 : bitmapWidth;

            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmap = new WriteableBitmap(bitmapSize.Width, bitmapSize.Height, pixelSize, pixelSize, PixelFormats.Bgra32, null);
                Debug.WriteLine($"stateDirectionToModify: {stateDirectionToModify} - mousePoint1: {mousePoint1} - mousePoint2: {mousePoint2} - bitmapSize: {imageSize}");
                mousePoint1 = CorrectMousePositionPoint(stateDirectionToModify, mousePoint1, imageSize);
                mousePoint2 = CorrectMousePositionPoint(stateDirectionToModify, mousePoint2, imageSize);
                DrawSelectionRect(bitmap, mousePoint1, mousePoint2, pixelSize);
                StatesController.stateSourceDictionary[stateDirectionToModify][StateImageType.Selection][stateImageSideType].Source = bitmap;
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
                StatesController.stateSourceDictionary[stateDirectionToModify][StateImageType.Selection][StateImageSideType.Left].Source = bitmap;
                StatesController.stateSourceDictionary[stateDirectionToModify][StateImageType.Selection][StateImageSideType.Right].Source = bitmap;
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

            // Draw points for debugging
            VisualizePoints(bitmap, point1, point2, pixelSize);
        }

        private static void VisualizePoints(WriteableBitmap bitmap, System.Drawing.Point point1, System.Drawing.Point point2, int pixelSize)
        {
            byte alpha = 150;
            System.Windows.Media.Color redColor = System.Windows.Media.Colors.Red;
            redColor.A = alpha;
            System.Windows.Media.Color greenColor = System.Windows.Media.Colors.Green;
            greenColor.A = alpha;
            bitmap.FillRectangle(point1.X * pixelSize, point1.Y * pixelSize, point1.X * pixelSize + pixelSize, point1.Y * pixelSize + pixelSize, redColor);
            bitmap.FillRectangle(point2.X * pixelSize, point2.Y * pixelSize, point2.X * pixelSize + pixelSize, point2.Y * pixelSize + pixelSize, greenColor);
        }

        #endregion Selector
    }
}