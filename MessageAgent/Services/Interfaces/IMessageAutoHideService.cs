namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Controls the one-shot timer that hides an expanded message label.
/// </summary>
public interface IMessageAutoHideService : IDisposable
{
    /// <summary>
    /// Raised when the configured auto-hide interval expires.
    /// </summary>
    event EventHandler? Elapsed;

    /// <summary>
    /// Starts or restarts the timer.
    /// </summary>
    /// <param name="dispatcher">The UI dispatcher that creates the timer.</param>
    /// <param name="timeout">The positive timeout interval.</param>
    void Start(IDispatcher dispatcher, TimeSpan timeout);
    /// <summary>
    /// Stops the timer without raising the elapsed event.
    /// </summary>
    void Stop();
}
