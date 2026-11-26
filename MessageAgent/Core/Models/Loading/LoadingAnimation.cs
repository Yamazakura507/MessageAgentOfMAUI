using LottieAgent.Controls;
using LottieAgent.Models;
using MessageAgent.Core.Models.VisualValues;

namespace MessageAgent.Core.Models.Loading;

/// <summary>
/// Describes one animation that can be selected by a loading view.
/// </summary>
public sealed class LoadingAnimation
{
    /// <summary>
    /// Gets the package asset path, when the animation is asset-backed.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the raw Lottie JSON, when the animation is supplied inline.
    /// </summary>
    public string? RawJson { get; }

    /// <summary>
    /// Gets the modifiers applied by the Lottie view.
    /// </summary>
    public LottieModifierCollection Modifiers { get; }

    /// <summary>
    /// Gets an already configured Lottie view. Its source and modifiers are preserved.
    /// </summary>
    public LottieView? View { get; }

    /// <summary>
    /// Gets the accent used by the loading surface and dialog.
    /// </summary>
    public Color? AccentColor { get; }

    /// <summary>
    /// Gets the animation playback speed multiplier.
    /// </summary>
    public double Speed { get; }

    /// <summary>
    /// Gets the normalized starting progress.
    /// </summary>
    public double Progress { get; }

    /// <summary>
    /// Gets whether the animation should play in reverse.
    /// </summary>
    public bool IsReversed { get; }

    /// <summary>
    /// Creates an asset-backed animation.
    /// </summary>
    /// <param name="source">The Lottie asset path.</param>
    /// <param name="modifiers">Optional typed Lottie modifiers.</param>
    /// <param name="accentColor">Optional accent color used when inferring a dialog palette.</param>
    /// <param name="speed">The playback speed multiplier.</param>
    /// <param name="progress">The normalized starting progress.</param>
    /// <param name="isReversed">Whether playback is reversed.</param>
    public LoadingAnimation(
        string source,
        LottieModifierCollection? modifiers = null,
        Color? accentColor = null,
        double speed = 1d,
        double progress = 0d,
        bool isReversed = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        ValidatePlayback(speed, progress);

        Source = source;
        Modifiers = modifiers ?? new();
        AccentColor = accentColor;
        Speed = speed;
        Progress = progress;
        IsReversed = isReversed;
    }

    /// <summary>
    /// Creates an animation from an existing visual-value model.
    /// </summary>
    /// <param name="value">The Lottie visual value.</param>
    /// <param name="accentColor">Optional accent color used when inferring a dialog palette.</param>
    public LoadingAnimation(LottieVisualValue value, Color? accentColor = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        ValidatePlayback(value.Speed, value.Progress);

        Source = value.Source;
        Modifiers = value.Modifiers;
        AccentColor = accentColor;
        Speed = value.Speed;
        Progress = value.Progress;
        IsReversed = value.IsReverse;
    }

    /// <summary>
    /// Uses an already configured Lottie view without reprocessing its modifiers.
    /// </summary>
    /// <param name="view">The configured Lottie view.</param>
    /// <param name="accentColor">Optional accent color used by the loading surface.</param>
    public LoadingAnimation(LottieView view, Color? accentColor = null)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
        Modifiers = new();
        AccentColor = accentColor;
        Speed = 1d;
        Progress = 0d;
    }

    private LoadingAnimation(string rawJson, LottieModifierCollection? modifiers, Color? accentColor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawJson);

        RawJson = rawJson;
        Modifiers = modifiers ?? new();
        AccentColor = accentColor;
        Speed = 1d;
        Progress = 0d;
    }

    /// <summary>
    /// Creates an animation from a raw JSON document. Modifiers are applied by the Lottie view once.
    /// </summary>
    /// <param name="json">The complete Lottie JSON document.</param>
    /// <param name="modifiers">Optional typed modifiers applied by the Lottie view.</param>
    /// <param name="accentColor">Optional accent color used when inferring a dialog palette.</param>
    public static LoadingAnimation FromRawJson(string json, LottieModifierCollection? modifiers = null, Color? accentColor = null)
        => new(json, modifiers, accentColor);

    private static void ValidatePlayback(double speed, double progress)
    {
        if (!double.IsFinite(speed) || speed <= 0d) throw new ArgumentOutOfRangeException(nameof(speed), "Animation speed must be finite and greater than zero.");
        if (!double.IsFinite(progress) || progress is < 0d or > 1d) throw new ArgumentOutOfRangeException(nameof(progress), "Animation progress must be between zero and one.");
    }
}
