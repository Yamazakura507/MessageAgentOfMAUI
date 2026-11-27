using MessageAgent.Core.Enums.Loading;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MessageAgent.Core.Models.Loading;

/// <summary>
/// Observable presentation state for a single loading action.
/// </summary>
public sealed class LoadingActionState : INotifyPropertyChanged
{
    private LoadingActionStatus status;
    private double progress;
    private Exception? exception;
    private DateTimeOffset? finishedAt;
    private int loadedStatusAnimationViewCount;
    private int statusAnimationViewGeneration;
    private TaskCompletionSource statusAnimationCompletion = CompletedAnimationSource();
    private bool IsTerminalStatus => Status is LoadingActionStatus.Completed or LoadingActionStatus.Failed or LoadingActionStatus.Cancelled;

    /// <summary>
    /// Gets the original zero-based action position.
    /// </summary>
    public int Sequence { get; }

    /// <summary>
    /// Gets the user-facing action description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the current action status.
    /// </summary>
    public LoadingActionStatus Status
    {
        get => status;
        private set
        {
            if (status == value) return;
            status = value;
            statusAnimationCompletion = value is LoadingActionStatus.Completed
                or LoadingActionStatus.Failed
                or LoadingActionStatus.Cancelled
                    ? new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
                    : CompletedAnimationSource();

            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusGlyph));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusAnimationSource));
            OnPropertyChanged(nameof(StatusAnimationIsLooping));
        }
    }

    /// <summary>
    /// Gets the normalized progress in the inclusive range [0, 1].
    /// </summary>
    public double Progress
    {
        get => progress;
        private set
        {
            double normalized = double.IsFinite(value) ? Math.Clamp(value, 0d, 1d) : 0d;

            if (Math.Abs(progress - normalized) < 0.0001d) return;

            progress = normalized;

            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    /// <summary>
    /// Gets the formatted progress percentage.
    /// </summary>
    public string ProgressText => $"{Math.Round(Progress * 100d):0}%";

    /// <summary>
    /// Gets whether the progress controls should be displayed.
    /// </summary>
    public bool IsShowProgress { get; }

    /// <summary>
    /// Gets the exception produced by the action, if any.
    /// </summary>
    public Exception? Exception
    {
        get => exception;
        private set
        {
            if (ReferenceEquals(exception, value)) return;

            exception = value;

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the completion or failure time, when the action reached a terminal state.
    /// </summary>
    public DateTimeOffset? FinishedAt
    {
        get => finishedAt;
        private set
        {
            if (finishedAt == value) return;

            finishedAt = value;

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the Lottie asset used to represent the current action state.
    /// </summary>
    public string StatusAnimationSource => Status switch
    {
        LoadingActionStatus.Pending => "Lotties/LoadingActionStatus/pending.json",
        LoadingActionStatus.Running => "Lotties/LoadingActionStatus/running.json",
        LoadingActionStatus.Completed => "Lotties/LoadingActionStatus/completed.json",
        LoadingActionStatus.Failed or LoadingActionStatus.Cancelled
            => "Lotties/LoadingActionStatus/failedOrCancelled.json",
        _ => "Lotties/LoadingActionStatus/pending.json"
    };

    /// <summary>
    /// Gets whether the current status animation should repeat.
    /// Pending and Running repeat; terminal states play once.
    /// </summary>
    public bool StatusAnimationIsLooping => Status is LoadingActionStatus.Pending or LoadingActionStatus.Running;

    /// <summary>
    /// Gets the compact status glyph for compatibility with existing consumers.
    /// </summary>
    public string StatusGlyph => Status switch
    {
        LoadingActionStatus.Completed => "✓",
        LoadingActionStatus.Failed => "✕",
        LoadingActionStatus.Cancelled => "–",
        LoadingActionStatus.Running => "…",
        _ => "○"
    };

    /// <summary>
    /// Gets the status glyph color.
    /// </summary>
    public Color StatusColor => Status switch
    {
        LoadingActionStatus.Completed => Color.FromArgb("#2E9D65"),
        LoadingActionStatus.Failed => Color.FromArgb("#D64545"),
        LoadingActionStatus.Cancelled => Color.FromArgb("#8A9099"),
        LoadingActionStatus.Running => Color.FromArgb("#2785D8"),
        _ => Color.FromArgb("#8A9099")
    };

    /// <summary>
    /// Creates presentation state for an action.
    /// </summary>
    public LoadingActionState(string? description, bool isShowProgress, int sequence = 0)
    {
        Description = string.IsNullOrWhiteSpace(description) ? "Выполнение операции" : description;
        IsShowProgress = isShowProgress;
        Sequence = sequence;
    }

    /// <summary>
    /// Waits until the current terminal-state Lottie animation has reached its final frame.
    /// </summary>
    internal Task WaitForStatusAnimationAsync() => statusAnimationCompletion.Task;

    /// <summary>
    /// Registers a status animation view that is currently part of a visible list.
    /// </summary>
    internal void AttachStatusAnimationView()
    {
        Interlocked.Increment(ref statusAnimationViewGeneration);
        Interlocked.Increment(ref loadedStatusAnimationViewCount);
    }

    /// <summary>
    /// Unregisters a status animation view. If the last view disappears before its animation
    /// completes, the wait is released because there is no longer a visible animation to finish.
    /// </summary>
    internal void DetachStatusAnimationView()
    {
        int remaining = Interlocked.Decrement(ref loadedStatusAnimationViewCount);

        if (remaining < 0)
        {
            Interlocked.Exchange(ref loadedStatusAnimationViewCount, 0);

            return;
        }

        if (remaining == 0 && IsTerminalStatus)
        {
            int generation = Volatile.Read(ref statusAnimationViewGeneration);
            _ = CompleteWhenNoAnimationViewRemainsAsync(generation);
        }
    }

    private async Task CompleteWhenNoAnimationViewRemainsAsync(int generation)
    {
        await Task.Delay(300);

        if (Volatile.Read(ref loadedStatusAnimationViewCount) == 0
            && Volatile.Read(ref statusAnimationViewGeneration) == generation
            && IsTerminalStatus)
        {
            MarkStatusAnimationCompleted();
        }
    }

    /// <summary>
    /// Signals that an active terminal-state Lottie animation has completed.
    /// </summary>
    internal void MarkStatusAnimationCompleted()
    {
        if (IsTerminalStatus) statusAnimationCompletion.TrySetResult();
    }

    private static TaskCompletionSource CompletedAnimationSource()
    {
        TaskCompletionSource source = new(TaskCreationOptions.RunContinuationsAsynchronously);

        source.SetResult();

        return source;
    }

    internal void MarkRunning() => Status = LoadingActionStatus.Running;

    internal void ReportProgress(double percentage)
    {
        if (Status != LoadingActionStatus.Running) return;

        Progress = percentage / 100d;
    }

    internal void MarkCompleted()
    {
        Progress = 1d;
        FinishedAt = DateTimeOffset.UtcNow;
        Status = LoadingActionStatus.Completed;
    }

    internal void MarkFailed(Exception error)
    {
        Exception = error;
        FinishedAt = DateTimeOffset.UtcNow;
        Status = LoadingActionStatus.Failed;
    }

    internal void MarkCancelled() => Status = LoadingActionStatus.Cancelled;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
