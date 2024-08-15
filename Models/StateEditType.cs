using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdaptiveSpritesDMItool.Models
{
    /// <summary>
    /// Main preview window editing mode
    /// </summary>
    public enum StateEditType
    {
        Single,
        Fill,
        Pick,
        Delete,
        Select,
        Move
    }

    /// <summary>
    /// How many states are taken for editing
    /// </summary>
    public enum StateQuantityType
    {
        Single,
        Parallel,
        All
    }


    /// <summary>
    /// Type of state image
    /// </summary>
    public enum StateImageType
    {
        Left,
        Right,
        BackgroundLeft,
        BackgroundRight,
        OverlayLeft,
        OverlayRight,
        SelectionLeft,
        SelectionRight,
    }
}
