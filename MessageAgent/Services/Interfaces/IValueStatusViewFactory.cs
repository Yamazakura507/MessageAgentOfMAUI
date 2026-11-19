using MessageAgent.Core.Models;

namespace MessageAgent.Services.Interfaces;

public interface IValueStatusViewFactory
{
    View Create(ValueStatus valueStatus);
}
