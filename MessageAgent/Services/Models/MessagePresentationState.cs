namespace MessageAgent.Services.Models;

public sealed record MessagePresentationState(
    IReadOnlyList<StatusPresentationItem> VisibleStatuses,
    bool HasStatusOverflow,
    bool HasDetails,
    bool ShouldLoopNotification);
