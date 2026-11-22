using MessageAgent.Core.Enums.Link;
using MessageAgent.Core.Models.SortRule;
using SortLib.Collections;

namespace MessageAgent.Core.Models.Collections;

/// <summary>
/// Observable link collection that automatically maintains its configured sort order.
/// With no explicit rules, links are ordered by <see cref="MessageLink.SortOrder"/> ascending.
/// </summary>
public sealed class MessageLinksCollection : SortableObservableCollection<MessageLink, MessageLinkSortParametr, MessageLinkSortRule>
{
    /// <summary>
    /// Creates an empty link collection with the default SortOrder rule.
    /// </summary>
    public MessageLinksCollection() : base(GetSortKey, MessageLinkSortParametr.SortOrder) { }

    /// <summary>
    /// Creates a sorted link collection from an existing sequence.
    /// </summary>
    /// <param name="links">Links to add.</param>
    public MessageLinksCollection(IEnumerable<MessageLink> links) : base(links, GetSortKey, MessageLinkSortParametr.SortOrder) { }

    private static object? GetSortKey(MessageLink link, MessageLinkSortParametr parameter) => parameter switch
    {
        MessageLinkSortParametr.SortOrder => link.SortOrder,
        MessageLinkSortParametr.Type => link.Type,
        MessageLinkSortParametr.Path => link.Path,
        MessageLinkSortParametr.Title => link.Title,
        MessageLinkSortParametr.Description => link.Description,
        MessageLinkSortParametr.VisualType => link.Value?.GetType().Name,
        MessageLinkSortParametr.DestinationTitle => link.Page?.Title ?? link.Window?.Page?.Title,
        _ => link.SortOrder
    };
}
