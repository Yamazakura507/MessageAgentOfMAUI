using System.Globalization;
using MessageAgent.Core.Enums;

namespace MessageAgent.Converters;

public sealed class MessageTypeToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)=> value is MessageType type
        ? type switch
        {
            MessageType.Error => "Ошибка",
            MessageType.Alert => "Предупреждение",
            MessageType.Info => "Информация",
            _ => "Информация"
        }
        : "Информация";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
