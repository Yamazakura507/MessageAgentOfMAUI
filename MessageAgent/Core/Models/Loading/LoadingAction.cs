namespace MessageAgent.Core.Models.Loading;

/// <summary>
/// Represents one cancellable asynchronous operation displayed by a loading surface.
/// </summary>
public sealed class LoadingAction
{
    /// <summary>
    /// Gets the cancellation-aware action delegate.
    /// </summary>
    public Func<CancellationToken, Task> Execute { get; }

    /// <summary>
    /// Gets the progress-aware delegate, when one was supplied.
    /// Report progress percentages in the inclusive range [0, 100].
    /// </summary>
    public Func<IProgress<double>, CancellationToken, Task>? ExecuteWithProgress { get; }

    /// <summary>
    /// Gets the optional short description shown while the action runs.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets an optional asynchronous handler invoked when the action fails.
    /// It is not invoked when the user cancels the loading operation.
    /// </summary>
    public Func<Exception, Task>? OnError { get; set; }

    /// <summary>
    /// Creates an action that receives the operation cancellation token.
    /// </summary>
    public LoadingAction(Func<CancellationToken, Task> execute, string? description = null) : this(execute, description, null)
    {
    }

    /// <summary>
    /// Creates a cancellation-aware action with an asynchronous error handler.
    /// </summary>
    public LoadingAction(
        Func<CancellationToken, Task> execute,
        string? description,
        Func<Exception, Task>? onError)
    {
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        Description = description;
        OnError = onError;
    }

    /// <summary>
    /// Creates an action that can report progress and observe cancellation.
    /// </summary>
    public LoadingAction(
        Func<IProgress<double>, CancellationToken, Task> execute,
        string? description = null,
        Func<Exception, Task>? onError = null)
    {
        Func<IProgress<double>, CancellationToken, Task> progressExecute =
            execute ?? throw new ArgumentNullException(nameof(execute));

        ExecuteWithProgress = progressExecute;
        Execute = cancellationToken => progressExecute(new Progress<double>(_ => { }), cancellationToken);
        Description = description;
        OnError = onError;
    }

    /// <summary>
    /// Creates an action that does not need to observe cancellation.
    /// </summary>
    public LoadingAction(Func<Task> execute, string? description = null) : this(execute, description, null)
    {
    }

    /// <summary>
    /// Creates a non-cancellable action with an asynchronous error handler.
    /// </summary>
    public LoadingAction(Func<Task> execute, string? description, Func<Exception, Task>? onError) : this(_ => execute(), description, onError)
    {
        ArgumentNullException.ThrowIfNull(execute);
    }

    /// <summary>
    /// Creates an action from synchronous work.
    /// </summary>
    public LoadingAction(Action execute, string? description = null) : this(execute, description, null)
    {
    }

    /// <summary>
    /// Creates synchronous work with an asynchronous error handler.
    /// </summary>
    public LoadingAction(Action execute, string? description, Func<Exception, Task>? onError) 
        : this(_ =>
        {
            execute();

            return Task.CompletedTask;
        }, description, onError)
    {
        ArgumentNullException.ThrowIfNull(execute);
    }

    internal Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        => ExecuteWithProgress is null ? Execute(cancellationToken) : ExecuteWithProgress(progress, cancellationToken);
}
