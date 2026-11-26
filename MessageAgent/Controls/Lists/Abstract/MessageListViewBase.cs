using CollectionLib.Controls;
using SortLib.Enums;
using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Models;
using MessageAgent.Core.Models.Filtering;
using MessageAgent.Core.Models.Grouping;
using MessageAgent.Core.Models.SortRule;

namespace MessageAgent.Controls.Lists.Abstract;

/// <summary>
/// Typed MessageAgent adapter over CollectionLib's generic list implementation.
/// It binds the message rules and parameter enums to the shared collection-list UI.
/// </summary>
public abstract class MessageListViewBase : CollectionListView<
    Message,
    MessageFilterParametr,
    MessageSearchParametr,
    GroupMessageParametrs,
    MessageSortParametr,
    MessageFilterRule,
    MessageSearchRule,
    MessageGroupRule,
    MessageSortRule>
{
    internal static readonly MessageSearchParametr[] SearchParameters =
    [
        MessageSearchParametr.Title,
        MessageSearchParametr.Description,
        MessageSearchParametr.TextStatuses
    ];

    internal static readonly MessageSortParametr[] SortParameters =
    [
        MessageSortParametr.SortOrder,
        MessageSortParametr.Title,
        MessageSortParametr.Type,
        MessageSortParametr.Description,
        MessageSortParametr.IsImportant,
        MessageSortParametr.IsRead,
        MessageSortParametr.StatusCount,
        MessageSortParametr.ImageCount,
        MessageSortParametr.LinkCount
    ];

    internal static readonly MessageFilterParametr[] BooleanFilterParameters =
    [
        MessageFilterParametr.IsImportant,
        MessageFilterParametr.IsRead,
        MessageFilterParametr.IsStatus,
        MessageFilterParametr.IsImage,
        MessageFilterParametr.IsLink
    ];

    internal static readonly GroupMessageParametrs[] GroupParameters =
    [
        GroupMessageParametrs.Type,
        GroupMessageParametrs.IsImportant,
        GroupMessageParametrs.IsRead
    ];


    private static string FormatGroupValue(GroupMessageParametrs parameter, object? value)
    {
        if (parameter == GroupMessageParametrs.IsImportant)  return value is true ? "Важные" : "Обычные";
        if (parameter == GroupMessageParametrs.IsRead)  return value is true ? "Прочитанные" : "Непрочитанные";

        return value?.ToString() ?? "Не задано";
    }


    private static string GetSearchParameterName(MessageSearchParametr parameter) => parameter switch
    {
        MessageSearchParametr.Title => "Заголовок",
        MessageSearchParametr.Description => "Описание",
        MessageSearchParametr.TextStatuses => "Статусы",
        _ => parameter.ToString()
    };

    private static string GetSortParameterName(MessageSortParametr parameter) => parameter switch
    {
        MessageSortParametr.SortOrder => "Порядок отображения",
        MessageSortParametr.Title => "Заголовок",
        MessageSortParametr.Type => "Тип",
        MessageSortParametr.Description => "Описание",
        MessageSortParametr.IsImportant => "Важность",
        MessageSortParametr.IsRead => "Прочитанность",
        MessageSortParametr.StatusCount => "Количество статусов",
        MessageSortParametr.ImageCount => "Количество изображений",
        MessageSortParametr.LinkCount => "Количество ссылок",
        _ => parameter.ToString()
    };

    /// <summary>
    /// Gets the localized display name for a sorting algorithm.
    /// </summary>
    protected override string GetSortKindDisplayName(SortKind kind) => kind switch
    {
        SortKind.Bubble => "Пузырьковая",
        SortKind.Selection => "Выбором",
        SortKind.Insertion => "Вставками",
        SortKind.Quick => "Быстрая",
        SortKind.Merge => "Слиянием",
        SortKind.Heap => "Пирамидальная",
        _ => kind.ToString()
    };

    private static string GetFilterParameterName(MessageFilterParametr parameter) => parameter switch
    {
        MessageFilterParametr.IsImportant => "Важные сообщения",
        MessageFilterParametr.IsRead => "Прочитанные",
        MessageFilterParametr.IsStatus => "Есть статусы",
        MessageFilterParametr.IsImage => "Есть изображения",
        MessageFilterParametr.IsLink => "Есть ссылки",
        MessageFilterParametr.ContainStatus => "Текст статуса",
        MessageFilterParametr.Type => "Тип",
        _ => parameter.ToString()
    };

    private static string GetGroupParameterName(GroupMessageParametrs parameter) => parameter switch
    {
        GroupMessageParametrs.Type => "Тип",
        GroupMessageParametrs.IsImportant => "Важность",
        GroupMessageParametrs.IsRead => "Прочитанность",
        _ => parameter.ToString()
    };

    /// <summary>
    /// Initializes the base list with the message-specific search, sort, filter, and grouping parameters.
    /// </summary>
    protected MessageListViewBase() : base(
            SearchParameters,
            SortParameters,
            BooleanFilterParameters,
            GroupParameters,
            GetSearchParameterName,
            GetSortParameterName,
            GetFilterParameterName,
            GetGroupParameterName,
            FormatGroupValue)
    {
    }
}
