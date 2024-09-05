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
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Collections;
using System.Windows.Controls;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Globalization;
using System.Windows;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class DrawController
    {

        #region Properties of points

        /// <summary> Selected point for drawing on canvases. </summary>
        public static Point pickedPointFromPreview = new Point(-1, -1);

        /// <summary> Storage of selected points for moving to a new area. </summary>
        static Dictionary<StateDirection, Dictionary<Point, Point>> pointsStorage = new Dictionary<StateDirection, Dictionary<Point, Point>>();

        /// <summary> Offset from the original selected coordinate area. </summary>
        public static Point pointOffset;

        /// <summary> location of mouse click in the selected area </summary>
        public static Point pointOffsetDown;

        /// <summary> Coordinates of the smallest point of the offset boundaries </summary>
        public static Point pointOffsetMin;

        /// <summary> Coordinates of the largest point of the offset boundaries </summary>
        public static Point pointOffsetMax;

        /// <summary> The point showing what the previous offset was </summary>
        private static Point pointOffsetLast = new Point(0, 0);

        #endregion Properties of points


        #region Drawing properties

        static int borderThickness = 2;
        static byte alphaSelectedPoint = 175;


        #region Properties of the coordinate grid

        /// <summary> Left preview grid </summary>
        private static DrawingGroup dGroupTextGrid = new DrawingGroup();
        /// <summary> Right preview editable grid </summary>
        private static Dictionary<StateDirection, DrawingGroup> dGroupTextGridDictionary = new Dictionary<StateDirection, DrawingGroup>();

        private static SolidColorBrush brush = Brushes.Black;
        private static double fontSize = 1.25;
        private static Typeface typeface = new Typeface("Arial Narrow");
        private static FlowDirection flowDirection = FlowDirection.LeftToRight;
        private static CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-us");

        /// <summary> Coordinate grid canvas size </summary>
        private static int textGridSize = 127;

        private static Size pointSize;
        private static double pixelsPerDip;

        #endregion Properties of the coordinate grid

        #endregion Drawing properties


        #region Draw

        /// <summary>
        /// Set selected points using picked point and update the Data Pixel Storage.
        /// </summary>
        /// <param name="points"></param>
        public static void SetPickedPoint(Point[]? points = null)
        {
            if (pickedPointFromPreview.X < 0 || pickedPointFromPreview.Y < 0) return;

            var cellsSize = EnvironmentController.dataImageState.imageCellsSize;
            var stateDirections = StatesController.GetStateDirections();
            Point mousePos = MouseController.currentMousePosition;
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
                    var tempPoint = CorrectMousePositionPoint(stateDirection, point, cellsSize);
                    UpdatePixelPickedPoint(stateDirection, bitmapEditable, tempPoint, color);
                    UpdatePixelPickedPoint(stateDirection, bitmapOverlayEditable, tempPoint, colorOverlay);
                }
            }

            EnvironmentController.dataPixelStorage.UpdateAfterStorage();
            //pickedPoint = new Point(-1, -1);
        }


        #region Move Selected Area

        /// <summary>
        /// Updating data storage with saving new coordinates and references to values.
        /// </summary>
        public static void SetStoragePoints()
        {
            if (pointsStorage.Count == 0) return;

            var cellsSize = EnvironmentController.dataImageState.imageCellsSize;
            var stateDirections = StatesController.GetStateDirections();

            foreach (var stateDirection in stateDirections)
            {
                if (pointsStorage.ContainsKey(stateDirection) == false) continue;
                Point[] points = GetStoragePoints(stateDirection);

                WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Left);
                WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Left);

                WriteableBitmap bitmapEditable = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Right);
                WriteableBitmap bitmapOverlayEditable = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Right);

                foreach (var point in pointsStorage[stateDirection])
                {
                    //var tempPoint = CorrectMousePositionPoint(stateDirection, point.Key, cellsSize);
                    //var storagePoint = point.Value;

                    // Pixel Shift
                    Point tempPoint = CorrectMousePositionPoint(stateDirection, point.Key, cellsSize);

                    // Storage Pixel
                    Point selectedPoint = new Point(tempPoint.X + pointOffset.X, tempPoint.Y + pointOffset.Y);
                    selectedPoint = CorrectMousePositionPoint(stateDirection, selectedPoint, cellsSize);

                    Point storagePoint = pointsStorage[stateDirection][point.Key];
                    storagePoint = CorrectMousePositionPoint(stateDirection, storagePoint, cellsSize);

                    //Debug.WriteLine("======================================");
                    //Debug.WriteLine($"point: {point}, tempPoint: {tempPoint}");
                    //Debug.WriteLine($"selectedPoint: {selectedPoint}, storagePoint: {storagePoint}");
                    //Debug.WriteLine($"offset: {pointOffset}, down: {pointOffsetDown}, mouse: {MouseController.currentMousePosition}");

                    // Update Data Pixel Storage
                    UpdatePixel(stateDirection, bitmapPreview, bitmapEditable, selectedPoint, storagePoint);
                    UpdatePixel(stateDirection, bitmapOverlayPreview, bitmapOverlayEditable, selectedPoint, storagePoint);
                }
            }

            pointsStorage = new Dictionary<StateDirection, Dictionary<Point, Point>>();
            EnvironmentController.dataPixelStorage.UpdateAfterStorage();
        }

        /// <summary>
        /// Set selected points using picked point and update the Data Pixel Storage.
        /// </summary>
        /// <param name="points"></param>
        public static void UpdateStoragePoints(Point[] points)
        {
            var stateDirections = StatesController.GetStateDirections();
            var cellsSize = EnvironmentController.dataImageState.imageCellsSize;

            pointsStorage = pointsStorage ?? new Dictionary<StateDirection, Dictionary<Point, Point>>();

            foreach (var stateDirection in stateDirections)
            {
                pointsStorage[stateDirection] = new Dictionary<Point, Point>();
                foreach (var point in points)
                {
                    Point tempPoint = CorrectMousePositionPoint(stateDirection, point, cellsSize);
                    Point storagePoint = EnvironmentController.dataPixelStorage.GetPointStorage(stateDirection, point);
                    pointsStorage[stateDirection][tempPoint] = storagePoint;
                }
                //string pointStoragePoints = string.Join(", ", GetStoragePoints(stateDirection));
                //Debug.WriteLine($"StoragePoints: {stateDirection}: {pointStoragePoints}");
            }
        }

        /// <summary>
        /// Visualization of selected points with each pixel.
        /// </summary>
        /// <param name="points"></param>
        public static void ViewSelectedPoints(Point[] points)
        {
            if (points.Length > 1 && pointOffset.X == 0 && pointOffset.Y == 0) return;
            if (pointOffsetLast.X == pointOffset.X && pointOffsetLast.Y == pointOffset.Y) return;
            pointOffsetLast = pointOffset;

            var cellsSize = EnvironmentController.dataImageState.imageCellsSize;
            int pixelSize = EnvironmentController.dataImageState.pixelSize;
            var stateDirections = StatesController.GetStateDirections();

            foreach (var stateDirection in stateDirections)
            {
                WriteableBitmap bitmap = EnvironmentController.GetSelectorBMP(stateDirection, StateImageSideType.Right);
                bitmap.Clear();

                WriteableBitmap bitmapPreview = EnvironmentController.GetPreviewBMP(stateDirection, StateImageSideType.Left);
                WriteableBitmap bitmapOverlayPreview = EnvironmentController.GetOverlayBMP(stateDirection, StateImageSideType.Left);

                if (pointsStorage.ContainsKey(stateDirection) == false)
                    continue;

                foreach (Point point in points)
                {
                    // Pixel Shift
                    Point tempPoint = CorrectMousePositionPoint(stateDirection, point, cellsSize);

                    // Storage Pixel
                    Point selectedPoint = new Point(point.X - pointOffset.X, point.Y - pointOffset.Y);
                    selectedPoint = CorrectMousePositionPoint(stateDirection, selectedPoint, cellsSize);
                    Point storagePoint = pointsStorage[stateDirection][selectedPoint];
                    storagePoint = CorrectMousePositionPoint(stateDirection, storagePoint, cellsSize);

                    //Point curPos = MouseController.currentMousePosition;
                    //Debug.WriteLine($"curPos: {curPos}, point: {point}, temp: {tempPoint}, selected: {selectedPoint}, storage: {storagePoint}, --- offset: {pointOffset}");

                    // Pixel Color
                    Color color = bitmapOverlayPreview != null ? GetPointColor(bitmapOverlayPreview, storagePoint) : GetPointColor(bitmapPreview, storagePoint);
                    if (color.A == 0 || color == Colors.Transparent)
                        color = GetPointColor(bitmapPreview, storagePoint);
                    color.A = Math.Min(alphaSelectedPoint, color.A);
                    // If the point is still transparent
                    if (color.A == 0 || color == Colors.Transparent)
                    {
                        color = Colors.White;
                        color.A = 100;
                    }

                    //color = Colors.Red;
                    //color.A = 255;
                    bitmap.FillRectangle(
                        tempPoint.X * pixelSize + 1,
                        tempPoint.Y * pixelSize + 1,
                        tempPoint.X * pixelSize + pixelSize,
                        tempPoint.Y * pixelSize + pixelSize,
                        color);
                }
            }
        }

        /// <summary>
        /// Get an array of keys from pointStorage with pointOffset.
        /// </summary>
        public static Point[] GetStoragePoints(StateDirection stateDirection)
        {
            if (pointsStorage.Count == 0)
                return new Point[0];
            return pointsStorage[stateDirection].Keys.Select(point => new Point(point.X + pointOffset.X, point.Y + pointOffset.Y)).ToArray();
        }

        #endregion Move Selected Area


        /// <summary>
        /// Clear or undo changes to selected points and update the Data Pixel Storage.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="isUndo"></param>
        public static void ClearCurrentPoints(Point[]? points = null, bool isUndo = false)
        {
            var cellsSize = EnvironmentController.dataImageState.imageCellsSize;
            var stateDirections = StatesController.GetStateDirections();
            Point mousePos = MouseController.currentMousePosition;
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
                    pointToColor = CorrectMousePositionPoint(stateDirection, point, cellsSize);
                    if (isUndo)
                    {
                        color = GetPointColor(bitmapPreview, pointToColor);
                        colorOverlay = GetPointColor(bitmapOverlayPreview, pointToColor);
                    }
                    UpdatePixelPickedPoint(stateDirection, bitmapEditable, pointToColor, color, !isUndo);
                    UpdatePixelPickedPoint(stateDirection, bitmapOverlayEditable, pointToColor, colorOverlay, !isUndo);
                }
            }

            EnvironmentController.dataPixelStorage.UpdateAfterStorage();
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
                // if (point == pointForColor) continue;

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
                    Point tempPointForColor = new Point(pointForColor.x, pointForColor.y);
                    var pixel = GetPointColor(bitmapPreview, tempPointForColor);
                    var pixelOverlay = GetPointColor(bitmapOverlayPreview, tempPointForColor);
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
        private static void UpdatePixelPickedPoint(StateDirection stateDirection, WriteableBitmap bitmapEditable, Point point, Color color, bool isRemove = false)
        {
            bitmapEditable.SetPixel(point.X, point.Y, color);
            Point newPoint = pickedPointFromPreview;
            if(StatesController.currentStateEditMode == StateEditType.Undo || StatesController.currentStateEditMode == StateEditType.UndoArea)
                newPoint = point;
            //if (color == Colors.Transparent)
            if (isRemove)
            {
                newPoint.X = -1;
                newPoint.Y = -1;
            }
            EnvironmentController.dataPixelStorage.ChangePoint(stateDirection, (point.X, point.Y), (newPoint.X, newPoint.Y));
        }

        /// <summary>
        /// Change the point and update the Data Pixel Storage.
        /// </summary>
        /// <param name="stateDirection"></param>
        /// <param name="bitmapEditable"></param>
        /// <param name="pointKey"></param>
        /// <param name="pointValue"></param>
        private static void UpdatePixel(StateDirection stateDirection, WriteableBitmap bitmapPreview, WriteableBitmap bitmapEditable, Point pointKey, Point pointValue)
        {
            Color color = GetPointColor(bitmapPreview, pointValue);
            bitmapEditable.SetPixel(pointKey.X, pointKey.Y, color);
            EnvironmentController.dataPixelStorage.ChangePoint(stateDirection, (pointKey.X, pointKey.Y), (pointValue.X, pointValue.Y));
        }


        /// <summary>
        /// Get color from picked point in bitmap.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Color GetPickedPointColor(WriteableBitmap bitmap) => GetPointColor(bitmap, pickedPointFromPreview);

        /// <summary>
        /// Get color from point in bitmap.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Color GetPointColor(WriteableBitmap bitmap, Point point)
        {
            if (point.X == -1 || point.Y == -1)
                return Colors.Transparent;
            return bitmap.GetPixel(point.X, point.Y);
        }

        #endregion Draw

        #region Grids
        /// <summary>
        /// Create and get a completely new grid of pixelSize and bitmapUISize parameters.
        /// </summary>
        /// <param name="borderThickness"></param>
        /// <returns></returns>
        public static WriteableBitmap GetNewGrid()
        {
            int pixelSize = EnvironmentController.dataImageState.pixelSize;
            Color color = EnvironmentController.GetGridColor();
            System.Drawing.Size cellsSize = EnvironmentController.dataImageState.bitmapUISize;
            WriteableBitmap bitmap = new WriteableBitmap(cellsSize.Width, cellsSize.Height, pixelSize, pixelSize, PixelFormats.Bgra32, null);

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

        /// <summary>
        /// Draw a text grid with coordinates for the left previews and update the right preview according to the currently loaded data for each point.
        /// </summary>
        /// <param name="_pixelsPerDip"></param>
        public static void InitializeTextGrids(double _pixelsPerDip)
        {
            Pen shapeOutlinePen = new Pen(Brushes.Black, 2);
            shapeOutlinePen.Freeze();

            DrawingImage dImageSource = new DrawingImage(dGroupTextGrid);
            var stateDirections = StatesController.GetAllStateDirections();
            foreach (StateDirection stateDirection in stateDirections)
            {
                StatesController.stateSourceDictionary[stateDirection][StateImageType.TextGrid][StateImageSideType.Left].Source = dImageSource;

                dGroupTextGridDictionary[stateDirection] = new DrawingGroup();
                DrawingImage dImageSourceEditable = new DrawingImage(dGroupTextGridDictionary[stateDirection]);
                StatesController.stateSourceDictionary[stateDirection][StateImageType.TextGrid][StateImageSideType.Right].Source = dImageSourceEditable;
            }

            var cellsSize = EnvironmentController.dataImageState.imageCellsSize;
            pointSize = new Size(
                textGridSize / cellsSize.Width,
                textGridSize / cellsSize.Height);
            pixelsPerDip = _pixelsPerDip;

            // Obtain a DrawingContext from the DrawingGroup.
            using (DrawingContext dc = dGroupTextGrid.Open())
            {
                for (int i = 0; i < cellsSize.Width; i++)
                {
                    for (int j = 0; j < cellsSize.Height; j++)
                    {
                        System.Windows.Point point = new System.Windows.Point(i * pointSize.Width, j * pointSize.Height);
                        string text = $"{i}:{j}";

                        FormattedText formattedText = new FormattedText(
                            text,
                            cultureInfo,
                        flowDirection,
                            typeface,
                            fontSize,
                            brush,
                            pixelsPerDip);

                        dc.DrawText(formattedText, point);
                    }
                }

            }

            var allStateDirections = StatesController.GetAllStateDirections();
            RenderTextGrids(allStateDirections);

        }

        /// <summary>
        /// Update the right preview of the coordinate grid text according to the currently loaded data for each point.
        /// </summary>
        public static void RenderTextGrids(IEnumerable<StateDirection>? stateDirections = null)
        {
            if(stateDirections == null)
                stateDirections = StatesController.GetStateDirections();
            var cellsSize = EnvironmentController.dataImageState.imageCellsSize;

            foreach (StateDirection stateDirection in stateDirections)
            {
                // Obtain a DrawingContext from 
                // the DrawingGroup.
                DrawingGroup dGroup = dGroupTextGridDictionary[stateDirection];
                using (DrawingContext dc = dGroup.Open())
                {
                    for (int i = 0; i < cellsSize.Width; i++)
                    {
                        for (int j = 0; j < cellsSize.Height; j++)
                        {
                            System.Windows.Point pointPos = new System.Windows.Point(i * pointSize.Width, j * pointSize.Height);
                            Point point = new Point(i, j);
                            Point storagePoint = EnvironmentController.dataPixelStorage.GetPointStorage(stateDirection, point);

                            string text = $"{storagePoint.X}:{storagePoint.Y}";

                            FormattedText formattedText = new FormattedText(
                                text,
                                cultureInfo,
                            flowDirection,
                                typeface,
                                fontSize,
                                brush,
                                pixelsPerDip);

                            dc.DrawText(formattedText, pointPos);
                        }
                    }
                }
            }

        }

        #endregion Grids


        #region Selector
        // Dotted selection

        /// <summary>
        /// Dotted line selector for one point.
        /// </summary>
        /// <param name="stateImageSideType"></param>
        /// <param name="mousePoint"></param>
        public static void ViewSelectors(StateImageSideType stateImageSideType, Point mousePoint) => ViewSelectors(stateImageSideType, mousePoint, mousePoint);

        /// <summary>
        /// Selector with dotted lines.
        /// </summary>
        /// <param name="stateImageSideType"></param>
        /// <param name="mousePoint1"></param>
        /// <param name="mousePoint2"></param>
        public static void ViewSelectors(StateImageSideType stateImageSideType, Point mousePoint1, Point mousePoint2)
        {
            int pixelSize = EnvironmentController.dataImageState.pixelSize;
            System.Drawing.Size imageSize = EnvironmentController.dataImageState.imageCellsSize;
            var stateDirections = StatesController.GetStateDirections();

            foreach (StateDirection stateDirection in stateDirections)
            {
                WriteableBitmap bitmap = EnvironmentController.GetSelectorBMP(stateDirection, stateImageSideType);

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
            var stateDirections = StatesController.GetAllStateDirections();
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
            var stateDirections = StatesController.GetAllStateDirections();

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
            Color redColor = Colors.Red;
            redColor.A = alphaSelectedPoint;
            Color greenColor = Colors.Green;
            greenColor.A = alphaSelectedPoint;
            bitmap.FillRectangle(point1.X * pixelSize, point1.Y * pixelSize, point1.X * pixelSize + pixelSize, point1.Y * pixelSize + pixelSize, redColor);
            bitmap.FillRectangle(point2.X * pixelSize, point2.Y * pixelSize, point2.X * pixelSize + pixelSize, point2.Y * pixelSize + pixelSize, greenColor);
        }

        #endregion Visualize



        #region Helpers

        /// <summary>
        /// The main function of correcting points for correct placement taking into account mirroring and centralization.
        /// </summary>
        /// <param name="stateDirection"></param>
        /// <param name="mousePoint"></param>
        /// <param name="cellsSize"></param>
        /// <returns></returns>
        private static Point CorrectMousePositionPoint(StateDirection stateDirection, Point mousePoint, System.Drawing.Size cellsSize)
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
            int result = cellsSize.Width - mousePoint.X - 1 + additionValueX;
            result = Math.Max(result, 0);
            result = Math.Min(result, cellsSize.Width - 1);
            Point newMousePoint = new Point(result, mousePoint.Y);
            //Debug.WriteLine($"stateDirection: {stateDirection}: [Orig: {mousePoint} - Mod: {newMousePoint}]");
            return newMousePoint;

        }



        #region Offsets

        /// <summary>
        /// Update when the smallest and largest points within the selected area are found.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        public static void UpdatePointOffsetBounds(Point point1, Point point2)
        {
            pointOffsetMin.X = Math.Min(point1.X, point2.X);
            pointOffsetMin.Y = Math.Min(point1.Y, point2.Y);

            pointOffsetMax.X = Math.Max(point1.X, point2.X);
            pointOffsetMax.Y = Math.Max(point1.Y, point2.Y);

            Debug.WriteLine($"Update Point Offset Bounds, min: {pointOffsetMin}, max: {pointOffsetMax}. pointOffset: {pointOffset}, Down: {pointOffsetDown}, Last: {pointOffsetLast}");
        }

        /// <summary>
        /// Finding the current offset and preventing it from going beyond the boundaries.
        /// </summary>
        public static void UpdatePointOffset()
        {
            var cellsSize = EnvironmentController.dataImageState.imageCellsSize;
            Point mousePos = MouseController.currentMousePosition;

            pointOffset.X = mousePos.X - pointOffsetDown.X;
            pointOffset.Y = mousePos.Y - pointOffsetDown.Y;

            int XBeyondMin = pointOffset.X + pointOffsetMin.X;
            int YBeyondMin = pointOffset.Y + pointOffsetMin.Y;
            // Прибавляем остаток, тем самым выравнивая точку по и оффсет по краям и границам, чтобы не перейти их.
            if (XBeyondMin < 0)
                pointOffset.X -= XBeyondMin;
            if (YBeyondMin < 0)
                pointOffset.Y -= YBeyondMin;

            //Debug.WriteLine($"pointOffset: {pointOffset}, XbeyondMin: {XBeyondMin}, YBeyondMin: {YBeyondMin}");

            int xMax = cellsSize.Width - 1;
            int yMax = cellsSize.Height - 1;
            // расстояние от max до границы
            int Xdist = xMax - pointOffsetMax.X;
            int Ydist = yMax - pointOffsetMax.Y;
            // максимум куда (коорд) можно сместить выбранный
            int XdistMax = pointOffsetDown.X + Xdist;
            int YdistMax = pointOffsetDown.Y + Ydist;
            // допустимо смещать ВЫБРАННЫЙ на расстояние MAX до данной границы
            if (mousePos.X > XdistMax)
                pointOffset.X = Xdist;
            if (mousePos.Y > YdistMax)
                pointOffset.Y = Ydist;

            //Debug.WriteLine($"Update Point Offset, where Down {pointOffsetDown}, offset: {pointOffset}, min: {pointOffsetMin}, max: {pointOffsetMax}, mousePos: {mousePos}");
        }

        /// <summary>
        /// Determining the location of the offsetDown when the offset itself and the mouse are shifted.
        /// </summary>
        public static void UpdatePointOffsetDown()
        {
            pointOffsetDown = MouseController.currentMousePosition;
            pointOffsetDown.X -= pointOffset.X;
            pointOffsetDown.Y -= pointOffset.Y;
        }

        /// <summary>
        /// Reset all offsets.
        /// </summary>
        public static void ResetOffset()
        {
            pointOffsetMin.X += pointOffset.X;
            pointOffsetMin.Y += pointOffset.Y;

            pointOffsetMax.X += pointOffset.X;
            pointOffsetMax.Y += pointOffset.Y;

            pointOffset = new Point(0, 0);
            pointOffsetDown = new Point(0, 0);
            pointOffsetLast = new Point(0, 0);
        }

        #endregion Offsets


        #endregion Helpers
    }
}
