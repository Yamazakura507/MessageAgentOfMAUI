using MessageAgent.Core.Enums;

namespace MessageAgent.Services.Interfaces;

public interface IMessageLabelAnimator
{
    Task SetVisibilityAsync(Border target, bool visible, MessageLabelEffect effect, bool animated);
}
