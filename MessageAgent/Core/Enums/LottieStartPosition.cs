namespace MessageAgent.Core.Enums;

/// <summary>
/// Initial edge and playback direction of a Lottie status animation.
/// </summary>
public enum LottieStartPosition
{
    /// <summary>
    /// Begin at the first frame and play towards the last frame.
    /// </summary>
    Start,

    /// <summary>
    /// Begin at the last frame and play backwards towards the first frame.
    /// </summary>
    End
}
