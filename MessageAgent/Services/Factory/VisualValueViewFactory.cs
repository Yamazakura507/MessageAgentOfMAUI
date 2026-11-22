using MessageAgent.Core.Models.VisualValues.Abstract;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Factory;

/// <summary>
/// Creates the MAUI view registered for a concrete visual value type.
/// </summary>
public sealed class VisualValueViewFactory(IEnumerable<IVisualValueHandler> handlers) : IVisualValueViewFactory
{
    private readonly IReadOnlyList<IVisualValueHandler> handlers = handlers.ToArray();

    /// <inheritdoc />
    public View Create(VisualValue visualValue)
    {
        ArgumentNullException.ThrowIfNull(visualValue);
        IVisualValueHandler? handler = handlers.FirstOrDefault(candidate => candidate.CanHandle(visualValue));

        return handler?.CreateView(visualValue) 
            ?? throw new NotSupportedException($"No visual value handler is registered for {visualValue.GetType().FullName}.");
    }
}
