using CollectionLib.Resources;
using CollectionLib.Resources.ExtensionResources;
using LottieAgent.Controls;
using LottieAgent.Models;
using MessageAgent.Services.Extensions;
using ResourcesLib.Models;
using ResourcesLib.Resolvers;

namespace MessageAgent.Services.Factory;

/// <summary>
/// Creates shared Lottie settings using application-overridable resources.
/// </summary>
internal static class MessageAgentLottieSettingsFactory
{
    /// <summary>
    /// Resolves CollectionLib's shared close animation with the requested fill color.
    /// </summary>
    public static LottieResourceSettings GetCloseSettings(Color fillColor) => LottieResourceResolver.GetSettings(
            CollectionListLottieKeys.Close,
            defaultResources: CollectionListDefaultResourceDictionary.Instance,
            defaultRestingProgress: 1d,
            defaultModifiers: new LottieModifierCollection().SetColor("Fill", fillColor),
            applicationResourceKeys: new[] { CollectionListLottieKeys.Close },
            defaultResourceKeys: new[] { CollectionListLottieKeys.Close },
            mergeModifiers: MessageAgentLottieModifiers.Merge);

    /// <summary>
    /// Applies a resolved animation resource to its XAML-declared Lottie view.
    /// </summary>
    public static void Apply(LottieView animation, LottieResourceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(settings);

        animation.Source = settings.Source;
        animation.Modifiers = settings.Modifiers as LottieModifierCollection ?? new LottieModifierCollection();
        animation.Progress = settings.RestingProgress ?? 0d;
        animation.Speed = settings.Speed ?? 1d;
        animation.IsReversed = settings.IsReversed ?? false;
    }
}