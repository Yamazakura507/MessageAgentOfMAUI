using LottieAgent.Models;
using LottieAgent.Models.Abstract;

namespace MessageAgent.Services.Extensions;

/// <summary>
/// Renderer-specific modifier composition used when resolving MessageAgent animations.
/// ResourcesLib remains independent of the Lottie renderer and receives this behavior as a callback.
/// </summary>
internal static class MessageAgentLottieModifiers
{
    /// <summary>
    /// Combines modifier collections in fallback, dictionary-default and application-override order.
    /// </summary>
    /// <param name="defaults">Lower-priority modifier collection.</param>
    /// <param name="custom">Higher-priority modifier collection.</param>
    /// <returns>A new combined collection, or null when both inputs are null.</returns>
    public static object? Merge(object? defaults, object? custom) => Combine(defaults as LottieModifierCollection, custom as LottieModifierCollection);

    private static LottieModifierCollection? Combine(LottieModifierCollection? defaults, LottieModifierCollection? custom)
    {
        if (defaults is null && custom is null) return null;

        LottieModifierCollection combined = new();

        if (defaults is not null)
        {
            foreach (LottieModifier modifier in defaults) combined.Add(modifier);
        }

        if (custom is not null)
        {
            foreach (LottieModifier modifier in custom) combined.Add(modifier);
        }

        return combined;
    }
}
