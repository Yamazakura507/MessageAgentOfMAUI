using MessageAgent.Core.Enums.Loading;

namespace MessageAgent.Core.Models.Loading;

/// <summary>
/// Contains the outcome and measured duration of a loading operation.
/// </summary>
public sealed record LoadingResult(LoadingStatus Status, TimeSpan Elapsed, Exception? Exception = null)
{
    /// <summary>
    /// Gets whether all actions completed successfully.
    /// </summary>
    public bool IsSuccess => Status == LoadingStatus.Completed;
}