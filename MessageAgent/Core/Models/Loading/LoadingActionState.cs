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
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusGlyph));
            OnPropertyChanged(nameof(StatusColor));
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
    /// Gets the compact status glyph used by the loading dialog.
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
