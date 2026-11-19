namespace MessageAgent.Services.Interfaces;

public interface IMessageAutoHideService : IDisposable
{
    event EventHandler? Elapsed;
    void Start(IDispatcher dispatcher, TimeSpan timeout);
    void Stop();
}
