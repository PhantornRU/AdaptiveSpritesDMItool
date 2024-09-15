using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using DMISharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDMItool.Models
{
    public record StateItem
    {
        public string FileName { get; init; }

        public string FilePath { get; init; }

        public string StateName { get; init; }

        public Brush? Color { get; init; }


        //public string Name => $"{FileName} {StateName}";

        public BitmapSource ImageSource { get; init; }

        public StateItem(string _fileName, string _filePath, string _stateName, BitmapSource _bmpIcon, Brush? _color = null)
        {
            FileName = _fileName;
            FilePath = _filePath;
            StateName = _stateName;
            ImageSource = _bmpIcon;
            Color = _color;
        }
    }
}
