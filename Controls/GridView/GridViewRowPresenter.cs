// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.
// https://github.com/lepoco/wpfui/blob/main/src/Wpf.Ui/Controls/GridView/GridViewRowPresenter.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdaptiveSpritesDMItool.Controls
{
    /// <summary>
    /// Extends <see cref="System.Windows.Controls.GridViewRowPresenter"/>, and adds header row layout support for <see cref="GridViewColumn"/>, which can have <see cref="GridViewColumn.MinWidth"/> and <see cref="GridViewColumn.MaxWidth"/>.
    /// </summary>
    public class GridViewRowPresenter : System.Windows.Controls.GridViewRowPresenter
    {
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // update the desired width of each column (clamps desiredwidth to MinWidth and MaxWidth)
            if (Columns != null)
            {
                foreach (GridViewColumn column in Columns.OfType<GridViewColumn>())
                {
                    column.UpdateDesiredWidth();
                }
            }

            return base.ArrangeOverride(arrangeSize);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Columns != null)
            {
                foreach (GridViewColumn column in Columns.OfType<GridViewColumn>())
                {
                    column.UpdateDesiredWidth();
                }
            }

            return base.MeasureOverride(constraint);
        }
    }
}
