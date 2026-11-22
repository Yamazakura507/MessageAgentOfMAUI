using System.Globalization;
using MessageAgent.Core.Enums.Message;

namespace MessageAgent.Converters;

/// <summary>
/// Returns geometry-aware padding for a compact notification label.
/// </summary>
public sealed class MessageLabelShapePaddingConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MessageLabelShape.Message ? new Thickness(8, 3, 8, 9) : new Thickness(7, 4);

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
