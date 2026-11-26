using System.Globalization;
using MessageAgent.Resources;
using MessageAgent.Resources.ExtensionResources;
using ResourcesLib.Resolvers;

namespace MessageAgent.Converters;

/// <summary>
/// Converts a MessageAgent image key into its application-overridable image source.
/// </summary>
public sealed class MessageAgentImageSourceConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? key = parameter?.ToString() ?? value?.ToString();

        return key switch
        {
            MessageAgentImageKeys.Read => ImageResourceResolver.Get(key, "message_read.png", DefaultResourceDictionary.Instance),
            MessageAgentImageKeys.Unread => ImageResourceResolver.Get(key, "message_unread.png", DefaultResourceDictionary.Instance),
            MessageAgentImageKeys.Important => ImageResourceResolver.Get(key, "important.png", DefaultResourceDictionary.Instance),
            MessageAgentImageKeys.Input => ImageResourceResolver.Get(key, "message_input.png", DefaultResourceDictionary.Instance),
            MessageAgentImageKeys.Question => ImageResourceResolver.Get(key, "message_question.png", DefaultResourceDictionary.Instance),
            MessageAgentImageKeys.Captcha => ImageResourceResolver.Get(key, "message_captcha.png", DefaultResourceDictionary.Instance),
            MessageAgentImageKeys.CaptchaDragHorizontal => ImageResourceResolver.Get(key, "captcha_drag_horizontal.png", DefaultResourceDictionary.Instance),
            MessageAgentImageKeys.CaptchaDragVertical => ImageResourceResolver.Get(key, "captcha_drag_vertical.png", DefaultResourceDictionary.Instance),
            _ => null
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
