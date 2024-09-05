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
        Move,
        Select,
        Delete,
        Undo,
        UndoArea
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
        Preview,
        Background,
        Overlay,
        Selection,
        TextGrid
    }

    public enum StateImageSideType
    {
        Left,
        Right
    }

    public enum SelectMode
    {
        None,
        Select,
        Move
    }

    public enum StatusBarType
    {
        State,
        SinglePoint,
        MultiPoint
    }
}
