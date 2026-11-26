using System.Globalization;
using CollectionLib.Resources;
using CollectionLib.Resources.ExtensionResources;
using MessageAgent.Resources;
using MessageAgent.Resources.ExtensionResources;
using ResourcesLib.Resolvers;

namespace MessageAgent.Converters;

/// <summary>
/// Converts a MessageAgent Lottie key into its application-overridable animation source.
/// The legacy close key is an alias for CollectionLib's shared close resource.
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
            MessageAgentLottieKeys.Close => ResolveSharedCloseSource(),
            MessageAgentLottieKeys.More => ResolveSource(key, "Lotties/more_point.json"),
            MessageAgentLottieKeys.Minimize => ResolveSource(key, "Lotties/minimize.json"),
            _ => null
        };
    }

    private static string ResolveSharedCloseSource() => LottieResourceResolver.GetSettings(
            CollectionListLottieKeys.Close,
            defaultResources: CollectionListDefaultResourceDictionary.Instance,
            defaultRestingProgress: 1d,
            applicationResourceKeys: new[] { CollectionListLottieKeys.Close },
            defaultResourceKeys: new[] { CollectionListLottieKeys.Close }).Source ?? string.Empty;

    private static string ResolveSource(string key, string fallbackSource) => LottieResourceResolver.GetSource(
            key,
            defaultResources: DefaultResourceDictionary.Instance,
            defaultSource: fallbackSource,
            applicationResourceKeys: new[] { key },
            defaultResourceKeys: new[] { key });

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}