namespace MessageAgent.Services.Models;

/// <summary>
/// Immutable result of evaluating whether a message title, status badges and details
/// can be presented in the current compact layout.
/// </summary>
public sealed record MessagePresentationState
{
    /// <summary>
    /// Gets the statuses selected for display on the compact label.
    /// </summary>
    public IReadOnlyList<StatusPresentationItem> VisibleStatuses { get; init; }

    /// <summary>
    /// Gets whether one or more statuses must be shown in the overflow popup.
    /// </summary>
    public bool HasStatusOverflow { get; init; }

    /// <summary>
    /// Gets whether the message contains content that can be opened in the details dialog.
    /// </summary>
    public bool HasDetails { get; init; }

    /// <summary>
    /// Gets whether the notification animation should loop in the current state.
    /// </summary>
    public bool ShouldLoopNotification { get; init; }

    /// <summary>
    /// Initializes a complete immutable message presentation result.
    /// </summary>
    /// <param name="visibleStatuses">The statuses visible on the compact label.</param>
    /// <param name="hasStatusOverflow">Whether there are additional statuses.</param>
    /// <param name="hasDetails">Whether a details dialog is available.</param>
    /// <param name="shouldLoopNotification">Whether the notification animation should loop.</param>
    public MessagePresentationState(
        IReadOnlyList<StatusPresentationItem> visibleStatuses,
        bool hasStatusOverflow,
        bool hasDetails,
        bool shouldLoopNotification)
    {
        VisibleStatuses = visibleStatuses ?? throw new ArgumentNullException(nameof(visibleStatuses));
        HasStatusOverflow = hasStatusOverflow;
        HasDetails = hasDetails;
        ShouldLoopNotification = shouldLoopNotification;
    }
}
