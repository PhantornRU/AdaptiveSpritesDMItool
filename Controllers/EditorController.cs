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
using System.Collections.Concurrent;
using Point = System.Drawing.Point;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class EditorController
    {
        static SelectMode selectMode = SelectMode.None;

        #region Editor Modes

        #region Draw

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
                    DrawController.SetPickedPoint();
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
                        DrawController.SetPickedPoint(points);
                    }
                    break;
            }
        }

        #endregion Draw


        #region Clear

        public static void EditDeleteMode(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    DrawController.ClearSelectors();
                    DrawController.ClearCurrentPoints();
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
                    DrawController.ClearSelectors();
                    DrawController.ClearCurrentPoints(isUndo: true);
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
                    ViewSingleSelectorAtCurrentPosition(_stateImageSideType);
                    DrawController.ClearCurrentPoints(isUndo: true);
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
            DrawController.ClearSelectors();
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    var points = GetPointsFromMouseDownToUp();
                    DrawController.ClearCurrentPoints(points, true);
                    break;
            }
        }

        #endregion Clear


        #region Select

        public static void EditMoveModeStart(StateImageSideType _stateImageSideType)
        {
            if (_stateImageSideType != StateImageSideType.Right) return;

            selectMode = SelectMode.Move;
            Point currPos = MouseController.currentMousePosition;
            DrawController.pointOffsetDown = currPos;
            DrawController.pointOffsetMin = currPos;
            DrawController.pointOffsetMax = currPos;
            DrawController.pointOffset = new Point(0, 0);

            Point[] selectedPoints = new Point[] { MouseController.currentMousePosition };
            DrawController.UpdateStoragePoints(selectedPoints);
            ViewSelectedPoints(_stateImageSideType);
        }

        public static void EditMoveMode(StateImageSideType _stateImageSideType)
        {
            if (_stateImageSideType != StateImageSideType.Right) return;

            DrawController.UpdatePointOffset();
            ViewSelectedPoints(_stateImageSideType);
        }

        public static void EditMoveModeEnd(StateImageSideType _stateImageSideType)
        {
            if (_stateImageSideType != StateImageSideType.Right) return;

            DrawController.SetStoragePoints();
            DrawController.ClearSelectors();
        }

        public static void EditSelectModeStart(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:

                    // If the mouse is pressed within the ALREADY selected area - then:
                    // Set Move - the movement mode
                    var direction = StatesController.currentStateDirection;
                    var pointsDirection = DrawController.GetStoragePoints(direction);
                    if(direction != StatesController.selectedStateDirection && pointsDirection.Length == 0) break;
                    //string pointsString = string.Join(", ", pointsDirection.Select(x => x.ToString()));
                    //Debug.WriteLine($"pointsDirection[{direction}][{pointsDirection.Length}]: {pointsString}");
                    if (MouseController.isMouseInPoints(pointsDirection))
                    {
                        selectMode = SelectMode.Move;
                        //Debug.WriteLine("Select Start - In Points => Move");
                        DrawController.UpdatePointOffsetDown();
                        break;
                    }

                    //Debug.WriteLine("Select Start - Out Points => Select");
                    // Select again
                    selectMode = SelectMode.Select;
                    ViewSelectedPoints(_stateImageSideType);
                    DrawController.SetStoragePoints();
                    DrawController.ResetOffset();
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
                    switch(selectMode)
                    {
                        case SelectMode.Select:
                            // Select - continue to select the area
                            //Debug.WriteLine("Select - Select");
                            var points = GetPointsFromMouseDownToUp();
                            ViewSelectedPoints(_stateImageSideType, points);
                            break;
                        case SelectMode.Move:
                            // Move - move the area
                            //Debug.WriteLine("Select - Move");
                            DrawController.UpdatePointOffset();
                            ViewSelectedPoints(_stateImageSideType);
                            break;
                    }

                    break;
            }
        }

        public static void EditSelectModeEnd(StateImageSideType _stateImageSideType)
        {
            Point[] selectedPoints = GetPointsFromMouseDownToUp();
            if (selectedPoints.Length <= 1)
            {
                //Debug.WriteLine("Select End - Finish");
                selectMode = SelectMode.None;
                DrawController.ClearSelectors();
                return;
            }

            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    switch (selectMode)
                    {
                        case SelectMode.Select:
                            //Debug.WriteLine("Select End - Select");
                            // Select - Complete the selection of the area, save the Picked Points
                            //var direction = StatesController.currentStateDirection;
                            //string pointsString = string.Join(", ", selectedPoints.Select(x => x.ToString()));
                            //Debug.WriteLine($"UpdateStoragePoints[{direction}][{selectedPoints.Length}]: {pointsString}");
                            DrawController.UpdateStoragePoints(selectedPoints);
                            // Define offset boundaries for SelectMode.Move
                            DrawController.UpdatePointOffsetBounds(MouseController.currentMouseDownPosition, MouseController.currentMousePosition);
                            break;
                        case SelectMode.Move:
                            //Debug.WriteLine("Select End - Move");
                            break;
                    }

                    break;
            }
        }

        #endregion Select


        public static void EditWhenEnterImage(StateImageSideType _stateImageSideType)
        {
            DrawController.ClearSelectors();
        }

        #endregion Editor Modes


        #region Editor View

        private static void ViewMultSelectorAtCurrentToMDownPosition(StateImageSideType stateImageSideType, bool isNeedClear = true)
        {
            ViewMultSelectorAtPoints(stateImageSideType, MouseController.currentMouseDownPosition, MouseController.currentMousePosition, isNeedClear);
        }

        private static void ViewSingleSelectorAtCurrentPosition(StateImageSideType stateImageSideType, bool isNeedClear = true)
        {
            ViewMultSelectorAtPoints(stateImageSideType, MouseController.currentMousePosition, MouseController.currentMousePosition, isNeedClear);
        }

        private static void ViewMultSelectorAtPoints(StateImageSideType stateImageSideType, Point point1, Point point2, bool isNeedClear = true)
        {
            if (!MouseController.isMouseInImage)
                return;
            if (isNeedClear)
                DrawController.ClearSelectors(stateImageSideType);
            DrawController.ViewSelectors(stateImageSideType, point1, point2);
        }

        private static void ViewSelectedPoints(StateImageSideType _stateImageSideType) => ViewSelectedPoints(_stateImageSideType, new Point[] { MouseController.currentMousePosition });

        private static void ViewSelectedPoints(StateImageSideType _stateImageSideType, Point[] points)
        {
            switch (selectMode)
            {
                case SelectMode.None:
                    break;
                case SelectMode.Select:
                    if (points.Length <= 1)
                        ViewSingleSelectorAtCurrentPosition(_stateImageSideType);
                    else
                        ViewMultSelectorAtCurrentToMDownPosition(_stateImageSideType);
                    break;
                case SelectMode.Move:
                    var pointsDirection = DrawController.GetStoragePoints(StatesController.currentStateDirection);
                    DrawController.ViewSelectedPoints(pointsDirection);

                    if (points.Length <= 1 && (DrawController.pointOffsetMin == DrawController.pointOffsetMax))
                    {
                        ViewSingleSelectorAtCurrentPosition(_stateImageSideType, false);
                        break;
                    }

                    Point point1 = DrawController.pointOffsetMin;
                    point1.X += DrawController.pointOffset.X;
                    point1.Y += DrawController.pointOffset.Y;
                    Point point2 = DrawController.pointOffsetMax;
                    point2.X += DrawController.pointOffset.X;
                    point2.Y += DrawController.pointOffset.Y;

                    ViewMultSelectorAtPoints(_stateImageSideType, point1, point2, false);
                    break;
            }
        }

        private static void PickPointAtCurrentPosition()
        {
            if (!MouseController.isMouseInImage)
                return;
            DrawController.pickedPointFromPreview = MouseController.currentMousePosition;
        }

        #endregion Editor View


        #region Editor Functions

        /// <summary>
        /// Get background mesh from saved file or create new one and save.
        /// </summary>
        /// <returns></returns>
        public static WriteableBitmap GetGridBackground()
        {
            string gridBitmapPath = EnvironmentController.GetGridPath();
            WriteableBitmap bitmap;

            if (!File.Exists(gridBitmapPath))
            {
                bitmap = DrawController.GetNewGrid();
                EnvironmentController.SaveBitmapIntoFile(gridBitmapPath, bitmap);
            }
            else
            {
                BitmapImage bitmapTemp = new BitmapImage(new Uri(gridBitmapPath, UriKind.Relative));
                bitmap = new WriteableBitmap(bitmapTemp);
            }

            return bitmap;
        }

        private static Point[] GetPointsFromMouseDownToUp() => GetPointsFromTo(MouseController.currentMouseDownPosition, MouseController.currentMousePosition);

        /// <summary>
        /// Get points from mouse down position to mouse up position
        /// </summary>
        /// <returns></returns>
        private static Point[] GetPointsFromTo(Point point1, Point point2)
        {
            int x1 = Math.Min(point1.X, point2.X);
            int x2 = Math.Max(point1.X, point2.X);
            int y1 = Math.Min(point1.Y, point2.Y);
            int y2 = Math.Max(point1.Y, point2.Y);

            var points = new Point[(x2 - x1 + 1) * (y2 - y1 + 1)];
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

        #endregion Editor Functions

    }
}
