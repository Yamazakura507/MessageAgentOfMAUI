using CollectionLib.Controls;
using SortLib.Enums;
using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Models;
using MessageAgent.Core.Models.Filtering;
using MessageAgent.Core.Models.Grouping;
using MessageAgent.Core.Models.SortRule;

namespace MessageAgent.Controls.Lists;

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
        if (parameter == GroupMessageParametrs.IsImportant)
            return value is true ? "Р’Р°Р¶РЅС‹Рµ" : "РћР±С‹С‡РЅС‹Рµ";
        if (parameter == GroupMessageParametrs.IsRead)
            return value is true ? "РџСЂРѕС‡РёС‚Р°РЅРЅС‹Рµ" : "РќРµРїСЂРѕС‡РёС‚Р°РЅРЅС‹Рµ";
        return value?.ToString() ?? "РќРµ Р·Р°РґР°РЅРѕ";
    }


    private static string GetSearchParameterName(MessageSearchParametr parameter) => parameter switch
    {
        MessageSearchParametr.Title => "Р—Р°РіРѕР»РѕРІРѕРє",
        MessageSearchParametr.Description => "РћРїРёСЃР°РЅРёРµ",
        MessageSearchParametr.TextStatuses => "РЎС‚Р°С‚СѓСЃС‹",
        _ => parameter.ToString()
    };

    private static string GetSortParameterName(MessageSortParametr parameter) => parameter switch
    {
        MessageSortParametr.SortOrder => "РџРѕСЂСЏРґРѕРє РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ",
        MessageSortParametr.Title => "Р—Р°РіРѕР»РѕРІРѕРє",
        MessageSortParametr.Type => "РўРёРї",
        MessageSortParametr.Description => "РћРїРёСЃР°РЅРёРµ",
        MessageSortParametr.IsImportant => "Р’Р°Р¶РЅРѕСЃС‚СЊ",
        MessageSortParametr.IsRead => "РџСЂРѕС‡РёС‚Р°РЅРЅРѕСЃС‚СЊ",
        MessageSortParametr.StatusCount => "РљРѕР»РёС‡РµСЃС‚РІРѕ СЃС‚Р°С‚СѓСЃРѕРІ",
        MessageSortParametr.ImageCount => "РљРѕР»РёС‡РµСЃС‚РІРѕ РёР·РѕР±СЂР°Р¶РµРЅРёР№",
        MessageSortParametr.LinkCount => "РљРѕР»РёС‡РµСЃС‚РІРѕ СЃСЃС‹Р»РѕРє",
        _ => parameter.ToString()
    };

    protected override string GetSortKindDisplayName(SortKind kind) => kind switch
    {
        SortKind.Bubble => "РџСѓР·С‹СЂСЊРєРѕРІР°СЏ",
        SortKind.Selection => "Р’С‹Р±РѕСЂРѕРј",
        SortKind.Insertion => "Р’СЃС‚Р°РІРєР°РјРё",
        SortKind.Quick => "Р‘С‹СЃС‚СЂР°СЏ",
        SortKind.Merge => "РЎР»РёСЏРЅРёРµРј",
        SortKind.Heap => "РџРёСЂР°РјРёРґР°Р»СЊРЅР°СЏ",
        _ => kind.ToString()
    };

    private static string GetFilterParameterName(MessageFilterParametr parameter) => parameter switch
    {
        MessageFilterParametr.IsImportant => "Р’Р°Р¶РЅС‹Рµ СЃРѕРѕР±С‰РµРЅРёСЏ",
        MessageFilterParametr.IsRead => "РџСЂРѕС‡РёС‚Р°РЅРЅС‹Рµ",
        MessageFilterParametr.IsStatus => "Р•СЃС‚СЊ СЃС‚Р°С‚СѓСЃС‹",
        MessageFilterParametr.IsImage => "Р•СЃС‚СЊ РёР·РѕР±СЂР°Р¶РµРЅРёСЏ",
        MessageFilterParametr.IsLink => "Р•СЃС‚СЊ СЃСЃС‹Р»РєРё",
        MessageFilterParametr.ContainStatus => "РўРµРєСЃС‚ СЃС‚Р°С‚СѓСЃР°",
        MessageFilterParametr.Type => "РўРёРї",
        _ => parameter.ToString()
    };

    private static string GetGroupParameterName(GroupMessageParametrs parameter) => parameter switch
    {
        GroupMessageParametrs.Type => "РўРёРї",
        GroupMessageParametrs.IsImportant => "Р’Р°Р¶РЅРѕСЃС‚СЊ",
        GroupMessageParametrs.IsRead => "РџСЂРѕС‡РёС‚Р°РЅРЅРѕСЃС‚СЊ",
        _ => parameter.ToString()
    };

    protected MessageListViewBase()
        : base(
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
