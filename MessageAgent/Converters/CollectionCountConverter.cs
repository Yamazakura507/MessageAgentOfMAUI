using System.Collections;
using System.Globalization;

namespace MessageAgent.Converters;

/// <summary>
/// Returns whether an enumerable contains at least the configured number of elements.
/// </summary>
public sealed class CollectionCountConverter : IValueConverter
{
    public int MinimumCount { get; set; } = 1;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int minimum = Math.Max(1, MinimumCount);

        if (value is ICollection collection) return collection.Count >= minimum;
        if (value is not IEnumerable items) return false;

        IEnumerator enumerator = items.GetEnumerator();

        try
        {
            for (int count = 0; count < minimum; count++)
            {
                if (!enumerator.MoveNext()) return false;
            }

            return true;
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
