namespace MessageAgent.Core.Enums.Link;

/// <summary>
/// Fields available for sorting a <see cref="Models.MessageLink"/> collection.
/// </summary>
public enum MessageLinkSortParametr
{
    /// <summary>
    /// Explicit display order, used by default when no rules are configured.
    /// </summary>
    SortOrder,
    /// <summary>
    /// Destination kind.
    /// </summary>
    Type,
    /// <summary>
    /// Destination path, URL or Shell route.
    /// </summary>
    Path,
    /// <summary>
    /// Displayed link title.
    /// </summary>
    Title,
    /// <summary>
    /// Optional link description.
    /// </summary>
    Description,
    /// <summary>
    /// Runtime type name of the assigned visual value.
    /// </summary>
    VisualType,
    /// <summary>
    /// Page title supplied for navigation links.
    /// </summary>
    DestinationTitle
}
