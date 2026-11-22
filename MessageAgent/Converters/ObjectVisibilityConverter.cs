using System.Globalization;

namespace MessageAgent.Converters;

/// <summary>
/// Returns whether a bound object is present; Invert can be used for fallback visuals.
/// </summary>
public sealed class ObjectVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets whether the visibility result is inverted.
    /// </summary>
    public bool Invert { get; set; }

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is not null) != Invert;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
