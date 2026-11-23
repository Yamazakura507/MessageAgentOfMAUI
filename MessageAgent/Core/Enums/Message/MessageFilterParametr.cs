namespace MessageAgent.Core.Enums.Message;

/// <summary>
/// Message fields available for filtering in a MessageCollection.
/// </summary>
public enum MessageFilterParametr
{
    /// <summary>
    /// Message type name.
    /// </summary>
    Type,
    /// <summary>
    /// Message importance flag.
    /// </summary>
    IsImportant,
    /// <summary>
    /// Message read flag.
    /// </summary>
    IsRead,
    /// <summary>
    /// Whether the message has at least one status.
    /// </summary>
    IsStatus,
    /// <summary>
    /// Whether any status name contains the supplied filter text.
    /// </summary>
    ContainStatus,
    /// <summary>
    /// Whether the message has at least one image attachment.
    /// </summary>
    IsImage,
    /// <summary>
    /// Whether the message has at least one link.
    /// </summary>
    IsLink
}
