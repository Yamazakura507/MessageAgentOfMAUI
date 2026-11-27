namespace MessageAgent.Core.Enums.Loading;

/// <summary>
/// Defines the ordering used by the visible and expanded loading-task lists.
/// </summary>
public enum LoadingProgressSortMode
{
    /// <summary>
    /// Preserves the original action order.
    /// </summary>
    None,

    /// <summary>
    /// Shows successful actions, then failed actions, then unfinished actions.
    /// </summary>
    Access,

    /// <summary>
    /// Shows failed actions, then successful actions, then unfinished actions.
    /// </summary>
    Error,

    /// <summary>
    /// Shows unfinished actions ordered by progress, then successful actions.
    /// </summary>
    Process
}
