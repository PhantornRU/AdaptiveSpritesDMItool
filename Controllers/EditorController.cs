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








        public static void EditMoveMode(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:
                    //ViewSelectedPoints(_stateImageSideType);
                    //DrawController.ClearSelectors();
                    //DrawController.ViewSelectedPoints(points);
                    //ViewSingleSelectorAtCurrentPosition(_stateImageSideType, false);
                    
                    //EditSelectModeStart(_stateImageSideType);
                    break;
            }
        }





        static SelectMode selectMode = SelectMode.None;

        public static void EditSelectModeStart(StateImageSideType _stateImageSideType)
        {
            switch (_stateImageSideType)
            {
                case StateImageSideType.Left:
                    break;
                case StateImageSideType.Right:

                    // Если нажата мышь в пределах УЖЕ выделенной области - то
                    //      Устанавливаем Move - режим перемещения
                    var direction = StatesController.currentStateDirection;
                    var pointsDirection = DrawController.GetStoragePoints(direction);
                    //string pointsString = string.Join(", ", pointsDirection.Select(x => x.ToString()));
                    //Debug.WriteLine($"pointsDirection[{direction}][{pointsDirection.Length}]: {pointsString}");
                    if (MouseController.isMouseInPoints(pointsDirection))
                    {
                        selectMode = SelectMode.Move;
                        Debug.WriteLine("Select Start - In Points => Move");
                        DrawController.UpdatePointOffsetDown();
                        //DrawController.CopySelectedOffsetPoints();
                        break;
                    }

                    // Если нажата мышь ЗА пределами - то режим:
                    //      Устанавливаем Select для создания новой выборки

                    // Делаем сброс и очищаем полотно. Всё заного.
                    // !!!!!! Сделать потом проверку нажато ли 1 раз и только тогда сбрасывать, иначе разрешить выделение !!!!!!
                    //if (selectMode == SelectMode.Select)
                    //{
                    //    Debug.WriteLine("Select Start - Out Points - Select => None");
                    //    selectMode = SelectMode.None;
                    //    DrawController.ClearSelectors();
                    //    break;
                    //}

                    // Завершаем двигание объекта. Сохраняем перемещенные точки на полотно.
                    //if (selectMode == SelectMode.Move)
                    //{
                    //    Debug.WriteLine("Select Start - Out Points - Move");
                    //    // !!!!!!!! УСТАНАВЛИВАЕМ ПЕРЕМЕЩЕННОЕ ИЗОБРАЖЕНИЕ И ПОИНТЫ !!!!!!
                    //    // !!!!! COPY PIXELS !!!!!
                    //    // Делаем сброс и очищаем полотно.
                    //    selectMode = SelectMode.None;
                    //    DrawController.ClearSelectors();
                    //    DrawController.ShiftOffset();
                    //    break;
                    //}

                    Debug.WriteLine("Select Start - Out Points => Select");
                    // Начинаем вновь выделять
                    selectMode = SelectMode.Select;
                    ViewSelectedPoints(_stateImageSideType);
                    DrawController.ShiftOffset();

                    //UpdatePixel

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
                    //var points = GetPointsFromMouseDownToUp();
                    //ViewSelectedPoints(_stateImageSideType, points);

                    // Зависимость от режима

                    switch(selectMode)
                    {
                        case SelectMode.Select:
                            // Select - продолжаем выделять область
                            //Debug.WriteLine("Select - Select");
                            var points = GetPointsFromMouseDownToUp();
                            ViewSelectedPoints(_stateImageSideType, points);
                            break;
                        case SelectMode.Move:
                            // Move - двигаем изображение
                            //Debug.WriteLine("Select - Move");

                            // Обновляем оффсет точку
                            DrawController.UpdatePointOffset();
                            ViewSelectedPoints(_stateImageSideType);
                            //DrawController.WriteSelectedOffsetPoints();


                            // Показываем выделенные поинты
                            //Point[] pickedPoints = DrawController.GetStoragePoints(StatesController.currentStateDirection);
                            //ViewSelectedPoints(_stateImageSideType, pickedPoints);


                            // !!!!!!!!!! ДВИГАЕМ БИТМАП ИЗОБРАЖЕНИЕ !!!!!!!!!!!!
                            // !!!!! WRITE PIXELS !!!!!
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
                Debug.WriteLine("Select End - Finish");
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
                            Debug.WriteLine("Select End - Select");
                            // Select - Завершаем выделение области, сохраняем Picked Points
                            var direction = StatesController.currentStateDirection;
                            string pointsString = string.Join(", ", selectedPoints.Select(x => x.ToString()));
                            Debug.WriteLine($"UpdateStoragePoints[{direction}][{selectedPoints.Length}]: {pointsString}");
                            DrawController.UpdateStoragePoints(selectedPoints);
                            // Определяем границы оффсета для SelectMode.Move
                            DrawController.UpdatePointOffsetBounds(MouseController.currentMouseDownPosition, MouseController.currentMousePosition);
                            break;
                        case SelectMode.Move:
                            Debug.WriteLine("Select End - Move");
                            // Move - Отпускаем изображение
                            //Point[] checkPoints = DrawController.GetStoragePoints(StatesController.currentStateDirection);
                            break;
                    }

                    break;
            }
        }




        //private static void 











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
                    Point point1 = DrawController.pointOffsetMin;
                    point1.X += DrawController.pointOffset.X;
                    point1.Y += DrawController.pointOffset.Y;
                    Point point2 = DrawController.pointOffsetMax;
                    point2.X += DrawController.pointOffset.X;
                    point2.Y += DrawController.pointOffset.Y;

                    var pointsDirection = DrawController.GetStoragePoints(StatesController.currentStateDirection);
                    DrawController.ViewSelectedPoints(pointsDirection);
                    ViewMultSelectorAtPoints(_stateImageSideType, point1, point2, false);
                    break;
            }
        }






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
