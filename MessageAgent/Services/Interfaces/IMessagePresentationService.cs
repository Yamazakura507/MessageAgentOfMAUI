using MessageAgent.Core.Enums;
using MessageAgent.Core.Models;
using MessageAgent.Services.Models;

namespace MessageAgent.Services.Interfaces;

public interface IMessagePresentationService
{
    MessagePresentationState Evaluate(
        Message? message,
        MessageShowType showType,
        bool labelVisible,
        bool detailsDialogOpened,
        int? maxTitleLength,
        int maxStatusesOnLabel,
        double providerWidth,
        double borderWidth,
        Thickness borderPadding,
        double titleLabelWidth,
        double titleFontSize,
        string? titleFontFamily,
        FontAttributes titleFontAttributes,
        double titleCharacterSpacing);
}
