using MessageAgent.Core.Models;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Factory;

public sealed class ValueStatusViewFactory(IEnumerable<IValueStatusHandler> handlers) : IValueStatusViewFactory
{
    private readonly IReadOnlyList<IValueStatusHandler> handlers = handlers.ToArray();

    public View Create(ValueStatus valueStatus)
    {
        ArgumentNullException.ThrowIfNull(valueStatus);

        IValueStatusHandler? handler = handlers.FirstOrDefault(candidate => candidate.CanHandle(valueStatus));

        return handler?.CreateView(valueStatus) ?? throw new NotSupportedException($"No status value handler is registered for {valueStatus.GetType().FullName}.");
    }
}
