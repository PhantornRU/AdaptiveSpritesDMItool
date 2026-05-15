using AdaptiveSpritesDmiTool.Application;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public sealed class SpriteImageBitmapSourceFactory
{
    private readonly double _dpi = 96;

    public BitmapSource? Create(SpriteImage? image)
    {
        if (image is null)
        {
            return null;
        }

        var bgraBytes = new byte[image.RgbaBytes.Length];
        for (var index = 0; index < image.RgbaBytes.Length; index += 4)
        {
            bgraBytes[index] = image.RgbaBytes[index + 2];
            bgraBytes[index + 1] = image.RgbaBytes[index + 1];
            bgraBytes[index + 2] = image.RgbaBytes[index];
            bgraBytes[index + 3] = image.RgbaBytes[index + 3];
        }

        var bitmap = BitmapSource.Create(
            image.Width,
            image.Height,
            _dpi,
            _dpi,
            PixelFormats.Bgra32,
            null,
            bgraBytes,
            image.Width * 4);

        bitmap.Freeze();
        return bitmap;
    }
}