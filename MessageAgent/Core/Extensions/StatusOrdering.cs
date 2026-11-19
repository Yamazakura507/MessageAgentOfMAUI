namespace MessageAgent.Core.Extensions;

using MessageAgent.Core.Models;

/// <summary>
/// Applies the common stable status ordering used by compact and expanded presentations.
/// </summary>
internal static class StatusOrdering
{
    public static List<Status> Sort(this IEnumerable<Status>? statuses, bool distinct = false)
    {
        if (statuses is null) return [];

        IEnumerable<(Status Status, int Index)> indexed = statuses
            .Select((status, index) => (status, index))
            .OrderBy(item => item.status.SortOrder)
            .ThenBy(item => item.index)
            .Select(item => (item.status, item.index));

        if (!distinct) return indexed.Select(item => item.Status).ToList();

        HashSet<Status> seen = new (ReferenceEqualityComparer.Instance);

        return indexed.Select(item => item.Status).Where(seen.Add).ToList();
    }
}
