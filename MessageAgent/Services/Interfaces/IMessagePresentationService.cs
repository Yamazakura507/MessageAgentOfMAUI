using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Models;
using MessageAgent.Services.Models;

namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Calculates which statuses and message details fit the current compact presentation.
/// </summary>
public interface IMessagePresentationService
{
    /// <summary>
    /// Evaluates the message against visibility, size and truncation constraints.
    /// </summary>
    /// <param name="message">The current message, if any.</param>
    /// <param name="showType">The configured label visibility mode.</param>
    /// <param name="labelVisible">Whether the compact label is currently visible.</param>
    /// <param name="detailsDialogOpened">Whether the details dialog is already open.</param>
    /// <param name="maxTitleLength">An optional title character limit.</param>
    /// <param name="maxStatusesOnLabel">The maximum number of badges shown on the label.</param>
    /// <param name="providerWidth">The available provider width.</param>
    /// <param name="borderWidth">The measured label border width.</param>
    /// <param name="borderPadding">The label border padding.</param>
    /// <param name="titleLabelWidth">The measured title label width.</param>
    /// <param name="titleFontSize">The title font size.</param>
    /// <param name="titleFontFamily">The title font family.</param>
    /// <param name="titleFontAttributes">The title font attributes.</param>
    /// <param name="titleCharacterSpacing">The title character spacing.</param>
    /// <returns>The presentation state used by the provider view.</returns>
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
