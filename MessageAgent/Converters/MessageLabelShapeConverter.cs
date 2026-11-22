using System.Globalization;
using MessageAgent.Controls.Provider;
using MessageAgent.Services;
using Microsoft.Maui.Controls.Shapes;
using MessageAgent.Core.Enums.Message;

namespace MessageAgent.Converters;

/// <summary>
/// Creates the geometry for a notification label from its selected shape.
/// </summary>
public sealed class MessageLabelShapeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MessageLabelShape shape ? shape switch
        {
            MessageLabelShape.Message => new SpeechBubbleShape(),
            MessageLabelShape.Pill => new RoundRectangle { CornerRadius = new CornerRadius(100) },
            _ => new RoundRectangle { CornerRadius = new CornerRadius(10) }
        } : new RoundRectangle { CornerRadius = new CornerRadius(10) };

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
