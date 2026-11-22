using MessageAgent.Core.Enums.Status;
using MessageAgent.Core.Models.SortRule;
using SortLib.Collections;

namespace MessageAgent.Core.Models.Collections;

/// <summary>
/// Observable status collection that automatically maintains its configured sort order.
/// With no explicit rules, statuses are ordered by <see cref="Status.SortOrder"/> ascending.
/// </summary>
public sealed class StatusCollection : SortableObservableCollection<Status, StatusSortParametr, StatusSortRule>
{
    /// <summary>
    /// Creates an empty status collection with the default SortOrder rule.
    /// </summary>
    public StatusCollection() : base(GetSortKey, StatusSortParametr.SortOrder) { }

    /// <summary>
    /// Creates a sorted status collection from an existing sequence.
    /// </summary>
    /// <param name="statuses">Statuses to add.</param>
    public StatusCollection(IEnumerable<Status> statuses) : base(statuses, GetSortKey, StatusSortParametr.SortOrder) { }

    private static object? GetSortKey(Status status, StatusSortParametr parameter) => parameter switch
    {
        StatusSortParametr.SortOrder => status.SortOrder,
        StatusSortParametr.Name => status.Name,
        StatusSortParametr.ShowTitle => status.ShowTitle,
        StatusSortParametr.Description => status.Description,
        StatusSortParametr.BorderColorMode => status.BorderColorMode,
        StatusSortParametr.VisualType => status.Value?.GetType().Name,
        _ => status.SortOrder
    };
}
