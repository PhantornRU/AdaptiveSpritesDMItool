using System.Globalization;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

internal static class WorkspaceEnumParsing
{
    public static TEnum ParseDefinedEnumOrDefault<TEnum>(
        string? value,
        TEnum fallback)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value) ||
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return fallback;
        }

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed)
                ? parsed
                : fallback;
    }
}
