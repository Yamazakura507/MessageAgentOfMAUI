using System.Globalization;
using MessageAgent.Resources.ExtensionImages;
using MessageAgent.Services.Extensions;

namespace MessageAgent.Converters;

/// <summary>
/// Converts a MessageAgent image key into its application-overridable image source.
/// </summary>
public sealed class MessageAgentImageSourceConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? key = parameter?.ToString();

        return key switch
        {
            MessageAgentImageKeys.Read => MessageAgentImages.Get(key, "message_read.png"),
            MessageAgentImageKeys.Unread => MessageAgentImages.Get(key, "message_unread.png"),
            MessageAgentImageKeys.Important => MessageAgentImages.Get(key, "important.png"),
            _ => null
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
