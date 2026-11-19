namespace MessageAgent.Core.Enums;

/// <summary>
/// Determines how the outline and title color of a status are selected.
/// </summary>
public enum StatusBorderColorMode
{
    /// <summary>
    /// No outline. The title uses the adaptive neutral foreground.
    /// </summary>
    None,

    /// <summary>
    /// Uses the accent color of the owning message.
    /// </summary>
    Label,

    /// <summary>
    /// Uses a generated random color shared by all views of this status.
    /// </summary>
    Random,

    /// <summary>
    /// Extracts a representative color from the image or Lottie value.
    /// </summary>
    VisualElement
}
