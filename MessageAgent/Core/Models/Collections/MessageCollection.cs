using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GroupLib.Services;
using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Core.Models.Filtering;
using MessageAgent.Core.Models.Grouping;
using MessageAgent.Core.Models.SortRule;
using SortLib.Collections;
using WhereLib.Services;
using WhereLib.Models;

namespace MessageAgent.Core.Models.Collections;

/// <summary>
/// Observable message collection that maintains configurable sorting, filtering, searching
/// and hierarchical grouping. Filtering and searching create a view and do not remove source items.
/// </summary>
public sealed class MessageCollection : SortableObservableCollection<Message, MessageSortParametr, MessageSortRule>
{
    private readonly ObservableCollection<MessageFilterRule> filterRules = [];
    private readonly ObservableCollection<MessageSearchRule> searchRules = [];
    private readonly ObservableCollection<Message> filteredMessages = [];
    private readonly ReadOnlyObservableCollection<Message> filteredMessagesView;
    private readonly HashSet<MessageGroupRule> observedGroupRules = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<MessageFilterRule> observedFilterRules = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<MessageSearchRule> observedSearchRules = new(ReferenceEqualityComparer.Instance);

    private ObservableCollection<MessageGroupRule>? groupRules;
    private IReadOnlyList<MessageGroup> groups = Array.Empty<MessageGroup>();
    private SearchOptions searchOptions = new();

    /// <summary>
    /// Gets the grouping rules. Lower explicit LevelIndex values create outer levels first;
    /// unindexed rules follow indexed rules. If all indices are omitted, declaration order is used.
    /// Changes to this collection or its rules automatically rebuild <see cref="Groups"/>.
    /// </summary>
    public ObservableCollection<MessageGroupRule> GroupRules
    {
        get
        {
            if (groupRules is not null) return groupRules;

            groupRules = [];
            groupRules.CollectionChanged += GroupRulesOnCollectionChanged;

            return groupRules;
        }
    }

    /// <summary>
    /// Gets the filter rules. Rules are combined with AND by default.
    /// </summary>
    public ObservableCollection<MessageFilterRule> FilterRules => filterRules;

    /// <summary>
    /// Gets the text-search rules. Rules are combined with OR by default.
    /// </summary>
    public ObservableCollection<MessageSearchRule> SearchRules => searchRules;

    /// <summary>
    /// Gets or sets the search mode and matching options shared by all search rules.
    /// Changing any option immediately recalculates <see cref="FilteredMessages"/>.
    /// </summary>
    public SearchOptions SearchOptions
    {
        get => searchOptions;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(searchOptions, value)) return;

            searchOptions.PropertyChanged -= SearchOptionsOnPropertyChanged;
            searchOptions = value;
            searchOptions.PropertyChanged += SearchOptionsOnPropertyChanged;

            RefreshViews();
        }
    }

    /// <summary>
    /// Gets the current filtered and searched messages in the collection's current sort order.
    /// The returned collection is read-only; modify the source <see cref="MessageCollection"/> instead.
    /// </summary>
    public ReadOnlyObservableCollection<Message> FilteredMessages => filteredMessagesView;

    /// <summary>
    /// Gets the current hierarchical grouping snapshot built from <see cref="FilteredMessages"/>.
    /// It is empty when no grouping rules are configured.
    /// </summary>
    public IReadOnlyList<MessageGroup> Groups => groups;

    /// <summary>
    /// Creates an empty message collection with the default SortOrder rule.
    /// </summary>
    public MessageCollection() : base(GetSortKey, MessageSortParametr.SortOrder)
    {
        filteredMessagesView = new ReadOnlyObservableCollection<Message>(filteredMessages);

        InitializeViews();
    }

    /// <summary>
    /// Creates a sorted message collection from an existing sequence.
    /// </summary>
    /// <param name="messages">Messages to add.</param>
    public MessageCollection(IEnumerable<Message> messages) : base(messages, GetSortKey, MessageSortParametr.SortOrder)
    {
        filteredMessagesView = new ReadOnlyObservableCollection<Message>(filteredMessages);

        InitializeViews();
    }

    /// <summary>
    /// Creates a hierarchical snapshot using the supplied grouping levels.
    /// Grouping is performed against the current filtered result.
    /// </summary>
    /// <param name="parameters">Grouping levels, from outermost to innermost.</param>
    /// <returns>Root groups in the order in which their values occur in the filtered result.</returns>
    /// <exception cref="ArgumentNullException">The grouping parameter array is null.</exception>
    /// <exception cref="ArgumentException">No grouping level was supplied or a level is repeated.</exception>
    public IReadOnlyList<MessageGroup> GroupBy(params GroupMessageParametrs[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Length == 0) throw new ArgumentException("At least one grouping parameter must be specified.", nameof(parameters));

        return FilteredMessages.Group(parameters, GetGroupKey).Select(node => new MessageGroup(node)).ToList();
    }

    /// <summary>
    /// Rebuilds the grouping snapshot after an external dependency used by a grouping key changes.
    /// </summary>
    public void RefreshGroups() => UpdateGroups();

    /// <summary>
    /// Recalculates the filtered result and dependent grouping after external state changes.
    /// </summary>
    public void RefreshWhere() => RefreshViews();

    /// <inheritdoc />
    protected override void InsertItem(int index, Message item)
    {
        ArgumentNullException.ThrowIfNull(item);

        base.InsertItem(index, item);
        item.PropertyChanged += MessageOnPropertyChanged;
    }

    /// <inheritdoc />
    protected override void SetItem(int index, Message item)
    {
        ArgumentNullException.ThrowIfNull(item);

        this[index].PropertyChanged -= MessageOnPropertyChanged;
        base.SetItem(index, item);
        item.PropertyChanged += MessageOnPropertyChanged;
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        this[index].PropertyChanged -= MessageOnPropertyChanged;
        base.RemoveItem(index);
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        foreach (Message message in this)  message.PropertyChanged -= MessageOnPropertyChanged;

        base.ClearItems();
    }

    /// <inheritdoc />
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        RefreshViews();
    }

    private void InitializeViews()
    {
        _ = GroupRules;

        filterRules.CollectionChanged += FilterRulesOnCollectionChanged;
        searchRules.CollectionChanged += SearchRulesOnCollectionChanged;
        searchOptions.PropertyChanged += SearchOptionsOnPropertyChanged;

        foreach (Message message in this) message.PropertyChanged += MessageOnPropertyChanged;

        RefreshViews();
    }

    private void MessageOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshViews();

    private void GroupRulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (MessageGroupRule rule in observedGroupRules) rule.PropertyChanged -= GroupRuleOnPropertyChanged;

        observedGroupRules.Clear();

        foreach (MessageGroupRule rule in GroupRules)
        {
            if (observedGroupRules.Add(rule)) rule.PropertyChanged += GroupRuleOnPropertyChanged;
        }

        UpdateGroups();
    }

    private void GroupRuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateGroups();

    private void FilterRulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (MessageFilterRule rule in observedFilterRules) rule.PropertyChanged -= FilterRuleOnPropertyChanged;

        observedFilterRules.Clear();

        foreach (MessageFilterRule rule in FilterRules)
        {
            if (observedFilterRules.Add(rule)) rule.PropertyChanged += FilterRuleOnPropertyChanged;
        }

        RefreshViews();
    }

    private void FilterRuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshViews();

    private void SearchRulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (MessageSearchRule rule in observedSearchRules) rule.PropertyChanged -= SearchRuleOnPropertyChanged;

        observedSearchRules.Clear();

        foreach (MessageSearchRule rule in SearchRules)
        {
            if (observedSearchRules.Add(rule)) rule.PropertyChanged += SearchRuleOnPropertyChanged;
        }

        RefreshViews();
    }

    private void SearchRuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshViews();

    private void SearchOptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshViews();

    private void RefreshViews()
    {
        UpdateFilteredMessages();
        UpdateGroups();
    }

    private void UpdateFilteredMessages()
    {
        IReadOnlyList<Message> result =
            this.Apply<Message, MessageFilterParametr, MessageSearchParametr, MessageFilterRule, MessageSearchRule>
                (FilterRules, MatchesFilter, SearchRules, GetSearchValue, SearchOptions);

        SyncMessages(result);
    }

    private void UpdateGroups()
    {
        groups = groupRules is { Count: > 0 }
            ? FilteredMessages.Group(groupRules, GetGroupKey).Select(node => new MessageGroup(node)).ToList()
            : Array.Empty<MessageGroup>();

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Groups)));
    }

    private void SyncMessages(IReadOnlyList<Message> desired)
    {
        for (int targetIndex = 0; targetIndex < desired.Count; targetIndex++)
        {
            if (targetIndex < filteredMessages.Count && ReferenceEquals(filteredMessages[targetIndex], desired[targetIndex])) continue;

            int existingIndex = IndexOfReference(desired[targetIndex], targetIndex + 1);

            if (existingIndex >= 0) filteredMessages.Move(existingIndex, targetIndex);
            else filteredMessages.Insert(targetIndex, desired[targetIndex]);
        }

        while (filteredMessages.Count > desired.Count) filteredMessages.RemoveAt(filteredMessages.Count - 1);
    }

    private int IndexOfReference(Message message, int startIndex)
    {
        for (int index = startIndex; index < filteredMessages.Count; index++)
        {
            if (ReferenceEquals(filteredMessages[index], message)) return index;
        }

        return -1;
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

        string expectedName = value is MessageType messageType ? messageType.Name : value.ToString() ?? string.Empty;

        return string.Equals(message.Type.Name, expectedName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsStatus(Message message, object? value)
    {
        if (message.Status.Count == 0 || value is null) return false;

        string expectedText = value.ToString() ?? string.Empty;

        if (expectedText.Length == 0) return false;

        return message.Status.Any(status => status.Name.Contains(expectedText, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ReadBoolean(object? value)
    {
        if (value is null) return true;
        if (value is bool boolean) return boolean;
        if (value is string text && bool.TryParse(text, out bool parsed)) return parsed;

        return Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string? GetSearchValue(Message message, MessageSearchRule rule) => rule.WhereParametr switch
    {
        MessageSearchParametr.Title => message.Title,
        MessageSearchParametr.Description => message.Description,
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
