using System.Globalization;

namespace MessageAgent.Converters;

/// <summary>
/// Converts the read flag to either its text label or the corresponding image source.
/// </summary>
public sealed class ReadStateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isRead = value is true;

        return parameter?.ToString() switch
        {
            "Image" => ImageSource.FromFile(isRead ? "message_read.png" : "message_unread.png"),
            _ => isRead ? "Прочитано" : "Не прочитано"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
