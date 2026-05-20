using AdaptiveSpritesDmiTool.Application;
using System.Globalization;
using System.Windows.Data;
using WpfBinding = System.Windows.Data.Binding;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

internal sealed class OverwritePolicyLabelConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        value switch
        {
            OverwritePolicy.SkipExisting => App.Text("Text.Batch.Policy.SkipExisting", "Skip existing"),
            OverwritePolicy.OverwriteExisting => App.Text("Text.Batch.Policy.OverwriteExisting", "Overwrite existing"),
            OverwritePolicy.FailIfExists => App.Text("Text.Batch.Policy.FailIfExists", "Fail if exists"),
            _ => value?.ToString() ?? string.Empty
        };

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        WpfBinding.DoNothing;
}
