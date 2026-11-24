using System.Globalization;
using CollectionLib.Resources;
using CollectionLib.Resources.ExtensionResources;
using MessageAgent.Resources;
using MessageAgent.Resources.ExtensionResources;
using ResourcesLib.Resolvers;

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
            MessageAgentLottieKeys.Arrow => ResolveSource(key, "Lotties/arrow.json"),
            MessageAgentLottieKeys.Close => ResolveSource(key, "Lotties/close.json"),
            MessageAgentLottieKeys.More => ResolveSource(key, "Lotties/more_point.json"),
            MessageAgentLottieKeys.Minimize => ResolveSource(key, "Lotties/minimize.json"),
            _ => null
        };
    }

    private static string ResolveSource(string key, string fallbackSource)
    {
        bool isSharedClose = key == MessageAgentLottieKeys.Close;

        return LottieResourceResolver.GetSource(
            key,
            defaultResources: isSharedClose
                ? CollectionListDefaultResourceDictionary.Instance
                : DefaultResourceDictionary.Instance,
            defaultSource: fallbackSource,
            applicationResourceKeys: isSharedClose
                ? new[] { CollectionListLottieKeys.Close, key }
                : new[] { key },
            defaultResourceKeys: isSharedClose
                ? new[] { CollectionListLottieKeys.Close }
                : new[] { key });
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
