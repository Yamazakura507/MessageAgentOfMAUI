using MessageAgent.Core.Enums.Message;

namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Runs the configured show and hide transition for a compact message label.
/// </summary>
public interface IMessageLabelAnimator
{
    /// <summary>
    /// Changes label visibility using the requested transition.
    /// </summary>
    /// <param name="target">The border control representing the label.</param>
    /// <param name="visible">Whether the label should be visible.</param>
    /// <param name="effect">The transition effect.</param>
    /// <param name="animated">Whether to run the animated transition.</param>
    /// <returns>A task that completes after the transition.</returns>
    Task SetVisibilityAsync(Border target, bool visible, MessageLabelEffect effect, bool animated);
}
