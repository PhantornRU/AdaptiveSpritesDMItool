using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdaptiveSpritesDMItool.Helpers
{
    internal static class MouseController
    {
        public static System.Drawing.Point GetMouseCoordinates(System.Windows.Input.MouseButtonEventArgs _e, System.Windows.Controls.Image _img)
        {
            System.Windows.Point pos = _e.GetPosition(_img);
            int x = (int)Math.Floor(pos.X * _img.Source.Width / _img.ActualWidth);
            int y = (int)Math.Floor(pos.Y * _img.Source.Height / _img.ActualHeight);
            return new System.Drawing.Point(x, y);

        }
    }


}
