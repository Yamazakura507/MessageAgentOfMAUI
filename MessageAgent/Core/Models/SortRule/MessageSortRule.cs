using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Models.Collections;
using SortLib.Enums;
using SortLib.Models;

namespace MessageAgent.Core.Models.SortRule;

/// <summary>
/// Strongly typed sorting rule for <see cref="MessageCollection"/>.
/// </summary>
public sealed class MessageSortRule : SortRule<MessageSortParametr>
{
    /// <summary>
    /// Initializes a message sort rule with default ascending merge sort settings.
    /// </summary>
    public MessageSortRule() { }

    /// <summary>
    /// Initializes a message sort rule for a property, direction and algorithm.
    /// </summary>
    public MessageSortRule(
        MessageSortParametr sortParametr,
        SortType sortType = SortType.Ascending,
        SortKind sortKind = SortKind.Merge) : base(sortParametr, sortType, sortKind) { }
}
