using AdaptiveSpritesDMItool.Controllers;
using AdaptiveSpritesDMItool.Helpers;
using DMISharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDMItool.Models
{
    public record EnvironmentItem
    {
        public string FileName { get; init; }

        public string FilePath { get; init; }

        public DateTime LastModifiedDate { get; init; }

        public EnvironmentItem(string _fileName, string _filePath, DateTime _lastModifiedDate)
        {
            FileName = _fileName;
            FilePath = _filePath;
            LastModifiedDate = _lastModifiedDate;
        }
    }
}
