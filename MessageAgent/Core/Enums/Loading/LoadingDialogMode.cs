namespace MessageAgent.Core.Enums.Loading;

/// <summary>
/// Controls the visual presentation of the modal loading dialog.
/// </summary>
public enum LoadingDialogMode
{
    /// <summary>
    /// Decorated dialog with close and cancel controls.
    /// </summary>
    Base,
    /// <summary>
    /// Decorated dialog whose accent follows the current Lottie frame.
    /// </summary>
    Dynamic,
    /// <summary>
    /// Only the loading animation is visible.
    /// </summary>
    Transparent,
    /// <summary>
    /// Only the loading animation and the corner close control are visible.
    /// </summary>
    TransparentAndCancelElement
}