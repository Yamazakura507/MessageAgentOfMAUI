namespace MessageAgent.Core.Models.Loading;

/// <summary>
/// Represents one cancellable asynchronous operation displayed by a loading surface.
/// </summary>
public sealed class LoadingAction
{
    /// <summary>
    /// Gets the action delegate.
    /// </summary>
    public Func<CancellationToken, Task> Execute { get; }

    /// <summary>
    /// Gets the optional short description shown while the action set runs.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Creates an action that receives the operation cancellation token.
    /// </summary>
    /// <param name="execute">The asynchronous operation.</param>
    /// <param name="description">Optional user-facing description.</param>
    public LoadingAction(Func<CancellationToken, Task> execute, string? description = null)
    {
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        Description = description;
    }

    /// <summary>
    /// Creates an action that does not need to observe cancellation.
    /// </summary>
    /// <param name="execute">The asynchronous operation.</param>
    /// <param name="description">Optional user-facing description.</param>
    public LoadingAction(Func<Task> execute, string? description = null) : this(_ => execute(), description)
    {
        ArgumentNullException.ThrowIfNull(execute);
    }

    /// <summary>
    /// Creates an action from synchronous work.
    /// </summary>
    /// <param name="execute">The synchronous operation.</param>
    /// <param name="description">Optional user-facing description.</param>
    public LoadingAction(Action execute, string? description = null) : this(_ => { execute(); return Task.CompletedTask; }, description)
    {
        ArgumentNullException.ThrowIfNull(execute);
    }
}