using MessageAgent.Core.Models.VisualValues.Abstract;

namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Creates the MAUI control used to display a polymorphic visual value.
/// </summary>
public interface IVisualValueViewFactory
{
    /// <summary>
    /// Creates a view for an image, Lottie animation or custom element visual value.
    /// </summary>
    View Create(VisualValue visualValue);
}
