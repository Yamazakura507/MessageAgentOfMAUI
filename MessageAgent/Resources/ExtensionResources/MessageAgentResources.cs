using ResourcesLib.Extensions;

namespace MessageAgent.Resources.ExtensionResources;

/// <summary>
/// Bridges application-overridable XAML defaults to code that needs typed resource values.
/// </summary>
internal static class MessageAgentResources
{
    internal const string DefaultNotificationLottieKey = "MessageAgent.Lotties.DefaultNotification";

    internal const string AlertAccentKey = "MessageAgent.Colors.Type.AlertAccent";
    internal const string AlertBackgroundKey = "MessageAgent.Colors.Type.AlertBackground";
    internal const string ErrorAccentKey = "MessageAgent.Colors.Type.ErrorAccent";
    internal const string ErrorBackgroundKey = "MessageAgent.Colors.Type.ErrorBackground";
    internal const string InfoAccentKey = "MessageAgent.Colors.Type.InfoAccent";
    internal const string InfoBackgroundKey = "MessageAgent.Colors.Type.InfoBackground";
    internal const string AccentContentFallbackKey = "MessageAgent.Colors.Type.AccentContentFallback";
    internal const string RandomAccentSaturationKey = "MessageAgent.Palette.RandomAccentSaturation";
    internal const string RandomAccentLightnessKey = "MessageAgent.Palette.RandomAccentLightness";
    internal const string DialogSurfaceTintLightKey = "MessageAgent.Palette.DialogSurfaceTintLight";
    internal const string DialogSurfaceTintDarkKey = "MessageAgent.Palette.DialogSurfaceTintDark";
    internal const string StatusSurfaceTintLightKey = "MessageAgent.Palette.StatusSurfaceTintLight";
    internal const string StatusSurfaceTintDarkKey = "MessageAgent.Palette.StatusSurfaceTintDark";
    internal const string AccentBackgroundOpacityKey = "MessageAgent.Palette.AccentBackgroundOpacity";
    internal const string TypeCountBadgeOpacityKey = "MessageAgent.Palette.TypeCountBadgeOpacity";
    internal const string TypeChipUnselectedStrokeOpacityKey = "MessageAgent.Palette.TypeChipUnselectedStrokeOpacity";
    internal const string TypeChipSelectedBackgroundOpacityKey = "MessageAgent.Palette.TypeChipSelectedBackgroundOpacity";
    internal const string TypeChipSelectedStrokeThicknessKey = "MessageAgent.Palette.TypeChipSelectedStrokeThickness";
    internal const string TypeChipUnselectedStrokeThicknessKey = "MessageAgent.Palette.TypeChipUnselectedStrokeThickness";
    internal const string StatusActionFirstCircleHueOffsetKey = "MessageAgent.Palette.StatusAction.FirstCircleHueOffset";
    internal const string StatusActionFirstCircleSaturationOffsetKey = "MessageAgent.Palette.StatusAction.FirstCircleSaturationOffset";
    internal const string StatusActionFirstCircleLightnessOffsetKey = "MessageAgent.Palette.StatusAction.FirstCircleLightnessOffset";
    internal const string StatusActionThirdCircleHueOffsetKey = "MessageAgent.Palette.StatusAction.ThirdCircleHueOffset";
    internal const string StatusActionThirdCircleSaturationOffsetKey = "MessageAgent.Palette.StatusAction.ThirdCircleSaturationOffset";
    internal const string StatusActionThirdCircleLightnessOffsetKey = "MessageAgent.Palette.StatusAction.ThirdCircleLightnessOffset";

    internal const string DialogSurfaceLightKey = "MessageAgent.Colors.DialogSurfaceLight";
    internal const string DialogSurfaceDarkKey = "MessageAgent.Colors.DialogSurfaceDark";
    internal const string DialogForegroundLightKey = "MessageAgent.Colors.DialogForegroundLight";
    internal const string DialogForegroundDarkKey = "MessageAgent.Colors.DialogForegroundDark";
    internal const string DialogSecondaryForegroundLightKey = "MessageAgent.Colors.DialogSecondaryForegroundLight";
    internal const string DialogSecondaryForegroundDarkKey = "MessageAgent.Colors.DialogSecondaryForegroundDark";
    internal const string StatusTextLightKey = "MessageAgent.Colors.StatusTextLight";
    internal const string StatusTextDarkKey = "MessageAgent.Colors.StatusTextDark";
    internal const string StatusSurfaceLightKey = "MessageAgent.Colors.StatusSurfaceLight";
    internal const string StatusSurfaceDarkKey = "MessageAgent.Colors.StatusSurfaceDark";
    internal const string StatusBorderFallbackKey = "MessageAgent.Colors.StatusBorderFallback";
    internal const string SwipeDeleteSurfaceLightKey = "MessageAgent.Colors.SwipeDeleteSurfaceLight";
    internal const string SwipeDeleteSurfaceDarkKey = "MessageAgent.Colors.SwipeDeleteSurfaceDark";
    internal const string StatusOverflowSurfaceLightKey = "MessageAgent.Colors.StatusOverflowSurfaceLight";
    internal const string StatusOverflowSurfaceDarkKey = "MessageAgent.Colors.StatusOverflowSurfaceDark";
    internal const string StatusOverflowForegroundLightKey = "MessageAgent.Colors.StatusOverflowForegroundLight";
    internal const string StatusOverflowForegroundDarkKey = "MessageAgent.Colors.StatusOverflowForegroundDark";
    internal const string TypeFiltersSurfaceLightKey = "MessageAgent.Colors.TypeFiltersSurfaceLight";
    internal const string TypeFiltersSurfaceDarkKey = "MessageAgent.Colors.TypeFiltersSurfaceDark";

    /// <summary>
    /// Resolves an application override first, then the built-in XAML resource.
    /// </summary>
    internal static Color GetColor(string key) => key.GetValue<Color>();

    /// <summary>
    /// Creates a random accent using the configurable saturation and lightness defaults.
    /// </summary>
    internal static Color CreateRandomAccentColor() => Color.FromHsla(
        Random.Shared.NextDouble(),
        GetDouble(RandomAccentSaturationKey),
        GetDouble(RandomAccentLightnessKey));

    /// <summary>
    /// Resolves a numeric application override first, then the built-in XAML resource.
    /// </summary>
    internal static double GetDouble(string key) => key.GetValue<double>();

    /// <summary>
    /// Resolves an application override first, then the built-in XAML resource.
    /// </summary>
    internal static string GetString(string key) => key.GetValue<string>();

    private static T GetValue<T>(this string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (ResourceDictionaryExtensions.TryFindApplicationResource(key, out object? applicationValue)
            && applicationValue is T applicationVal) return applicationVal;

        if (DefaultResourceDictionary.Instance.TryFindResource(key, out object? defaultValue)
            && defaultValue is T defaultVal) return defaultVal;

        throw new KeyNotFoundException($"MessageAgent {TypeStr<T>()} resource '{key}' was not found.");
    }

    private static string TypeStr<T>()
    {
        return default(T) switch
        {
            double => "numeric",
            Color => "color",
            _ => typeof(T).Name.ToLowerInvariant()
        };
    }
}
