using MessageAgent.Core.Enums.Link;
using MessageAgent.Core.Models.Collections;
using SortLib.Enums;
using SortLib.Models;

namespace MessageAgent.Core.Models.SortRule;

/// <summary>
/// Strongly typed sorting rule for <see cref="MessageLinksCollection"/>.
/// </summary>
public sealed class MessageLinkSortRule : SortRule<MessageLinkSortParametr>
{
    /// <summary>
    /// Initializes a link sort rule with default ascending merge sort settings.
    /// </summary>
    public MessageLinkSortRule() { }

    /// <summary>
    /// Initializes a link sort rule for a property, direction and algorithm.
    /// </summary>
    public MessageLinkSortRule(
        MessageLinkSortParametr sortParametr, 
        SortType sortType = SortType.Ascending,
        SortKind sortKind = SortKind.Merge) : base(sortParametr, sortType, sortKind) { }
}
