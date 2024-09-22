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
    /// Preview element type.
    /// </summary>

    public enum StateImageType
    {
        Preview,
        Background,
        Overlay,
        Selection,
        TextGrid
    }

    /// <summary>
    /// Side of the preview element.
    /// </summary>
    public enum StateImageSideType
    {
        Left,
        Right
    }

    /// <summary>
    /// The current mode of the pixel move tool.
    /// </summary>
    public enum SelectMode
    {
        None,
        Select,
        Move
    }

    /// <summary>
    /// Element type status bar.
    /// </summary>
    public enum StatusBarType
    {
        State,
        SinglePoint,
        MultiPoint
    }

    /// <summary>
    /// The selected preview type for state overlay.
    /// </summary>
    public enum StatePreviewType
    {
        Left,
        Right,
        Overlay
    }
}
