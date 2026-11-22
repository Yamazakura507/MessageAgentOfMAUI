using LottieAgent.Models;
using MessageAgent.Core.Models.VisualValues.Abstract;

namespace MessageAgent.Core.Models.VisualValues;

/// <summary>
/// Represents a Lottie animation visual value usable by statuses and links.
/// </summary>
public sealed record LottieVisualValue : VisualValue
{
    private double progress;

    /// <summary>
    /// Gets or sets the Lottie asset path or supported source identifier.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    public double Speed { get; set; } = 1d;

    /// <summary>
    /// Gets or sets the normalized initial animation position in the inclusive range [0, 1].
    /// Zero represents the first frame and one represents the final frame.
    /// </summary>
    public double Progress
    {
        get => progress;
        set
        {
            if (!double.IsFinite(value) || value is < 0d or > 1d) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress must be between 0 and 1.");

            progress = value;
        }
    }

    /// <summary>
    /// Gets or sets whether playback runs from the final frame to the first frame.
    /// </summary>
    public bool IsReverse { get; set; }

    /// <summary>
    /// Gets the named Lottie modifier collection applied before rendering.
    /// </summary>
    public LottieModifierCollection Modifiers { get; set; } = new();

    /// <summary>
    /// Initializes an empty Lottie visual value for XAML.
    /// </summary>
    public LottieVisualValue() { }

    /// <summary>
    /// Initializes a Lottie visual value with optional playback and modifier settings.
    /// </summary>
    /// <param name="source">Lottie asset path or supported source identifier.</param>
    /// <param name="alternativeText">Accessible description of the animation.</param>
    /// <param name="speed">Playback speed multiplier.</param>
    /// <param name="progress">Normalized initial animation position in the range [0, 1].</param>
    /// <param name="isReverse">Whether playback runs from the final frame to the first frame.</param>
    /// <param name="modifiers">Optional named modifier collection.</param>
    public LottieVisualValue(
        string source,
        string? alternativeText = null,
        double speed = 1d,
        double progress = 0d,
        bool isReverse = false,
        LottieModifierCollection? modifiers = null)
    {
        Source = source;
        AlternativeText = alternativeText;
        Speed = speed;
        Progress = progress;
        IsReverse = isReverse;
        Modifiers = modifiers ?? new ();
    }
}
