using MessageAgent.Core.Models;

namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Creates a visual control for a concrete ValueStatus subtype.
/// </summary>
public interface IValueStatusHandler
{
    bool CanHandle(ValueStatus valueStatus);
    View CreateView(ValueStatus valueStatus);
}
