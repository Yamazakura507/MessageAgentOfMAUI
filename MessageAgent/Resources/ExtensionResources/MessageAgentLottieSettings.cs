using LottieAgent.Models;

namespace MessageAgent.Resources.ExtensionResources;

/// <summary>
/// Application override settings for one of MessageAgent's built-in Lottie animations.
/// Set only the properties that should differ from the library defaults.
/// </summary>
public sealed class MessageAgentLottieSettings
{
    private double? restingProgress;
    private double? speed;

    /// <summary>
    /// Gets or sets the replacement Lottie asset path. When omitted, the library default is retained.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the modifiers applied to the animation. When omitted, the library defaults are retained.
    /// Custom modifiers are applied after the default modifiers.
    /// </summary>
    public LottieModifierCollection? Modifiers { get; set; }

    /// <summary>
    /// Gets or sets the normalized position to restore whenever the animation is idle.
    /// The value must be between zero and one.
    /// </summary>
    public double? RestingProgress
    {
        get => restingProgress;
        set
        {
            if (value is double progress && (!double.IsFinite(progress) || progress is < 0d or > 1d))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Resting progress must be between 0 and 1.");

            restingProgress = value;
        }
    }

    /// <summary>
    /// Gets or sets the playback speed multiplier. The value must be greater than zero.
    /// </summary>
    public double? Speed
    {
        get => speed;
        set
        {
            if (value is double multiplier && (!double.IsFinite(multiplier) || multiplier <= 0d))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Animation speed must be greater than zero.");

            speed = value;
        }
    }

    /// <summary>
    /// Gets or sets whether playback runs from the final frame to the first frame.
    /// </summary>
    public bool? IsReversed { get; set; }
}
