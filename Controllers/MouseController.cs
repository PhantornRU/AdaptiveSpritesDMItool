using AdaptiveSpritesDMItool.Models;
using DMISharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class MouseController
    {

        public static System.Drawing.Point currentMouseDownPosition;
        public static System.Drawing.Point currentMousePosition;
        public static System.Drawing.Point currentMouseUpPosition;

        public static bool isMouseInImage = false;


        #region Buttons

        public static void state_MouseDown(MouseButtonEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            var stateImage = StatesController.GetImage(_stateDirection, _stateImageSideType);

            currentMouseDownPosition = GetModifyMousePosition(e, stateImage);
            currentMousePosition = currentMouseDownPosition;

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Single:
                    EditorController.EditSingleMode(_stateImageSideType);
                    break;
                case StateEditType.Fill:
                    EditorController.EditFillModeStart();
                    break;
                case StateEditType.Move:
                    EditorController.EditMoveMode();
                    break;
                case StateEditType.Select:
                    EditorController.EditSelectModeStart();
                    break;
                case StateEditType.Delete:
                    EditorController.EditDeleteMode();
                    break;
                case StateEditType.Undo:
                    EditorController.EditUndoMode();
                    break;
                case StateEditType.UndoArea:
                    EditorController.EditUndoAreaModeStart();
                    break;
            }
        }

        public static void state_MouseUp(MouseButtonEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            var stateImage = StatesController.GetImage(_stateDirection, _stateImageSideType);

            currentMouseUpPosition = GetModifyMousePosition(e, stateImage);
            currentMousePosition = currentMouseUpPosition;

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Fill:
                    EditorController.EditFillModeEnd();
                    break;
                case StateEditType.Select:
                    EditorController.EditSelectModeEnd();
                    break;
                case StateEditType.UndoArea:
                    EditorController.EditUndoAreaModeEnd();
                    break;
            }

        }

        public static void state_MouseMove(MouseEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            bool mouseIsDown = System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed;
            if (!mouseIsDown)
                return;
            var stateImage = StatesController.GetImage(_stateDirection, _stateImageSideType);
            StatesController.UpdateCurrentStateDirection(_stateDirection);

            currentMousePosition = GetModifyMousePosition(e, stateImage);

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Single:
                    EditorController.EditSingleMode(_stateImageSideType);
                    break;
                case StateEditType.Fill:
                    EditorController.EditFillMode();
                    break;
                case StateEditType.Move:
                    EditorController.EditMoveMode();
                    break;
                case StateEditType.Select:
                    EditorController.EditSelectMode();
                    break;
                case StateEditType.Delete:
                    EditorController.EditDeleteMode();
                    break;
                case StateEditType.Undo:
                    EditorController.EditUndoMode();
                    break;
                case StateEditType.UndoArea:
                    EditorController.EditUndoAreaMode();
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
            EditorController.EditWhenEnterImage();
        }

        public static void state_MouseLeave(MouseEventArgs e, StateDirection _stateDirection, StateImageSideType _stateImageSideType)
        {
            isMouseInImage = false;
        }

        #endregion Buttons


        #region Helpers

        public static System.Drawing.Point GetModifyMousePosition(System.Windows.Input.MouseButtonEventArgs _e, System.Windows.Controls.Image _img)
        {
            Point pos = _e.GetPosition(_img);
            return GetModifyPosition(pos, _img);
        }

        public static System.Drawing.Point GetModifyMousePosition(System.Windows.Input.MouseEventArgs _e, System.Windows.Controls.Image _img)
        {
            Point pos = _e.GetPosition(_img);
            return GetModifyPosition(pos, _img);

        }

        private static System.Drawing.Point GetModifyPosition(Point _pos, System.Windows.Controls.Image _img)
        {
            int x = (int)Math.Floor(_pos.X * _img.Source.Width / _img.ActualWidth);
            int y = (int)Math.Floor(_pos.Y * _img.Source.Height / _img.ActualHeight);
            return new System.Drawing.Point(x, y);
        }

        #endregion Helpers

        #region Getters

        public static System.Drawing.Point GetCurrentMousePosition() => currentMousePosition;

        #endregion Getters
    }
}
