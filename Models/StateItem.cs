using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDMItool.Models
{
    public record StateItem
    {
        public string FileName { get; init; }

        public string StateName { get; init; }

        //public string Name => $"{FileName} {StateName}";

        public BitmapSource ImageSource { get; init; }

        public StateItem(string _fileName, string _stateName, BitmapSource _imageSource)
        {
            FileName = _fileName;
            StateName = _stateName;
            ImageSource = _imageSource;
        }
    }
}
