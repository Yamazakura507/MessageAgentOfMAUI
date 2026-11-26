namespace MessageAgent.Core.Enums.Loading;

/// <summary>
/// Describes the terminal or current state of a loading operation.
/// </summary>
public enum LoadingStatus
{
    /// <summary>
    /// The operation has not started.
    /// </summary>
    Pending,
    /// <summary>
    /// One or more actions are currently running.
    /// </summary>
    Running,
    /// <summary>
    /// All actions completed successfully.
    /// </summary>
    Completed,
    /// <summary>
    /// The operation was cancelled.
    /// </summary>
    Cancelled,
    /// <summary>
    /// An action failed.
    /// </summary>
    Failed
}