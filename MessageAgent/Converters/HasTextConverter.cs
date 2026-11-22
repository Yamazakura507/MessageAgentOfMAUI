using System.Globalization;

namespace MessageAgent.Converters
{
    /// <summary>
    /// Converts text values to a visibility-compatible Boolean.
    /// </summary>
    public sealed class HasTextConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !string.IsNullOrWhiteSpace(value as string);
        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
