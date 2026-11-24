using CollectionLib.Collections;
using GroupLib.Models;
using GroupLib.Services;
using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Core.Models.Filtering;
using MessageAgent.Core.Models.Grouping;
using MessageAgent.Core.Models.SortRule;

namespace MessageAgent.Core.Models.Collections;

/// <summary>
/// Message-specific collection. Generic sorting, filtering, searching and grouping mechanics
/// are implemented by CollectionViewCollection; this class supplies only message selectors.
/// </summary>
public sealed class MessageCollection : CollectionViewCollection<
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
    /// <summary>Filtered and searched messages in current sort order.</summary>
    public System.Collections.ObjectModel.ReadOnlyObservableCollection<Message> FilteredMessages => FilteredItems;

    /// <summary>Creates an empty collection with SortOrder as the default key.</summary>
    public MessageCollection()
        : base(GetSortKey, MessageSortParametr.SortOrder, MatchesFilter, GetSearchValues, GetGroupKey) { }

    /// <summary>Creates a collection from existing messages.</summary>
    /// <param name="messages">Initial messages to add to the collection.</param>
    public MessageCollection(IEnumerable<Message> messages)
        : base(messages, GetSortKey, MessageSortParametr.SortOrder, MatchesFilter, GetSearchValues, GetGroupKey) { }

    /// <summary>Creates a grouping snapshot using the shared GroupNode model.</summary>
    /// <param name="parameters">Message fields used to create the hierarchy, in outer-to-inner order.</param>
    /// <returns>The resulting generic group nodes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when no grouping parameters are supplied.</exception>
    public IReadOnlyList<GroupNode<Message, GroupMessageParametrs>> GroupBy(params GroupMessageParametrs[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (parameters.Length == 0)
            throw new ArgumentException("At least one grouping parameter must be specified.", nameof(parameters));

        return FilteredItems.Group(parameters, GetGroupKey).ToList();
    }

    private static bool MatchesFilter(Message message, MessageFilterRule rule)
    {
        object? value = rule.Value;
        return rule.WhereParametr switch
        {
            MessageFilterParametr.Type => MatchesType(message, value),
            MessageFilterParametr.IsImportant => message.IsImportant == ReadBoolean(value),
            MessageFilterParametr.IsRead => message.IsRead == ReadBoolean(value),
            MessageFilterParametr.IsStatus => (message.Status.Count > 0) == ReadBoolean(value),
            MessageFilterParametr.ContainStatus => ContainsStatus(message, value),
            MessageFilterParametr.IsImage => (message.Images.Count > 0) == ReadBoolean(value),
            MessageFilterParametr.IsLink => (message.Links.Count > 0) == ReadBoolean(value),
            _ => throw new ArgumentOutOfRangeException(nameof(rule.WhereParametr), rule.WhereParametr, "Unsupported message filter parameter.")
        };
    }

    private static bool MatchesType(Message message, object? value)
    {
        if (value is null) return true;
        if (value is IEnumerable<MessageType> types)
            return types.Any(type => string.Equals(message.Type.Name, type.Name, StringComparison.OrdinalIgnoreCase));
        if (value is IEnumerable<string> names)
            return names.Any(name => string.Equals(message.Type.Name, name, StringComparison.OrdinalIgnoreCase));

        string expected = value is MessageType messageType ? messageType.Name : value.ToString() ?? string.Empty;
        return string.Equals(message.Type.Name, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsStatus(Message message, object? value)
    {
        if (message.Status.Count == 0 || value is null) return false;
        string expected = value.ToString() ?? string.Empty;
        return expected.Length > 0
            && message.Status.Any(status => status.Name.Contains(expected, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ReadBoolean(object? value)
    {
        if (value is null) return true;
        if (value is bool boolean) return boolean;
        if (value is string text && bool.TryParse(text, out bool parsed)) return parsed;
        return Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IEnumerable<string?> GetSearchValues(Message message, MessageSearchRule rule) => rule.WhereParametr switch
    {
        MessageSearchParametr.Title => new string?[] { message.Title },
        MessageSearchParametr.Description => new string?[] { message.Description },
        MessageSearchParametr.TextStatuses => message.Status.Select(status => (string?)status.Name),
        _ => throw new ArgumentOutOfRangeException(nameof(rule.WhereParametr), rule.WhereParametr, "Unsupported message search parameter.")
    };

    private static object? GetGroupKey(Message message, GroupMessageParametrs parameter) => parameter switch
    {
        GroupMessageParametrs.Type => message.Type.Name,
        GroupMessageParametrs.IsRead => message.IsRead,
        GroupMessageParametrs.IsImportant => message.IsImportant,
        _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter, "Unsupported message grouping parameter.")
    };

    private static object? GetSortKey(Message message, MessageSortParametr parameter) => parameter switch
    {
        MessageSortParametr.SortOrder => message.SortOrder,
        MessageSortParametr.Id => message.Id,
        MessageSortParametr.Title => message.Title,
        MessageSortParametr.Type => message.Type.Name,
        MessageSortParametr.Description => message.Description,
        MessageSortParametr.IsImportant => message.IsImportant,
        MessageSortParametr.IsRead => message.IsRead,
        MessageSortParametr.StatusCount => message.Status.Count,
        MessageSortParametr.ImageCount => message.Images.Count,
        MessageSortParametr.LinkCount => message.Links.Count,
        _ => message.SortOrder
    };
}
