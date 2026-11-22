using MessageAgent.Core.Enums.Status;
using MessageAgent.Core.Models.Collections;
using SortLib.Enums;
using SortLib.Models;

namespace MessageAgent.Core.Models.SortRule;

/// <summary>
/// Strongly typed sorting rule for <see cref="StatusCollection"/>.
/// </summary>
public sealed class StatusSortRule : SortRule<StatusSortParametr>
{
    /// <summary>
    /// Initializes a status sort rule with default ascending merge sort settings.
    /// </summary>
    public StatusSortRule() { }

    /// <summary>
    /// Initializes a status sort rule for a property, direction and algorithm.
    /// </summary>
    public StatusSortRule(
        StatusSortParametr sortParametr, 
        SortType sortType = SortType.Ascending,
        SortKind sortKind = SortKind.Merge) : base(sortParametr, sortType, sortKind) { }
}
