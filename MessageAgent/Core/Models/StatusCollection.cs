using System.Collections.ObjectModel;

namespace MessageAgent.Core.Models;

/// <summary>
/// Observable collection of message statuses. Can be populated directly in MAUI XAML.
/// </summary>
public sealed class StatusCollection : ObservableCollection<Status>
{
    public StatusCollection() { }

    public StatusCollection(IEnumerable<Status> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        foreach (Status status in statuses) Add(status);
    }
}
