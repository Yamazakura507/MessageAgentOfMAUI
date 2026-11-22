using MessageAgent.Services.Models;

namespace MessageAgent.Services.Extensions;

/// <summary>
/// Builds the overflow action palette from its randomly selected outline color.
/// Circle shades retain the relative differences of the original Lottie artwork.
/// </summary>
internal static class StatusActionPaletteFactory
{
    /// <summary>
    /// Creates a coordinated action palette from a seed color.
    /// </summary>
    /// <param name="sourceColor">The source color used to derive the action palette.</param>
    /// <returns>A set of surface, foreground and animation colors.</returns>
    public static StatusActionPalette Create(this Color sourceColor)
    {
        StatusVisualPalette surface = sourceColor.CreateStatusPalette();

        return new StatusActionPalette(
            surface,
            surface.BorderColor.ApplyRelativeShade(-14.2687, -0.19208, 0.0196),
            surface.BorderColor,
            surface.BorderColor.ApplyRelativeShade(-2.4964, -0.055656, 0.2235));
    }

    private static Color ApplyRelativeShade(this Color baseColor, double hueOffset, double saturationOffset, double lightnessOffset)
    {
        (double hue, double saturation, double lightness) = baseColor.ToHsl();

        hue = (hue + hueOffset + 360) % 360;
        saturation = Math.Clamp(saturation + saturationOffset, 0, 1);
        lightness = Math.Clamp(lightness + lightnessOffset, 0, 1);

        return hue.FromHsl(saturation, lightness);
    }

    private static (double Hue, double Saturation, double Lightness) ToHsl(this Color color)
    {
        double red = color.Red;
        double green = color.Green;
        double blue = color.Blue;
        double maximum = Math.Max(red, Math.Max(green, blue));
        double minimum = Math.Min(red, Math.Min(green, blue));
        double delta = maximum - minimum;
        double lightness = (maximum + minimum) / 2;

        if (delta <= double.Epsilon) return (0, 0, lightness);

        double saturation = delta / (1 - Math.Abs(2 * lightness - 1));
        double hue = maximum switch
        {
            double value when Math.Abs(value - red) < double.Epsilon => 60 * (((green - blue) / delta) % 6),
            double value when Math.Abs(value - green) < double.Epsilon => 60 * ((blue - red) / delta + 2),
            _ => 60 * ((red - green) / delta + 4)
        };

        if (hue < 0) hue += 360;

        return (hue, saturation, lightness);
    }

    private static Color FromHsl(this double hue, double saturation, double lightness)
    {
        double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        double secondary = chroma * (1 - Math.Abs((hue / 60 % 2) - 1));
        double offset = lightness - chroma / 2;

        (double red, double green, double blue) = hue switch
        {
            < 60 => (chroma, secondary, 0d),
            < 120 => (secondary, chroma, 0d),
            < 180 => (0d, chroma, secondary),
            < 240 => (0d, secondary, chroma),
            < 300 => (secondary, 0d, chroma),
            _ => (chroma, 0d, secondary)
        };

        return Color.FromRgb(
            (float)Math.Clamp(red + offset, 0, 1),
            (float)Math.Clamp(green + offset, 0, 1),
            (float)Math.Clamp(blue + offset, 0, 1));
    }
}


