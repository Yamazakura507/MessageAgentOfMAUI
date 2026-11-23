namespace MessageAgent.Core.Enums.Message;

/// <summary>
/// Text fields available for searching in a MessageCollection.
/// </summary>
public enum MessageSearchParametr
{
    /// <summary>
    /// Message title.
    /// </summary>
    Title,
    /// <summary>
    /// Message description.
    /// </summary>
    Description,
    /// <summary>
    /// Searches the names of all statuses attached to the message.
    /// A message matches when at least one status name matches the search text.
    /// </summary>
    TextStatuses
}
