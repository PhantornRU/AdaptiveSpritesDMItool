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
using DMISharp;
using System.Windows.Input;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using System.Drawing;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class EditorController
    {
        private static System.Drawing.Point pickedPoint = new System.Drawing.Point(-1, -1);

        #region Editor Modes

        public static void EditSingleMode(StateImageSideType _stateImageSideType)
        {
            //SetPixel();
            if(!MouseController.isMouseInImage)
                return;

            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    ViewSingleSelectorAtCurrentPosition(_stateImageSideType);
                    PickPointAtCurrentPosition();
                    break;
                case StateImageSideType.Right:
                    SetPickedPoints();
                    break;
            }
        }

        public static void EditFillModeStart(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    ViewSingleSelectorAtCurrentPosition(_stateImageSideType);
                    PickPointAtCurrentPosition();
                    break;
                case StateImageSideType.Right:
                    ViewMultSelectorAtCurrentToMDownPosition(_stateImageSideType);
                    break;
            }
        }

        public static void EditFillMode(StateImageSideType _stateImageSideType)
        {
            if (!MouseController.isMouseInImage)
                return;

            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    ViewSingleSelectorAtCurrentPosition(_stateImageSideType);
                    break;
                case StateImageSideType.Right:
                    ViewMultSelectorAtCurrentToMDownPosition(_stateImageSideType);
                    break;
            }

        }

        public static void EditFillModeEnd(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    if (MouseController.currentMouseDownPosition != MouseController.currentMousePosition)
                    {
                        var points = GetPointsFromMouseDownToUp();
                        SetPickedPoints(points);
                    }
                    break;
            }
        }

        public static void EditDeleteMode(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    ClearSelectors();
                    ClearCurrentPoints();
                    break;
            }
        }

        public static void EditUndoMode(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    ClearSelectors();
                    ClearCurrentPoints(isUndo: true);
                    break;
            }
        }

        public static void EditUndoAreaModeStart(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    ClearSelectors();
                    ViewSingleSelectorAtCurrentPosition(_stateImageSideType);
                    ClearCurrentPoints(isUndo: true);
                    break;
            }
        }

        public static void EditUndoAreaMode(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    ViewMultSelectorAtCurrentToMDownPosition(_stateImageSideType);
                    break;
            }
        }

        public static void EditUndoAreaModeEnd(StateImageSideType _stateImageSideType)
        {
            ClearSelectors();
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    var points = GetPointsFromMouseDownToUp();
                    ClearCurrentPoints(points, true);
                    break;
            }
        }

        public static void EditSelectModeStart(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    ClearSelectors();
                    ViewSingleSelectorAtCurrentPosition(_stateImageSideType);
                    break;
            }
        }

        public static void EditSelectMode(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    ViewMultSelectorAtCurrentToMDownPosition(_stateImageSideType);
                    break;
            }
        }

        public static void EditSelectModeEnd(StateImageSideType _stateImageSideType)
        {
            //ClearSelectors();
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    break;
            }
        }

        public static void EditMoveMode(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    ViewSingleSelectorAtCurrentPosition(_stateImageSideType);
                    break;
            }
        }

        public static void EditWhenEnterImage(StateImageSideType _stateImageSideType)
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

        private static void PickPointAtCurrentPosition()
        {
            if (MouseController.isMouseInImage)
                pickedPoint = MouseController.currentMousePosition;
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
            int pixelSize = EnvironmentController.dataImageState.pixelSize;
            System.Windows.Media.Color color = EnvironmentController.GetGridColor();
            System.Drawing.Size bitmapSize = EnvironmentController.dataImageState.bitmapUISize;
            WriteableBitmap bitmap = new WriteableBitmap(bitmapSize.Width, bitmapSize.Height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

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

        #region Draw

        private static void SetPickedPoints(System.Drawing.Point[]? points = null)
        {
            if (pickedPoint.X <= 0) return;

            int bitmapWidth = EnvironmentController.dataImageState.imageCellsSize.Width;
            var stateDirections = StatesController.GetStateDirections();
            System.Drawing.Point mousePos = MouseController.GetCurrentMousePosition();
            if (points == null) points = new System.Drawing.Point[] { mousePos };

            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirectionToModify, StateImageSideType.Left);
                WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirectionToModify, StateImageSideType.Left);

                WriteableBitmap bitmapEditable = EnvironmentController.GetPreviewBMP(stateDirectionToModify, StateImageSideType.Right);
                WriteableBitmap bitmapOverlayEditable = EnvironmentController.GetOverlayBMP(stateDirectionToModify, StateImageSideType.Right);

                System.Windows.Media.Color color = GetPickedPointColor(bitmapPreview);
                System.Windows.Media.Color colorOverlay = GetPickedPointColor(bitmapOverlayPreview);

                foreach (System.Drawing.Point point in points)
                {
                    var tempPoint = point;
                    tempPoint.X = CorrectMousePositionX(stateDirectionToModify, point.X, bitmapWidth);
                    bitmapEditable.SetPixel(tempPoint.X, tempPoint.Y, color);
                    bitmapOverlayEditable.SetPixel(tempPoint.X, tempPoint.Y, colorOverlay);
                }
            }

            //pickedPoint = new System.Drawing.Point(-1, -1);
        }

        private static void ClearCurrentPoints(System.Drawing.Point[]? points = null, bool isUndo = false)
        {
            int bitmapWidth = EnvironmentController.dataImageState.imageCellsSize.Width;
            var stateDirections = StatesController.GetStateDirections();
            System.Drawing.Point mousePos = MouseController.GetCurrentMousePosition();
            if (points == null) points = new System.Drawing.Point[] { mousePos };

            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirectionToModify, StateImageSideType.Left);
                WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirectionToModify, StateImageSideType.Left);

                WriteableBitmap bitmapEditable = EnvironmentController.GetPreviewBMP(stateDirectionToModify, StateImageSideType.Right);
                WriteableBitmap bitmapOverlayEditable = EnvironmentController.GetOverlayBMP(stateDirectionToModify, StateImageSideType.Right);

                System.Windows.Media.Color color = Colors.Transparent;
                System.Windows.Media.Color colorOverlay = Colors.Transparent;

                foreach (System.Drawing.Point point in points)
                {
                    var tempPoint = point;
                    if (isUndo)
                    {
                        color = bitmapPreview.GetPixel(tempPoint.X, tempPoint.Y);
                        colorOverlay = bitmapOverlayPreview.GetPixel(tempPoint.X, tempPoint.Y);
                    }
                    tempPoint.X = CorrectMousePositionX(stateDirectionToModify, point.X, bitmapWidth);
                    bitmapEditable.SetPixel(tempPoint.X, tempPoint.Y, color);
                    bitmapOverlayEditable.SetPixel(tempPoint.X, tempPoint.Y, colorOverlay);
                }
            }
        }

        private static (WriteableBitmap, WriteableBitmap) GetBitmaps(StateDirection stateDirection) =>
            (EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Right),
             EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Right));

        private static (System.Windows.Media.Color, System.Windows.Media.Color) GetColors(WriteableBitmap previewBitmap, WriteableBitmap overlayBitmap) =>
            (GetPickedPointColor(previewBitmap), GetPickedPointColor(overlayBitmap));

        public static System.Windows.Media.Color GetPickedPointColor(WriteableBitmap bitmap) => bitmap.GetPixel(pickedPoint.X, pickedPoint.Y);

        private static System.Drawing.Point[] GetPointsFromMouseDownToUp()
        {
            int x1 = Math.Min(MouseController.currentMouseDownPosition.X, MouseController.currentMousePosition.X);
            int x2 = Math.Max(MouseController.currentMouseDownPosition.X, MouseController.currentMousePosition.X);
            int y1 = Math.Min(MouseController.currentMouseDownPosition.Y, MouseController.currentMousePosition.Y);
            int y2 = Math.Max(MouseController.currentMouseDownPosition.Y, MouseController.currentMousePosition.Y);

            var points = new System.Drawing.Point[(x2 - x1 + 1) * (y2 - y1 + 1)];
            int pointIndex = 0;
            for (int y = y1; y <= y2; y++)
            {
                for (int x = x1; x <= x2; x++)
                {
                    points[pointIndex].X = x;
                    points[pointIndex].Y = y;
                    pointIndex++;
                }
            }
            return points;
        }

        #endregion Draw

        #region Selector
        public static void SetSelectors(StateImageSideType stateImageSideType, System.Drawing.Point mousePos1) => SetSelectors(stateImageSideType, mousePos1, mousePos1);
        public static void SetSelectors(StateImageSideType stateImageSideType, System.Drawing.Point mousePoint1, System.Drawing.Point mousePoint2)
        {
            int pixelSize = EnvironmentController.dataImageState.pixelSize;
            System.Drawing.Size imageSize = EnvironmentController.dataImageState.imageCellsSize;
            var stateDirections = StatesController.GetStateDirections();
            //bitmapWidth = _stateDirection == stateDirectionToModify ? 0 : bitmapWidth;

            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmap = EnvironmentController.GetSelectorBMP(stateDirectionToModify, stateImageSideType);
                bitmap.Clear();
                Debug.WriteLine($"stateDirectionToModify: {stateDirectionToModify} - mousePoint1: {mousePoint1} - mousePoint2: {mousePoint2} - bitmapSize: {imageSize}");
                mousePoint1 = CorrectMousePositionPoint(stateDirectionToModify, mousePoint1, imageSize);
                mousePoint2 = CorrectMousePositionPoint(stateDirectionToModify, mousePoint2, imageSize);
                DrawSelectionRect(bitmap, mousePoint1, mousePoint2, pixelSize);
                StatesController.stateSourceDictionary[stateDirectionToModify][StateImageType.Selection][stateImageSideType].Source = bitmap;
            }
        }

        public static void ClearSelectors(StateImageSideType _stateImageSideType)
        {
            var stateDirections = StatesController.allStateDirection(DirectionDepth.Four);
            foreach (var stateDirectionToModify in stateDirections)
            {
                WriteableBitmap bitmap = EnvironmentController.dataImageState.stateBMPdict[stateDirectionToModify][StateImageType.Selection][_stateImageSideType];
                bitmap.Clear();
            }
        }

        public static void ClearSelectors()
        {
            var stateDirections = StatesController.allStateDirection(DirectionDepth.Four);

            foreach (var stateDirectionToModify in stateDirections)
            {
                foreach (StateImageSideType stateImageSideType in Enum.GetValues(typeof(StateImageSideType)))
                {
                    WriteableBitmap bitmap = EnvironmentController.dataImageState.stateBMPdict[stateDirectionToModify][StateImageType.Selection][stateImageSideType];
                    bitmap.Clear();
                }
            }
        }

        #region Visualize

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
            //VisualizeMousePoints(bitmap, point1, point2, pixelSize);
        }

        private static void VisualizeMousePoints(WriteableBitmap bitmap, System.Drawing.Point point1, System.Drawing.Point point2, int pixelSize)
        {
            byte alpha = 150;
            System.Windows.Media.Color redColor = System.Windows.Media.Colors.Red;
            redColor.A = alpha;
            System.Windows.Media.Color greenColor = System.Windows.Media.Colors.Green;
            greenColor.A = alpha;
            bitmap.FillRectangle(point1.X * pixelSize, point1.Y * pixelSize, point1.X * pixelSize + pixelSize, point1.Y * pixelSize + pixelSize, redColor);
            bitmap.FillRectangle(point2.X * pixelSize, point2.Y * pixelSize, point2.X * pixelSize + pixelSize, point2.Y * pixelSize + pixelSize, greenColor);
        }

        public static void VisualizeSelectedPoint(StateDirection stateDirection, WriteableBitmap bitmap, System.Drawing.Point point, int pixelSize)
        {
            WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Left);
            WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Left);
            byte alpha = 100;
            System.Windows.Media.Color color = bitmapOverlayPreview.GetPixel(point.X, point.Y);
            color.A = alpha;
            bitmap.FillRectangle(point.X * pixelSize, point.Y * pixelSize, point.X * pixelSize + pixelSize, point.Y * pixelSize + pixelSize, color);
        }

        #endregion Visualize

        #endregion Selector
    }
}
