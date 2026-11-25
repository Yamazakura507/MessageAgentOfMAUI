using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Resources.ExtensionResources;
using MessageAgent.Services.Models;

namespace MessageAgent.Services.Extensions;

/// <summary>
/// Creates coordinated dialog and status colors with explicit contrast targets.
/// </summary>
public static class MessageColorPalette
{
    /// <summary>
    /// Gets the theme-aware dialog surface derived from a message type.
    /// </summary>
    /// <param name="type">The message type defining the accent and optional overrides.</param>
    /// <param name="theme">The theme to use, or the current application theme when omitted.</param>
    /// <returns>A dialog surface color.</returns>
    public static Color GetDialogSurface(this MessageType type, AppTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.DialogSurfaceColor is Color customSurface) return customSurface;

        bool dark = theme.IsDark();
        Color neutral = MessageAgentResources.GetColor(dark
            ? MessageAgentResources.DialogSurfaceDarkKey
            : MessageAgentResources.DialogSurfaceLightKey);
        double tint = MessageAgentResources.GetDouble(dark
            ? MessageAgentResources.DialogSurfaceTintDarkKey
            : MessageAgentResources.DialogSurfaceTintLightKey);

        return Mix(neutral, type.GetAccentColor(), tint);
    }

    /// <summary>
    /// Gets a dialog foreground color with sufficient contrast against its surface.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="theme">The theme to use, or the current application theme.</param>
    /// <returns>A contrast-adjusted foreground color.</returns>
    public static Color GetDialogForeground(this MessageType type, AppTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        Color surface = type.GetDialogSurface(theme);

        if (type.DialogForegroundColor is Color custom) return EnsureContrast(custom, surface, 4.5);

        Color neutral = MessageAgentResources.GetColor(theme.IsDark()
            ? MessageAgentResources.DialogForegroundDarkKey
            : MessageAgentResources.DialogForegroundLightKey);

        return EnsureContrast(neutral, surface, 7.0);
    }

    /// <summary>
    /// Gets the contrast-adjusted secondary dialog text color.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="theme">The theme to use, or the current application theme.</param>
    /// <returns>A secondary foreground color.</returns>
    public static Color GetDialogSecondaryForeground(this MessageType type, AppTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        Color surface = type.GetDialogSurface(theme);

        if (type.DialogSecondaryForegroundColor is Color custom) return EnsureContrast(custom, surface, 4.5);

        Color neutral = MessageAgentResources.GetColor(theme.IsDark()
            ? MessageAgentResources.DialogSecondaryForegroundDarkKey
            : MessageAgentResources.DialogSecondaryForegroundLightKey);

        return EnsureContrast(neutral, surface, 4.5);
    }

    /// <summary>
    /// Gets an accent color adjusted for visibility on the dialog surface.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="theme">The theme to use, or the current application theme.</param>
    /// <returns>A contrast-adjusted accent color.</returns>
    public static Color GetDialogAccent(this MessageType type, AppTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        return EnsureContrast(type.DialogAccentColor ?? type.GetAccentColor(), type.GetDialogSurface(theme), 3.0);
    }

    /// <summary>
    /// Gets a foreground color suitable for controls filled with the message accent.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>A contrast-adjusted content color.</returns>
    public static Color GetAccentContent(this MessageType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return EnsureContrast(
            type.AccentContentColor ?? MessageAgentResources.GetColor(MessageAgentResources.AccentContentFallbackKey),
            type.GetAccentColor(),
            4.5);
    }

    /// <summary>
    /// Gets the neutral status text color for a light or dark theme.
    /// </summary>
    /// <param name="theme">The theme to use, or the current application theme.</param>
    /// <returns>A neutral text color.</returns>
    public static Color GetNeutralStatusTextColor(AppTheme? theme = null)
    {
        bool dark = theme.IsDark();

        return MessageAgentResources.GetColor(dark
            ? MessageAgentResources.StatusTextDarkKey
            : MessageAgentResources.StatusTextLightKey);
    }

    /// <summary>
    /// Creates coordinated border, background and text colors for a status badge.
    /// </summary>
    /// <param name="sourceColor">The base accent color.</param>
    /// <param name="theme">The theme to use, or the current application theme.</param>
    /// <returns>A complete status visual palette.</returns>
    public static StatusVisualPalette CreateStatusPalette(this Color sourceColor, AppTheme? theme = null)
    {
        bool dark = theme.IsDark();
        Color neutral = MessageAgentResources.GetColor(dark
            ? MessageAgentResources.StatusSurfaceDarkKey
            : MessageAgentResources.StatusSurfaceLightKey);
        double tint = MessageAgentResources.GetDouble(dark
            ? MessageAgentResources.StatusSurfaceTintDarkKey
            : MessageAgentResources.StatusSurfaceTintLightKey);
        Color background = Mix(neutral, sourceColor, tint);
        Color border = EnsureContrast(sourceColor, background, 3.0);
        Color text = EnsureContrast(sourceColor, background, 4.5);

        return new StatusVisualPalette(border, background, text);
    }

    /// <summary>
    /// Adjusts a foreground color to meet the requested contrast ratio.
    /// </summary>
    /// <param name="foreground">The original foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <param name="minimumRatio">The minimum WCAG contrast ratio.</param>
    /// <returns>The original color when sufficient, otherwise a blended contrast-safe color.</returns>
    public static Color EnsureContrast(Color foreground, Color background, double minimumRatio)
    {
        foreground = foreground.Opaque();
        background = background.Opaque();

        if (ContrastRatio(foreground, background) >= minimumRatio) return foreground;

        Color target = ContrastRatio(Colors.Black, background) >= ContrastRatio(Colors.White, background) ? Colors.Black : Colors.White;
        double low = 0;
        double high = 1;

        for (int i = 0; i < 24; i++)
        {
            double middle = (low + high) / 2;
            Color candidate = Mix(foreground, target, middle);

            if (ContrastRatio(candidate, background) >= minimumRatio) high = middle;
            else low = middle;
        }

        return Mix(foreground, target, high);
    }

    /// <summary>
    /// Gets the primary accent color of the message type, falling back to Info.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>The accent color.</returns>
    public static Color GetAccentColor(this MessageType type) => type?.AccentColor ?? MessageType.Info.AccentColor;

    /// <summary>
    /// Gets the configured label background or a translucent accent fallback.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>The label background color.</returns>
    public static Color GetBackgroundColor(this MessageType type)
    {
        float fallbackOpacity = (float)MessageAgentResources.GetDouble(MessageAgentResources.AccentBackgroundOpacityKey);

        return type is null
            ? MessageType.Info.BackgroundColor ?? MessageType.Info.AccentColor.WithAlpha(fallbackOpacity)
            : type.BackgroundColor ?? type.GetAccentColor().WithAlpha(fallbackOpacity);

    }

    private static bool IsDark(this AppTheme? theme) => (theme ?? Application.Current?.RequestedTheme ?? AppTheme.Light) == AppTheme.Dark;

    private static Color Opaque(this Color color) => Color.FromRgb(color.Red, color.Green, color.Blue);

    private static Color Mix(Color first, Color second, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);

        return Color.FromRgb(
            (float)(first.Red + (second.Red - first.Red) * amount),
            (float)(first.Green + (second.Green - first.Green) * amount),
            (float)(first.Blue + (second.Blue - first.Blue) * amount));
    }

    private static double ContrastRatio(Color first, Color second)
    {
        double a = first.RelativeLuminance();
        double b = second.RelativeLuminance();
        double lighter = Math.Max(a, b);
        double darker = Math.Min(a, b);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(this Color color) => 0.2126 * Linearize(color.Red) + 0.7152 * Linearize(color.Green) + 0.0722 * Linearize(color.Blue);

    private static double Linearize(this double channel) => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
