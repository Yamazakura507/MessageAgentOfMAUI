using System.Globalization;
using MessageAgent.Resources;
using MessageAgent.Resources.ExtensionResources;
using ResourcesLib.Resolvers;

namespace MessageAgent.Converters;

/// <summary>
/// Converts the read flag to either its text label or the corresponding image source.
/// </summary>
public sealed class ReadStateConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isRead = value is true;

        return parameter?.ToString() switch
        {
            "Image" => isRead
                ? ImageResourceResolver.Get(MessageAgentImageKeys.Read, "message_read.png", DefaultResourceDictionary.Instance)
                : ImageResourceResolver.Get(MessageAgentImageKeys.Unread, "message_unread.png", DefaultResourceDictionary.Instance),
            _ => isRead ? "Прочитано" : "Не прочитано"
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
