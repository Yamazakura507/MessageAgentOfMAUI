using System.Globalization;

namespace MessageAgent.Converters;

/// <summary>
/// Creates contrasting loading-dialog colors from the dominant Lottie accent color.
/// </summary>
public sealed class LoadingAccentColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Color accent) return Colors.Transparent;

        float red = accent.Red;
        float green = accent.Green;
        float blue = accent.Blue;
        float luminance = 0.2126f * red + 0.7152f * green + 0.0722f * blue;

        return parameter?.ToString() switch
        {
            "Surface" => accent.WithAlpha(0.12f),
            "Border" => accent.WithAlpha(0.7f),
            "Foreground" => luminance > 0.58f ? Colors.Black : Colors.White,
            _ => accent
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}