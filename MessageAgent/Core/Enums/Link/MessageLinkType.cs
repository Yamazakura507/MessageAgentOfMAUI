namespace MessageAgent.Core.Enums.Link;

/// <summary>
/// Destination kind for a message link.
/// </summary>
public enum MessageLinkType
{
    /// <summary>
    /// Opens a validated HTTP or HTTPS URL.
    /// </summary>
    Web,
    /// <summary>
    /// Opens a file confined to the application writable data directory.
    /// </summary>
    LocalStorage,
    /// <summary>
    /// Navigates to a Shell route, page or window.
    /// </summary>
    Navigation
}
