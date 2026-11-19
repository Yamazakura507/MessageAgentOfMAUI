using MessageAgent.Controls.Provider.StatusMessage;
using MessageAgent.Core.Models;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Handlers;

public sealed class ImageValueStatusHandler : IValueStatusHandler
{
    public bool CanHandle(ValueStatus valueStatus) => valueStatus is ImageValueStatus;

    public View CreateView(ValueStatus valueStatus) => new ImageValueStatusView((ImageValueStatus)valueStatus);
}
