using System.Globalization;
using MessageAgent.Resources.ExtensionResources;
using MessageAgent.Services.Extensions;

namespace MessageAgent.Converters;

/// <summary>
/// Converts a MessageAgent Lottie key into its application-overridable animation source.
/// </summary>
public sealed class MessageAgentLottieSourceConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? key = parameter?.ToString();

        return key switch
        {
            MessageAgentLottieKeys.Arrow => MessageAgentLotties.Get(key, "Lotties/arrow.json"),
            MessageAgentLottieKeys.Close => MessageAgentLotties.Get(key, "Lotties/close.json"),
            MessageAgentLottieKeys.More => MessageAgentLotties.Get(key, "Lotties/more_point.json"),
            MessageAgentLottieKeys.Minimize => MessageAgentLotties.Get(key, "Lotties/minimize.json"),
            _ => null
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
