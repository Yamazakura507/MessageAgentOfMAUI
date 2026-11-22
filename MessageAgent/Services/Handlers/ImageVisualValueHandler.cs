using MessageAgent.Controls.Provider.StatusMessage;
using MessageAgent.Core.Models.VisualValues;
using MessageAgent.Core.Models.VisualValues.Abstract;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Handlers;

/// <summary>
/// Creates image views for <see cref="ImageVisualValue"/> models.
/// </summary>
public sealed class ImageVisualValueHandler : IVisualValueHandler
{
    /// <inheritdoc />
    public bool CanHandle(VisualValue visualValue) => visualValue is ImageVisualValue;

    /// <inheritdoc />
    public View CreateView(VisualValue visualValue) => new ImageVisualValueView((ImageVisualValue)visualValue);
}
