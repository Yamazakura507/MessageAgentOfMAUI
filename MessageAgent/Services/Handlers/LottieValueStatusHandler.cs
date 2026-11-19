using LottieAgent.Interfaces;
using MessageAgent.Controls.Provider.StatusMessage;
using MessageAgent.Core.Models;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Handlers;

public sealed class LottieValueStatusHandler(ILottieJsonLoader jsonLoader, ILottieModifierProcessor modifierProcessor) : IValueStatusHandler
{
    public bool CanHandle(ValueStatus valueStatus) => valueStatus is LottieValueStatus;

    public View CreateView(ValueStatus valueStatus) => new LottieStatusView((LottieValueStatus)valueStatus, jsonLoader, modifierProcessor);
}
