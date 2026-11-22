using System.Globalization;

namespace MessageAgent.Converters
{
    /// <summary>
    /// Truncates a message title to an optional maximum length.
    /// </summary>
    public sealed class MessageTitleConverter : IMultiValueConverter
    {
        /// <inheritdoc />
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            string title = values.FirstOrDefault() as string ?? string.Empty;
            int? maxLength = values.Length > 1 && values[1] is int length && length >= 3 ? length : null;

            if (maxLength is not int limit || title.Length <= limit) return title;

            return limit == 3 ? "..." : title[..(limit - 3)] + "...";
        }
        /// <inheritdoc />
        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
