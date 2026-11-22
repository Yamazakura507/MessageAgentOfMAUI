using MessageAgent.Core.Models.VisualValues.Abstract;

namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Creates a visual MAUI control for one concrete <see cref="VisualValue"/> subtype.
/// </summary>
public interface IVisualValueHandler
{
    /// <summary>
    /// Determines whether this handler can render the supplied visual value.
    /// </summary>
    bool CanHandle(VisualValue visualValue);

    /// <summary>
    /// Creates the MAUI control used to display the supplied visual value.
    /// </summary>
    View CreateView(VisualValue visualValue);
}
