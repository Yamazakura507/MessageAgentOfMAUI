using LottieAgent.Models;
using MessageAgent.Core.Enums;
using MessageAgent.Services.Extensions;
using System.Globalization;

namespace MessageAgent.Converters
{
    public sealed class MessageTypeToLottieModifiersConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            MessageType type = value is MessageType messageType ? messageType : MessageType.Info;

            return new LottieModifierCollection().SetColor("info contornos", type.GetAccentColor());
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
