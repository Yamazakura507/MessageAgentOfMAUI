using LottieAgent.Models;
using MessageAgent.Resources.ExtensionResources;

namespace MessageAgent.Services.Extensions;

/// <summary>
/// Resolves a MessageAgent Lottie animation and its application-overridable settings.
/// </summary>
public static class MessageAgentLotties
{
    /// <summary>
    /// Gets an animation source, preferring an application resource override.
    /// The resource can be a source string (legacy form) or a <see cref="MessageAgentLottieSettings"/> object.
    /// </summary>
    public static string Get(string resourceKey, string defaultSource) => resourceKey.GetSettings(defaultSource).Source!;

    /// <summary>
    /// Resolves the complete animation settings. Unspecified properties inherit the supplied defaults.
    /// </summary>
    /// <param name="resourceKey">The key from <see cref="MessageAgentLottieKeys"/>.</param>
    /// <param name="defaultSource">The packaged fallback animation source.</param>
    /// <param name="defaultRestingProgress">The normalized idle position when no override is supplied.</param>
    /// <param name="defaultModifiers">The modifiers applied before any application-provided modifiers.</param>
    /// <param name="defaultSpeed">The fallback playback speed.</param>
    /// <param name="defaultIsReversed">The fallback playback direction.</param>
    /// <returns>A complete settings object suitable for applying to a LottieView.</returns>
    public static MessageAgentLottieSettings GetSettings(
        this string resourceKey,
        string defaultSource,
        double defaultRestingProgress = 0d,
        LottieModifierCollection? defaultModifiers = null,
        double defaultSpeed = 1d,
        bool defaultIsReversed = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultSource);

        if (!double.IsFinite(defaultRestingProgress) || defaultRestingProgress is < 0d or > 1d) throw new ArgumentOutOfRangeException(nameof(defaultRestingProgress));
        if (!double.IsFinite(defaultSpeed) || defaultSpeed <= 0d) throw new ArgumentOutOfRangeException(nameof(defaultSpeed));

        object? resourceValue = null;
        ResourceDictionary? resources = Application.Current?.Resources;

        if (resources is not null) resources.TryFindResource(resourceKey, out resourceValue);

        MessageAgentLottieSettings? configured = resourceValue as MessageAgentLottieSettings;
        string source = resourceValue is string sourceOverride && !string.IsNullOrWhiteSpace(sourceOverride)
            ? sourceOverride
            : !string.IsNullOrWhiteSpace(configured?.Source)
                ? configured.Source!
                : defaultSource;

        return new MessageAgentLottieSettings
        {
            Source = source,
            Modifiers = CombineModifiers(defaultModifiers, configured?.Modifiers),
            RestingProgress = configured?.RestingProgress ?? defaultRestingProgress,
            Speed = configured?.Speed ?? defaultSpeed,
            IsReversed = configured?.IsReversed ?? defaultIsReversed
        };
    }

    private static LottieModifierCollection? CombineModifiers(LottieModifierCollection? defaults, LottieModifierCollection? custom)
    {
        if (defaults is null && custom is null) return null;

        LottieModifierCollection combined = new();

        if (defaults is not null)
        {
            foreach (var modifier in defaults) combined.Add(modifier);
        }

        if (custom is not null)
        {
            foreach (var modifier in custom) combined.Add(modifier);
        }

        return combined;
    }
}
