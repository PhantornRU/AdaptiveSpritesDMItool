using AdaptiveSpritesDMItool.Models;
using DMISharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Point = System.Drawing.Point;

namespace AdaptiveSpritesDMItool.Controllers
{
    internal static class StatusBarController
    {
        #region Updates

        public static void UpdateStatus(MouseEventArgs e, System.Windows.Controls.Image _img, StateDirection _stateDirection)
        {
            UpdateStatusSingle(e, _img, _stateDirection);

            switch (StatesController.currentStateEditMode)
            {
                case StateEditType.Fill:
                    UpdateStatusMultiPoints();
                    break;
                case StateEditType.Select:
                    UpdateStatusMultiPoints();
                    break;
                case StateEditType.UndoArea:
                    UpdateStatusMultiPoints();
                    break;
                default:
                    StatesController.stateStatusBarDictionary[StatusBarType.MultiPoint].Text = "---";
                    break;
            }

        }

        public static void UpdateStatusSingle(MouseEventArgs e, System.Windows.Controls.Image _img, StateDirection _stateDirection)
        {
            if (!MouseController.isMouseInImage)
            {
                StatesController.stateStatusBarDictionary[StatusBarType.State].Text = "State: None";
                StatesController.stateStatusBarDictionary[StatusBarType.SinglePoint].Text = "---";
                return;
            }

            string stateText = $"State: {_stateDirection}";
            StatesController.stateStatusBarDictionary[StatusBarType.State].Text = stateText;

            var mousePos = MouseController.GetModifyMousePosition(e, _img);
            var storagePoint = EnvironmentController.dataPixelStorage.GetPointStorage(_stateDirection, mousePos);
            string mousePosText = GetPointText(mousePos);

            string mouseText = $"{mousePosText}";
            if (storagePoint != mousePos)
            {
                string storagePointText = GetPointText(storagePoint);
                mouseText = $"{mousePosText} [{storagePointText}]";
            }

            StatesController.stateStatusBarDictionary[StatusBarType.SinglePoint].Text = mouseText;
        }

        public static void UpdateStatusMultiPoints()
        {
            if (!MouseController.isMouseInImage)
                return;

            string mouseDownPosition = GetPointText(MouseController.currentMouseDownPosition);
            string mousePosition = GetPointText(MouseController.currentMousePosition);

            string text = $"{mouseDownPosition} x {mousePosition}";

            var pointOffset = DrawController.pointOffset;
            if (StatesController.currentStateEditMode == StateEditType.Select
                && !(pointOffset.X == 0 && pointOffset.Y == 0))
            {
                string offsetMin = GetPointText(DrawController.pointOffsetMin);
                string offsetMax = GetPointText(DrawController.pointOffsetMax);

                var point1 = DrawController.pointOffsetMin;
                point1.X += pointOffset.X;
                point1.Y += pointOffset.Y;
                var point2 = DrawController.pointOffsetMax;
                point2.X += pointOffset.X;
                point2.Y += pointOffset.Y;
                string offsetMinMoved = GetPointText(point1);
                string offsetMaxMoved = GetPointText(point2);

                text = $"{offsetMin} x {offsetMax} => {offsetMinMoved} x {offsetMaxMoved}";
            }

            StatesController.stateStatusBarDictionary[StatusBarType.MultiPoint].Text = text;
        }

        #endregion Updates

        #region Helpers

        private static string GetPointText(Point point)
        {
            return $"{point.X}:{point.Y}";
        }

        #endregion Helpers
    }
}
