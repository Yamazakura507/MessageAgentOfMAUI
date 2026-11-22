using System.Globalization;
using MessageAgent.Resources.ExtensionResources;
using MessageAgent.Services.Extensions;

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
                        ? MessageAgentImages.Get(MessageAgentImageKeys.Read, "message_read.png") 
                        : MessageAgentImages.Get(MessageAgentImageKeys.Unread, "message_unread.png"),
            _ => isRead ? "Прочитано" : "Не прочитано"
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
