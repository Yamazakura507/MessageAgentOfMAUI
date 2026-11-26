using System.Globalization;
using MessageAgent.Resources.ExtensionResources;

namespace MessageAgent.Converters;

/// <summary>
/// Resolves a MessageAgent color key, preferring Application.Resources over library defaults.
/// </summary>
public sealed class MessageAgentColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? key = parameter?.ToString() ?? value?.ToString();

        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return MessageAgentResources.GetColor(key);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
