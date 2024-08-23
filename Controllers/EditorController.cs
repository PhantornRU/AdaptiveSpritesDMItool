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
                    DrawController.SetPickedPoints();
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
                        DrawController.SetPickedPoints(points);
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
                    DrawController.ClearSelectors();
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

        public static void EditSelectModeStart(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    DrawController.ClearSelectors();
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
            DrawController.ClearSelectors();
        }

        #endregion Editor Modes


        #region Editor View

        private static void ViewMultSelectorAtCurrentToMDownPosition(StateImageSideType stateImageSideType)
        {
            if (MouseController.isMouseInImage)
                DrawController.SetSelectors(stateImageSideType, MouseController.currentMouseDownPosition, MouseController.currentMousePosition);
        }

        private static void ViewSingleSelectorAtCurrentPosition(StateImageSideType stateImageSideType)
        {
            if (MouseController.isMouseInImage)
                DrawController.SetSelectors(stateImageSideType, MouseController.currentMousePosition);
        }

        private static void PickPointAtCurrentPosition()
        {
            if (MouseController.isMouseInImage)
                DrawController.pickedPoint = MouseController.currentMousePosition;
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

        /// <summary>
        /// Get points from mouse down position to mouse up position
        /// </summary>
        /// <returns></returns>
        private static Point[] GetPointsFromMouseDownToUp()
        {
            int x1 = Math.Min(MouseController.currentMouseDownPosition.X, MouseController.currentMousePosition.X);
            int x2 = Math.Max(MouseController.currentMouseDownPosition.X, MouseController.currentMousePosition.X);
            int y1 = Math.Min(MouseController.currentMouseDownPosition.Y, MouseController.currentMousePosition.Y);
            int y2 = Math.Max(MouseController.currentMouseDownPosition.Y, MouseController.currentMousePosition.Y);

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
