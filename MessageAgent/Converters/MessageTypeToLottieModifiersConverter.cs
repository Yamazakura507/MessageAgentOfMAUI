using LottieAgent.Models;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Services.Extensions;
using System.Globalization;

namespace MessageAgent.Converters;

/// <summary>
/// Resolves the configured modifier collection for a message-type animation.
/// </summary>
public sealed class MessageTypeToLottieModifiersConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MessageType type) type = MessageType.Info;
        if (string.Equals(parameter?.ToString(), "AccentContent", StringComparison.OrdinalIgnoreCase))
            return new LottieModifierCollection().SetColor("Fill", type.GetAccentContent());

        return type.GetEffectiveNotificationAnimationModifiers();
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
