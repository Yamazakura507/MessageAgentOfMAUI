using MessageAgent.Core.Models.VisualValues.Abstract;

namespace MessageAgent.Core.Models.VisualValues;

/// <summary>
/// Represents a user-supplied MAUI view as a reusable visual value.
/// </summary>
public sealed record ElementVisualValue : VisualValue
{
    /// <summary>
    /// Gets or sets the custom view displayed by the host control.
    /// </summary>
    public View? VisualElement { get; set; }

    /// <summary>
    /// Initializes an empty custom element visual value for XAML.
    /// </summary>
    public ElementVisualValue() { }

    /// <summary>
    /// Initializes a custom element visual value.
    /// </summary>
    /// <param name="visualElement">The MAUI view to host.</param>
    /// <param name="alternativeText">Optional accessible text.</param>
    public ElementVisualValue(View visualElement, string? alternativeText = null)
    {
        VisualElement = visualElement ?? throw new ArgumentNullException(nameof(visualElement));
        AlternativeText = alternativeText;
    }
}
