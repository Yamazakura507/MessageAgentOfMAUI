namespace MessageAgent.Core.Models.VisualValues.Abstract;

/// <summary>
/// Base model for a visual value rendered inside a status or a message link.
/// Concrete types describe image, Lottie animation or custom MAUI view content.
/// </summary>
public abstract record VisualValue
{
    /// <summary>
    /// Gets or sets alternative text exposed to accessibility services.
    /// </summary>
    public string? AlternativeText { get; set; }
}
