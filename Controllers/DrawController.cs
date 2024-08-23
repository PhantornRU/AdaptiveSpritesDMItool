using AdaptiveSpritesDMItool.Models;
using DMISharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Point = System.Drawing.Point;
using Color = System.Windows.Media.Color;
using System.Diagnostics;
using System.IO;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class DrawController
    {
        /// <summary>
        /// Selected point for drawing on canvases.
        /// </summary>
        public static Point pickedPoint = new Point(-1, -1);


        #region Draw

        /// <summary>
        /// Set selected points using picked point and update the Data Pixel Storage.
        /// </summary>
        /// <param name="points"></param>
        public static void SetPickedPoints(Point[]? points = null)
        {
            if (pickedPoint.X < 0 || pickedPoint.Y < 0) return;

            var bitmapSize = EnvironmentController.dataImageState.imageCellsSize;
            var stateDirections = StatesController.GetStateDirections();
            Point mousePos = MouseController.GetCurrentMousePosition();
            if (points == null) points = new Point[] { mousePos };

            foreach (var stateDirection in stateDirections)
            {
                WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Left);
                WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Left);

                WriteableBitmap bitmapEditable = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Right);
                WriteableBitmap bitmapOverlayEditable = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Right);

                Color color = GetPickedPointColor(bitmapPreview);
                Color colorOverlay = GetPickedPointColor(bitmapOverlayPreview);

                foreach (Point point in points)
                {
                    var tempPoint = CorrectMousePositionPoint(stateDirection, point, bitmapSize);
                    UpdatePixel(stateDirection, bitmapEditable, tempPoint, color);
                    UpdatePixel(stateDirection, bitmapOverlayEditable, tempPoint, colorOverlay);
                }
            }

            //pickedPoint = new Point(-1, -1);
        }

        /// <summary>
        /// Clear or undo changes to selected points and update the Data Pixel Storage.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="isUndo"></param>
        public static void ClearCurrentPoints(Point[]? points = null, bool isUndo = false)
        {
            var bitmapSize = EnvironmentController.dataImageState.imageCellsSize;
            var stateDirections = StatesController.GetStateDirections();
            Point mousePos = MouseController.GetCurrentMousePosition();
            if (points == null) points = new Point[] { mousePos };

            foreach (var stateDirection in stateDirections)
            {
                WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Left);
                WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Left);

                WriteableBitmap bitmapEditable = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Right);
                WriteableBitmap bitmapOverlayEditable = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Right);

                Color color = Colors.Transparent;
                Color colorOverlay = Colors.Transparent;

                foreach (Point point in points)
                {
                    var pointToColor = point;
                    pointToColor = CorrectMousePositionPoint(stateDirection, point, bitmapSize);
                    if (isUndo)
                    {
                        color = bitmapPreview.GetPixel(pointToColor.X, pointToColor.Y);
                        colorOverlay = bitmapOverlayPreview.GetPixel(pointToColor.X, pointToColor.Y);
                    }
                    UpdatePixel(stateDirection, bitmapEditable, pointToColor, color, !isUndo);
                    UpdatePixel(stateDirection, bitmapOverlayEditable, pointToColor, colorOverlay, !isUndo);
                }
            }
        }

        /// <summary>
        /// Drawing a large number of dots across all canvases. Used in DataPixelStorage
        /// </summary>
        /// <param name="points"></param>
        public static void DrawPixelStorageAtBitmaps(IEnumerable<(StateDirection, (int x, int y), (int x, int y))> points)
        {
            foreach (var (stateDirection, point, pointForColor) in points)
            {
                // Skip it to avoid unnecessary operations
                if (point == pointForColor) continue;

                // Bitmaps from which we take pixels
                WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Left);
                WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Left);

                // Bitmaps we edit
                WriteableBitmap bitmapEditable = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Right);
                WriteableBitmap bitmapOverlayEditable = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Right);

                if (pointForColor.x < 0 || pointForColor.y < 0)
                {
                    bitmapEditable.SetPixel(point.x, point.y, Colors.Transparent);
                    bitmapOverlayEditable.SetPixel(point.x, point.y, Colors.Transparent);
                }
                else
                {
                    var pixel = bitmapPreview.GetPixel(pointForColor.x, pointForColor.y);
                    var pixelOverlay = bitmapOverlayPreview.GetPixel(pointForColor.x, pointForColor.y);
                    bitmapEditable.SetPixel(point.x, point.y, pixel);
                    bitmapOverlayEditable.SetPixel(point.x, point.y, pixelOverlay);
                }
            }
        }

        /// <summary>
        /// Change the point and update the Data Pixel Storage.
        /// </summary>
        /// <param name="stateDirection"></param>
        /// <param name="bitmapEditable"></param>
        /// <param name="point"></param>
        /// <param name="color"></param>
        private static void UpdatePixel(StateDirection stateDirection, WriteableBitmap bitmapEditable, Point point, Color color, bool isRemove = false)
        {
            bitmapEditable.SetPixel(point.X, point.Y, color);
            Point newPoint = pickedPoint;
            //if (color == Colors.Transparent)
            if (isRemove)
            {
                newPoint.X = -1;
                newPoint.Y = -1;
            }
            EnvironmentController.dataPixelStorage.ChangePoint(stateDirection, (point.X, point.Y), (newPoint.X, newPoint.Y));
        }

        /// <summary>
        /// Get color from a pixel in the selected bitmap.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Color GetPickedPointColor(WriteableBitmap bitmap) => bitmap.GetPixel(pickedPoint.X, pickedPoint.Y);


        #endregion Draw


        #region Grids
        /// <summary>
        /// Create and get a completely new grid of pixelSize and bitmapUISize parameters.
        /// </summary>
        /// <param name="borderThickness"></param>
        /// <returns></returns>
        public static WriteableBitmap GetNewGrid(int borderThickness = 2)
        {
            int pixelSize = EnvironmentController.dataImageState.pixelSize;
            Color color = EnvironmentController.GetGridColor();
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


        #region Selector
        // Dotted selection

        /// <summary>
        /// Dotted line selector for one point.
        /// </summary>
        /// <param name="stateImageSideType"></param>
        /// <param name="mousePoint"></param>
        public static void SetSelectors(StateImageSideType stateImageSideType, Point mousePoint) => SetSelectors(stateImageSideType, mousePoint, mousePoint);

        /// <summary>
        /// Selector with dotted lines.
        /// </summary>
        /// <param name="stateImageSideType"></param>
        /// <param name="mousePoint1"></param>
        /// <param name="mousePoint2"></param>
        public static void SetSelectors(StateImageSideType stateImageSideType, Point mousePoint1, Point mousePoint2)
        {
            int pixelSize = EnvironmentController.dataImageState.pixelSize;
            System.Drawing.Size imageSize = EnvironmentController.dataImageState.imageCellsSize;
            var stateDirectionsAll = StatesController.GetStateDirections();

            foreach (StateDirection stateDirection in stateDirectionsAll)
            {
                WriteableBitmap bitmap = EnvironmentController.GetSelectorBMP(stateDirection, stateImageSideType);
                bitmap.Clear();

                var mousePoint1Temp = CorrectMousePositionPoint(stateDirection, mousePoint1, imageSize);
                var mousePoint2Temp = CorrectMousePositionPoint(stateDirection, mousePoint2, imageSize);

                DrawSelectionRect(bitmap, mousePoint1Temp, mousePoint2Temp, pixelSize);
                StatesController.stateSourceDictionary[stateDirection][StateImageType.Selection][stateImageSideType].Source = bitmap;
            }
        }

        /// <summary>
        /// Clearing the selection bitmap canvas.
        /// </summary>
        /// <param name="_stateImageSideType"></param>
        public static void ClearSelectors(StateImageSideType _stateImageSideType)
        {
            var stateDirections = StatesController.allStateDirection(DirectionDepth.Four);
            foreach (StateDirection stateDirection in stateDirections)
            {
                WriteableBitmap bitmap = EnvironmentController.dataImageState.stateBMPdict[stateDirection][StateImageType.Selection][_stateImageSideType];
                bitmap.Clear();
            }
        }

        /// <summary>
        /// Clearing all bitmap canvases of the selection.
        /// </summary>
        public static void ClearSelectors()
        {
            var stateDirections = StatesController.allStateDirection(DirectionDepth.Four);

            foreach (StateDirection stateDirection in stateDirections)
            {
                foreach (StateImageSideType stateImageSideType in Enum.GetValues(typeof(StateImageSideType)))
                {
                    WriteableBitmap bitmap = EnvironmentController.dataImageState.stateBMPdict[stateDirection][StateImageType.Selection][stateImageSideType];
                    bitmap.Clear();
                }
            }
        }

        #endregion Selector


        #region Visualize

        /// <summary>
        /// Draw a dotted line around one point.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="point"></param>
        /// <param name="pixelSize"></param>
        static void DrawSelectionRect(WriteableBitmap bitmap, Point point, int pixelSize) => DrawSelectionRect(bitmap, point, point, pixelSize);

        /// <summary>
        /// Draw a dotted line around the coordinates of the points.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="pixelSize"></param>
        static void DrawSelectionRect(WriteableBitmap bitmap, Point point1, Point point2, int pixelSize)
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
                            if (minY - thickness >= 0)
                                bitmap.SetPixel(tAdd, minY - thickness, EnvironmentController.GetSelectorColor());
                            if (maxY + thickness <= bitmap.Height)
                                bitmap.SetPixel(tAdd, maxY + thickness, EnvironmentController.GetSelectorColor());
                        }
                        int tMin = i - lengthHorizontal;
                        if (tMin >= minX)
                        {
                            if (minY - thickness >= 0)
                                bitmap.SetPixel(tMin, minY - thickness, EnvironmentController.GetSelectorColor());
                            if (maxY + thickness <= bitmap.Height)
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
                            if (minX - thickness >= 0)
                                bitmap.SetPixel(minX - thickness, tAdd, EnvironmentController.GetSelectorColor());
                            if (maxX + thickness <= bitmap.Width)
                                bitmap.SetPixel(maxX + thickness, tAdd, EnvironmentController.GetSelectorColor());
                        }
                        int tMin = j - lengthVertical;
                        if (tMin >= minY)
                        {
                            if (minX - thickness >= 0)
                                bitmap.SetPixel(minX - thickness, tMin, EnvironmentController.GetSelectorColor());
                            if (maxX + thickness <= bitmap.Width)
                                bitmap.SetPixel(maxX + thickness, tMin, EnvironmentController.GetSelectorColor());
                        }
                    }
                }
            }

            // Draw points for debugging
            //VisualizeMousePoints(bitmap, point1, point2, pixelSize);
        }

        /// <summary>
        /// Display corner points to better represent the start and end of a selection.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="pixelSize"></param>
        private static void VisualizeMousePoints(WriteableBitmap bitmap, Point point1, Point point2, int pixelSize)
        {
            byte alpha = 150;
            Color redColor = Colors.Red;
            redColor.A = alpha;
            Color greenColor = Colors.Green;
            greenColor.A = alpha;
            bitmap.FillRectangle(point1.X * pixelSize, point1.Y * pixelSize, point1.X * pixelSize + pixelSize, point1.Y * pixelSize + pixelSize, redColor);
            bitmap.FillRectangle(point2.X * pixelSize, point2.Y * pixelSize, point2.X * pixelSize + pixelSize, point2.Y * pixelSize + pixelSize, greenColor);
        }

        /// <summary>
        /// Display all points for better representation of the area coloring.
        /// </summary>
        /// <param name="stateDirection"></param>
        /// <param name="bitmap"></param>
        /// <param name="point"></param>
        /// <param name="pixelSize"></param>
        public static void VisualizeSelectedPoint(StateDirection stateDirection, WriteableBitmap bitmap, Point point, int pixelSize)
        {
            // TODO: It needs to be redone.
            WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Left);
            WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Left);
            byte alpha = 100;
            Color color = bitmapOverlayPreview.GetPixel(point.X, point.Y);
            color.A = alpha;
            bitmap.FillRectangle(point.X * pixelSize, point.Y * pixelSize, point.X * pixelSize + pixelSize, point.Y * pixelSize + pixelSize, color);
        }

        #endregion Visualize



        #region Helpers

        /// <summary>
        /// The main function of correcting points for correct placement taking into account mirroring and centralization.
        /// </summary>
        /// <param name="stateDirection"></param>
        /// <param name="mousePoint"></param>
        /// <param name="bitmapSize"></param>
        /// <returns></returns>
        private static Point CorrectMousePositionPoint(StateDirection stateDirection, Point mousePoint, System.Drawing.Size bitmapSize)
        {
            bool isMirroredState = StatesController.isMirroredState;
            bool isStateOpposite = StatesController.isStateOpposite(stateDirection);
            //bool isStateVerticalOpposite = StatesController.isStateVerticalOpposite(stateDirection);
            if (!isMirroredState
                || !isStateOpposite
                //|| isStateVerticalOpposite
                )
            {
                return mousePoint;
            }
            var additionValueX = StatesController.isCentralizedState ? -1 : 0;
            int result = bitmapSize.Width - mousePoint.X - 1 + additionValueX;
            result = Math.Max(result, 0);
            result = Math.Min(result, bitmapSize.Width - 1);
            Point newMousePoint = new Point(result, mousePoint.Y);
            //Debug.WriteLine($"stateDirection: {stateDirection}: [Orig: {mousePoint} - Mod: {newMousePoint}]");
            return newMousePoint;

        }

        #endregion Helpers
    }
}
