using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdaptiveSpritesDMItool.Helpers
{
    internal static class MouseController
    {
        public static System.Drawing.Point GetModifyMousePosition(System.Windows.Input.MouseButtonEventArgs _e, System.Windows.Controls.Image _img)
        {
            System.Windows.Point pos = _e.GetPosition(_img);
            return GetModifyPosition(pos, _img);
        }

        public static System.Drawing.Point GetModifyMousePosition(System.Windows.Input.MouseEventArgs _e, System.Windows.Controls.Image _img)
        {
            System.Windows.Point pos = _e.GetPosition(_img);
            return GetModifyPosition(pos, _img);

        }

        private static System.Drawing.Point GetModifyPosition(System.Windows.Point _pos, System.Windows.Controls.Image _img)
        {
            int x = (int)Math.Floor(_pos.X * _img.Source.Width / _img.ActualWidth);
            int y = (int)Math.Floor(_pos.Y * _img.Source.Height / _img.ActualHeight);
            return new System.Drawing.Point(x, y);
        }
    }


}
