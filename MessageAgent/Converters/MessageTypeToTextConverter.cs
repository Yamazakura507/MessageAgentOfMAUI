using System.Globalization;
using MessageAgent.Core.Models.ExtentionKinds;

namespace MessageAgent.Converters;

/// <summary>
/// Converts a message type to its localized display name.
/// </summary>
public sealed class MessageTypeToTextConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MessageType type ? string.IsNullOrWhiteSpace(type.DisplayName) ? type.Name : type.DisplayName : "Информация";

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
