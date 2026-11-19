using System.Globalization;
using MessageAgent.Core.Enums;
using MessageAgent.Services.Extensions;

namespace MessageAgent.Converters;

/// <summary>
/// Converts a message type to its Accent or Background color.
/// </summary>
public sealed class MessageTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        MessageType type = value is MessageType messageType ? messageType : MessageType.Info;

        return parameter?.ToString() switch
        {
            "Background" => type.GetBackgroundColor(),
            _ => type.GetAccentColor()
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
