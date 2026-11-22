using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Extensions;
using MessageAgent.Core.Models;
using MessageAgent.Services.Interfaces;
using MessageAgent.Services.Models;

namespace MessageAgent.Services;

/// <summary>
/// Calculates which statuses fit in the compact label.
/// It first removes optional status titles, then hides statuses from the end.
/// </summary>
public sealed class MessagePresentationService : IMessagePresentationService
{
    private const double TitleMinimumWidth = 38;
    private const double ReadIconWidth = 15;
    private const double StatusVisualWidth = 18;
    private const double StatusBorderHorizontalOverhead = 8;
    private const double AverageTitleCharacterWidth = 6.5;

    private static double GetStatusDisplayWidth(Status status, bool showTitle)
    {
        double visualWidth = status.Value is null ? 0 : StatusVisualWidth;
        double borderWidth = StatusBorderHorizontalOverhead;
        bool effectiveShowTitle = status.Value is null || showTitle;

        double titleWidth = effectiveShowTitle
            ? Math.Max(12, status.Name.Length * AverageTitleCharacterWidth) + (visualWidth > 0 ? 4 : 0) : 0;

        return borderWidth + visualWidth + titleWidth + 3;
    }

    /// <inheritdoc />
    public MessagePresentationState Evaluate(
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
        double titleCharacterSpacing)
    {
        if (message is null) return new MessagePresentationState(Array.Empty<StatusPresentationItem>(), false, false, false);

        List<Status> statuses = message.Status.Distinct<Status>(ReferenceEqualityComparer.Instance).ToList();
        int statusLimit = Math.Max(0, maxStatusesOnLabel);
        bool hasStatusOverflow = statuses.Count > statusLimit;
        List<Status> labelStatuses = statuses.Take(statusLimit).ToList();
        double effectiveBorderWidth = borderWidth > 0 ? borderWidth : Math.Max(0, providerWidth - 44);
        double usableWidth = effectiveBorderWidth - borderPadding.HorizontalThickness;
        double overflowActionWidth = hasStatusOverflow ? 36 : 0;
        double availableStatusWidth = Math.Max(0, usableWidth - TitleMinimumWidth - ReadIconWidth - 10 - overflowActionWidth);

        List<StatusPresentationItem> presentedStatuses = labelStatuses
            .Select(status => new StatusPresentationItem(status, status.Value is not null && status.ShowTitle))
            .ToList();

        double totalStatusWidth = presentedStatuses.Sum(item => GetStatusDisplayWidth(item.Status, item.ShowTitle));
        bool statusTitlesTruncated = false;

        if (totalStatusWidth > availableStatusWidth)
        {
            for (int index = 0; index < presentedStatuses.Count; index++)
            {
                StatusPresentationItem item = presentedStatuses[index];

                if (item.Status.Value is null || !item.ShowTitle) continue;

                double previousWidth = GetStatusDisplayWidth(item.Status, true);
                double compactWidth = GetStatusDisplayWidth(item.Status, false);

                presentedStatuses[index] = item with { ShowTitle = false };
                totalStatusWidth -= previousWidth - compactWidth;
                statusTitlesTruncated = true;

                if (totalStatusWidth <= availableStatusWidth) break;
            }
        }

        while (presentedStatuses.Count > 0 && totalStatusWidth > availableStatusWidth)
        {
            StatusPresentationItem last = presentedStatuses[^1];

            totalStatusWidth -= GetStatusDisplayWidth(last.Status, last.ShowTitle);
            presentedStatuses.RemoveAt(presentedStatuses.Count - 1);
        }

        IReadOnlyList<StatusPresentationItem> visibleStatuses = presentedStatuses;
        string title = message.Title ?? string.Empty;
        bool explicitlyTruncated = maxTitleLength is int limit && title.Length > limit;
        bool visuallyTruncated = false;

        if (title.Length > 0)
        {
            Label measurementLabel = new()
            {
                Text = title,
                FontSize = titleFontSize,
                FontFamily = titleFontFamily,
                FontAttributes = titleFontAttributes,
                CharacterSpacing = titleCharacterSpacing,
                LineBreakMode = LineBreakMode.NoWrap
            };

            Size naturalSize = measurementLabel.Measure(double.PositiveInfinity, double.PositiveInfinity);
            double iconsWidth = visibleStatuses.Sum(item => GetStatusDisplayWidth(item.Status, item.ShowTitle)) + ReadIconWidth + 3;
            double estimatedTitleWidth = Math.Max(0, usableWidth - iconsWidth - 6);
            double actualTitleWidth = titleLabelWidth > 0 ? titleLabelWidth : estimatedTitleWidth;

            visuallyTruncated = naturalSize.Width > actualTitleWidth + 0.5;
        }

        bool statusesHidden = statuses.Count > 0 && (showType == MessageShowType.HideLableAlways || !labelVisible);
        bool statusesTruncated = statuses.Count > visibleStatuses.Count || statusTitlesTruncated;
        bool hasDescription = !string.IsNullOrWhiteSpace(message.Description);
        bool hasImages = message.Images is { Count: > 0 };
        bool hasLinks = message.Links is { Count: > 0 };
        bool hasDetails = explicitlyTruncated || visuallyTruncated || statusesHidden || statusesTruncated || hasDescription || hasLinks || hasImages;
        bool isImportant = message.IsImportant;
        bool hiddenContent = showType == MessageShowType.HideLableAlways || hasDetails;
        bool shouldLoopNotification = isImportant && hiddenContent && !detailsDialogOpened && !message.IsRead;

        return new MessagePresentationState(visibleStatuses, hasStatusOverflow, hasDetails, shouldLoopNotification);
    }
}
