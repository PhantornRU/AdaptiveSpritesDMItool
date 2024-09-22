using AdaptiveSpritesDMItool.Models;
using DMISharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Point = System.Drawing.Point;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class MouseController
    {

        public static Point currentMouseDownPosition;
        public static Point currentMousePosition;
        public static Point currentMouseUpPosition;

        public static bool isMouseInImage = false;


        #region Buttons

        public static void state_MouseDown(MouseButtonEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            var stateImage = StatesController.GetImage(_stateDirection, _stateImageSideType);
            StatusBarController.UpdateStatus(e, stateImage, _stateDirection);
            StatesController.UpdateSelectedStateDirection(_stateDirection);

            currentMouseDownPosition = GetModifyMousePosition(e, stateImage);
            currentMousePosition = currentMouseDownPosition;

            if (_stateImageSideType == StateImageSideType.Right)
                UpdateBeforeStorage();

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Single:
                    EditorController.EditSingleMode(_stateImageSideType);
                    break;
                case StateEditType.Fill:
                    EditorController.EditFillModeStart(_stateImageSideType);
                    StatusBarController.UpdateStatusMultiPoints();
                    break;
                case StateEditType.Move:
                    EditorController.EditMoveModeStart(_stateImageSideType);
                    break;
                case StateEditType.Select:
                    EditorController.EditSelectModeStart(_stateImageSideType);
                    StatusBarController.UpdateStatusMultiPoints();
                    break;
                case StateEditType.Delete:
                    EditorController.EditDeleteMode(_stateImageSideType);
                    break;
                case StateEditType.Undo:
                    EditorController.EditUndoMode(_stateImageSideType);
                    break;
                case StateEditType.UndoArea:
                    EditorController.EditUndoAreaModeStart(_stateImageSideType);
                    StatusBarController.UpdateStatusMultiPoints();
                    break;
            }

        }

        public static void state_MouseUp(MouseButtonEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            var stateImage = StatesController.GetImage(_stateDirection, _stateImageSideType);
            StatusBarController.UpdateStatus(e, stateImage, _stateDirection);

            currentMouseUpPosition = GetModifyMousePosition(e, stateImage);
            currentMousePosition = currentMouseUpPosition;

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Fill:
                    EditorController.EditFillModeEnd(_stateImageSideType);
                    StatusBarController.UpdateStatusMultiPoints();
                    break;
                case StateEditType.Move:
                    EditorController.EditMoveModeEnd(_stateImageSideType);
                    break;
                case StateEditType.Select:
                    EditorController.EditSelectModeEnd(_stateImageSideType);
                    StatusBarController.UpdateStatusMultiPoints();
                    break;
                case StateEditType.UndoArea:
                    EditorController.EditUndoAreaModeEnd(_stateImageSideType);
                    StatusBarController.UpdateStatusMultiPoints();
                    break;
            }

        }

        public static void state_MouseMove(MouseEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            if (StatesController.currentStateDirection != StatesController.selectedStateDirection)
                return;

            var stateImage = StatesController.GetImage(_stateDirection, _stateImageSideType);
            StatusBarController.UpdateStatus(e, stateImage, _stateDirection);

            bool mouseIsDown = System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed;
            if (!mouseIsDown)
                return;
            StatesController.UpdateCurrentStateDirection(_stateDirection);

            currentMousePosition = GetModifyMousePosition(e, stateImage);

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Single:
                    EditorController.EditSingleMode(_stateImageSideType);
                    break;
                case StateEditType.Fill:
                    EditorController.EditFillMode(_stateImageSideType);
                    break;
                case StateEditType.Move:
                    EditorController.EditMoveMode(_stateImageSideType);
                    break;
                case StateEditType.Select:
                    EditorController.EditSelectMode(_stateImageSideType);
                    break;
                case StateEditType.Delete:
                    EditorController.EditDeleteMode(_stateImageSideType);
                    break;
                case StateEditType.Undo:
                    EditorController.EditUndoMode(_stateImageSideType);
                    break;
                case StateEditType.UndoArea:
                    EditorController.EditUndoAreaMode(_stateImageSideType);
                    break;
            }
        }

        public static void state_MouseEnter(MouseEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            StatesController.UpdateCurrentStateDirection(_stateDirection);
            isMouseInImage = true;

            bool mouseIsDown = System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed;
            if (!mouseIsDown)
                return;
            EditorController.EditWhenEnterImage(_stateImageSideType);
        }

        public static void state_MouseLeave(MouseEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            isMouseInImage = false;
        }

        #endregion Buttons


        #region Helpers

        public static Point GetModifyMousePosition(System.Windows.Input.MouseButtonEventArgs _e, System.Windows.Controls.Image _img)
        {
            System.Windows.Point pos = _e.GetPosition(_img);
            return GetModifyPosition(pos, _img);
        }

        public static Point GetModifyMousePosition(System.Windows.Input.MouseEventArgs _e, System.Windows.Controls.Image _img)
        {
            System.Windows.Point pos = _e.GetPosition(_img);
            return GetModifyPosition(pos, _img);

        }

        private static Point GetModifyPosition(System.Windows.Point _pos, System.Windows.Controls.Image _img)
        {
            int x = (int)Math.Floor(_pos.X * _img.Source.Width / _img.ActualWidth);
            int y = (int)Math.Floor(_pos.Y * _img.Source.Height / _img.ActualHeight);
            return new Point(x, y);
        }

        private static void UpdateBeforeStorage()
        {
            var currentStateEditMode = StatesController.currentStateEditMode;
            if (currentStateEditMode == StateEditType.Select &&
                EditorController.selectMode == Models.SelectMode.Select)
            {
                return;
            }

            EnvironmentController.dataPixelStorage.UpdateBeforeStorage();
        }

        #endregion Helpers

        #region Getters

        public static bool isMouseInPoints(Point[] _points)
        {
            if (_points == null || _points.Length == 0)
                return false;

            var x = currentMousePosition.X;
            var y = currentMousePosition.Y;

            return _points.Any(p => p.X == x && p.Y == y);
        }


        #endregion Getters
    }
}
