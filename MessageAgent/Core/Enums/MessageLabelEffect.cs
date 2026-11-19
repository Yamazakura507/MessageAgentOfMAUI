namespace MessageAgent.Core.Enums;

/// <summary>
/// Animation used when the message label appears or disappears.
/// </summary>
public enum MessageLabelEffect
{
    /// <summary>
    /// Opacity-only dissolve.
    /// </summary>
    Fade,

    /// <summary>
    /// Fade combined with a subtle scale transition.
    /// </summary>
    FadeScale,

    /// <summary>
    /// Fade combined with a short horizontal movement.
    /// </summary>
    Slide,

    /// <summary>
    /// Fade combined with outward movement, rotation and shrinking.
    /// </summary>
    Scatter
}
