using DMISharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmpCore.Options;
using Point = System.Drawing.Point;

namespace AdaptiveSpritesDMItool.Helpers
{
    internal static class UndoSaves
    {
        /// <summary>
        /// How many elements are allowed in a stack
        /// </summary>
        private static int undoCount = 20;

        /// <summary>
        /// Save the current canvas for undoing.
        /// </summary>
        private static LinkedList<ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>>> pixelsStorages =
            new LinkedList<ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>>>();
        // Stack<> is not suitable because we need to remove the first element, so LinkedList<> is selected.

        /// <summary>
        /// Undo changes
        /// </summary>
        public static ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>>? GetUndoStorage()
        {
            if (pixelsStorages.Count == 0)
                return null;

            // Similarly: Pop();
            var pixelStorage = pixelsStorages.Last();
            pixelsStorages.RemoveLast();
            return pixelStorage;
        }

        /// <summary>
        /// Save state before making changes
        /// </summary>
        public static void SaveStorage(ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>>? _pixelsStorage)
        {
            if (_pixelsStorage == null)
                return;

            if (pixelsStorages.Count >= undoCount)
                pixelsStorages.RemoveFirst();

            var tempPixelStorage = new ConcurrentDictionary<StateDirection, ConcurrentDictionary<(int x, int y), (int x, int y)>>();
            foreach (var direction in _pixelsStorage.Keys)
                tempPixelStorage[direction] = new ConcurrentDictionary<(int x, int y), (int x, int y)>(_pixelsStorage[direction]);

            //Similarly: Push(tempPixelStorage);
            pixelsStorages.AddLast(tempPixelStorage);
            Debug.WriteLine($"UndoStack PUSH: {pixelsStorages.Count}");
        }

    }
}
