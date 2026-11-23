namespace MessageAgent.Core.Enums.Message;

/// <summary>
/// Fields available for sorting a <see cref="Models.Message"/> collection.
/// </summary>
public enum MessageSortParametr
{
    /// <summary>
    /// Explicit display order, used by default when no rules are configured.
    /// </summary>
    SortOrder,

    /// <summary>
    /// Unique message identifier.
    /// </summary>
    Id,

    /// <summary>
    /// Message title.
    /// </summary>
    Title,

    /// <summary>
    /// Stable name of the assigned message type.
    /// </summary>
    Type,

    /// <summary>
    /// Optional message description.
    /// </summary>
    Description,

    /// <summary>
    /// Whether the message is marked as important.
    /// </summary>
    IsImportant,

    /// <summary>
    /// Whether the message has been read.
    /// </summary>
    IsRead,

    /// <summary>
    /// Number of statuses attached to the message.
    /// </summary>
    StatusCount,

    /// <summary>
    /// Number of image attachments.
    /// </summary>
    ImageCount,

    /// <summary>
    /// Number of links attached to the message.
    /// </summary>
    LinkCount
}
