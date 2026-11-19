using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services;

/// <summary>
/// Encapsulates the one-shot dispatcher timer used by auto-hide labels.
/// </summary>
public sealed class MessageAutoHideService : IMessageAutoHideService
{
    private IDispatcherTimer? timer;

    public event EventHandler? Elapsed;

    public void Start(IDispatcher dispatcher, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        timer ??= dispatcher.CreateTimer();
        timer.IsRepeating = false;
        timer.Tick -= OnTimerElapsed;
        timer.Tick += OnTimerElapsed;
        timer.Stop();
        timer.Interval = timeout;
        timer.Start();
    }

    public void Stop() => timer?.Stop();

    private void OnTimerElapsed(object? sender, EventArgs e)
    {
        Stop();

        Elapsed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (timer is null) return;

        timer.Stop();
        timer.Tick -= OnTimerElapsed;
        timer = null;
    }
}
