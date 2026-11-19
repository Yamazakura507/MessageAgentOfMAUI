using MessageAgent.Core.Enums;
using MessageAgent.Services.Models;

namespace MessageAgent.Services.Extensions;

/// <summary>
/// Creates coordinated dialog and status colors with explicit contrast targets.
/// </summary>
public static class MessageColorPalette
{
    public static Color GetDialogSurface(this MessageType type, AppTheme? theme = null)
    {
        bool dark = theme.IsDark();
        Color neutral = Color.FromRgb(0.985f, 0.982f, 0.973f);
        double tint = 0.105;

        if (dark)
        {
            neutral = Color.FromRgb(0.105f, 0.12f, 0.145f);
            tint = 0.24;
        }
        
        return Mix(neutral, type.GetAccentColor(), tint);
    }

    public static Color GetDialogForeground(this MessageType type, AppTheme? theme = null)
    {
        bool dark = theme.IsDark();
        Color neutral = dark ? Color.FromRgb(0.96f, 0.965f, 0.98f) : Color.FromRgb(0.105f, 0.11f, 0.13f);

        return EnsureContrast(neutral, type.GetDialogSurface(theme), 7.0);
    }

    public static Color GetDialogSecondaryForeground(this MessageType type, AppTheme? theme = null)
    {
        bool dark = theme.IsDark();
        Color neutral = dark ? Color.FromRgb(0.82f, 0.84f, 0.87f) : Color.FromRgb(0.30f, 0.32f, 0.36f);

        return EnsureContrast(neutral, type.GetDialogSurface(theme), 4.5);
    }

    public static Color GetDialogAccent(this MessageType type, AppTheme? theme = null) => EnsureContrast(type.GetAccentColor(), type.GetDialogSurface(theme), 3.0);

    public static Color GetAccentContent(this MessageType type) => EnsureContrast(Color.FromRgb(1, 1, 1), type.GetAccentColor(), 4.5);

    public static Color GetNeutralStatusTextColor(AppTheme? theme = null)
    {
        bool dark = theme.IsDark();

        return dark ? Color.FromRgb(0.93f, 0.94f, 0.96f) : Color.FromRgb(0.14f, 0.15f, 0.17f);
    }

    public static StatusVisualPalette CreateStatusPalette(this Color sourceColor, AppTheme? theme = null)
    {
        bool dark = theme.IsDark();
        Color neutral = Color.FromRgb(0.995f, 0.992f, 0.985f);
        double tint = 0.105;

        if (dark)
        {
            neutral = Color.FromRgb(0.13f, 0.145f, 0.17f);
            tint = 0.26;
        }
        
        Color background = Mix(neutral, sourceColor, tint);
        Color border = EnsureContrast(sourceColor, background, 3.0);
        Color text = EnsureContrast(sourceColor, background, 4.5);

        return new StatusVisualPalette(border, background, text);
    }

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

    internal static Color GetAccentColor(this MessageType type) => type switch
    {
        MessageType.Error => Color.FromRgba(179, 15, 15, 255),
        MessageType.Alert => Color.FromRgba(184, 134, 11, 255),
        MessageType.Info => Color.FromRgba(0, 120, 194, 255),
        _ => Color.FromRgba(0, 120, 194, 255)
    };

    internal static Color GetBackgroundColor(this MessageType type) => type switch
    {
        MessageType.Error => Color.FromRgba(179, 15, 15, 22),
        MessageType.Alert => Color.FromRgba(184, 134, 11, 22),
        MessageType.Info => Color.FromRgba(0, 120, 194, 22),
        _ => Color.FromRgba(0, 120, 194, 22)
    };

    private static bool IsDark(this AppTheme? theme)
    {
        AppTheme actual = theme ?? Application.Current?.RequestedTheme ?? AppTheme.Light;

        return actual == AppTheme.Dark;
    }

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
