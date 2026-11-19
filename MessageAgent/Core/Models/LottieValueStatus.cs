using MessageAgent.Core.Enums;

namespace MessageAgent.Core.Models;

/// <summary>
/// Displays a Lottie animation. StartPosition defines only the idle frame;
/// IsReverse defines the playback direction while the status is active.
/// </summary>
public sealed record LottieValueStatus : ValueStatus
{
    public string Source { get; set; } = string.Empty;
    public string? AlternativeText { get; set; }
    public double Speed { get; set; } = 1d;

    /// <summary>
    /// Frame displayed while the animation is idle.
    /// </summary>
    public LottieStartPosition StartPosition { get; set; } = LottieStartPosition.Start;

    /// <summary>
    /// Plays from the last frame to the first frame when true.
    /// </summary>
    public bool IsReverse { get; set; }

    public LottieValueStatus() { }

    public LottieValueStatus(
        string source,
        string? alternativeText = null,
        double speed = 1d,
        LottieStartPosition startPosition = LottieStartPosition.Start,
        bool isReverse = false)
    {
        Source = source;
        AlternativeText = alternativeText;
        Speed = speed;
        StartPosition = startPosition;
        IsReverse = isReverse;
    }
}
