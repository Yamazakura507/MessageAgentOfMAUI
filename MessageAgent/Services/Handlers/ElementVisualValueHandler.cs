using MessageAgent.Controls.Provider.StatusMessage;
using MessageAgent.Core.Models.VisualValues;
using MessageAgent.Core.Models.VisualValues.Abstract;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Handlers;

/// <summary>
/// Hosts a user-provided MAUI view from an <see cref="ElementVisualValue"/>.
/// </summary>
public sealed class ElementVisualValueHandler : IVisualValueHandler
{
    /// <inheritdoc />
    public bool CanHandle(VisualValue visualValue) => visualValue is ElementVisualValue;

    /// <inheritdoc />
    public View CreateView(VisualValue visualValue) => new ElementVisualValueView((ElementVisualValue)visualValue);
}
