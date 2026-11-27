namespace MessageAgent.Core.Enums.Loading;

/// <summary>
/// Describes the state of an individual loading action.
/// </summary>
public enum LoadingActionStatus
{
    /// <summary>
    /// The action has not started.
    /// </summary>
    Pending,
    /// <summary>
    /// The action is currently executing.
    /// </summary>
    Running,
    /// <summary>
    /// The action completed successfully.
    /// </summary>
    Completed,
    /// <summary>
    /// The action failed.
    /// </summary>
    Failed,
    /// <summary>
    /// The action was cancelled by cancellation of the loading operation.
    /// </summary>
    Cancelled
}
