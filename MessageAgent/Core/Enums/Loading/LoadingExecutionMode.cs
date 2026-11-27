namespace MessageAgent.Core.Enums.Loading;

/// <summary>
/// Defines how a set of loading actions is scheduled.
/// </summary>
public enum LoadingExecutionMode
{
    /// <summary>
    /// Runs all actions concurrently; individual failures do not cancel the other actions.
    /// </summary>
    Parallel,
    /// <summary>
    /// Runs actions one after another in their supplied order.
    /// </summary>
    Sequential
}