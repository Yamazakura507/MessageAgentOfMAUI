using System.ComponentModel;
using CollectionLib.Collections;
using CollectionLib.Controls;
using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Models;
using MessageAgent.Core.Models.Collections;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Core.Models.Filtering;
using MessageAgent.Core.Models.Grouping;
using MessageAgent.Core.Models.SortRule;
using MessageAgent.Controls.Provider;
using MessageAgent.Resources.ExtensionResources;
using CollectionLib.Resources.ExtensionResources;

namespace MessageAgent.Controls.Lists;

/// <summary>
/// Interactive message collection view. The control edits the supplied MessageCollection rules
/// and renders each message through MessageProvider.
/// </summary>
public partial class MessageListView : MessageListViewBase
{

    private readonly HashSet<string> selectedTypeNames = new(StringComparer.OrdinalIgnoreCase);
    private List<(MessageType Type, int Count)> availableTypes = [];
    private MessageFilterRule? typeFilterRule;
    private bool hasInitializedTypes;
    private string statusFilterText = string.Empty;
    private double lastRenderedTypeWidth = -1;

    /// <summary>
    /// Identifies the <see cref="ItemsSource"/> bindable property.
    /// </summary>
    public new static readonly BindableProperty ItemsSourceProperty = MessageListViewBase.ItemsSourceProperty;

    /// <summary>
    /// Provides the historical MessageListView identifier for the shared header title property.
    /// </summary>
    public new static readonly BindableProperty TitleProperty = CollectionListViewBase.TitleProperty;
    /// <summary>
    /// Provides the historical MessageListView identifier for the shared header icon property.
    /// </summary>
    public new static readonly BindableProperty IconProperty = CollectionListViewBase.IconProperty;
    /// <summary>
    /// Provides the historical MessageListView identifier for the shared dock title property.
    /// </summary>
    public new static readonly BindableProperty DockTitleTextProperty = CollectionListViewBase.DockTitleTextProperty;

    /// <summary>
    /// Collection that owns the source messages and their filter, search, sort and group rules.
    /// </summary>
    public new MessageCollection? ItemsSource
    {
        get => (MessageCollection?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Initializes the message list and applies its default icons, header and view state.
    /// </summary>
    public MessageListView()
    {
        InitializeComponent();
        RegisterThemedBackgroundSurface(TypeFiltersBorder, Color.FromArgb("#F4F6F8"), Color.FromArgb("#2C3037"));
        UpdateViewOptionStates();
        UpdateAppliedIndicators();
    }

    protected override IEnumerable<string> GetAdditionalImageResourceKeys(string resourceKey) => GetApplicationImageResourceKeys(resourceKey);

    private static IEnumerable<string> GetApplicationImageResourceKeys(string resourceKey)
    {
        yield return resourceKey;

        string? legacyKey = resourceKey switch
        {
            CollectionListImageKeys.ListSearch => MessageAgentImageKeys.ListSearch,
            CollectionListImageKeys.ListSort => MessageAgentImageKeys.ListSort,
            CollectionListImageKeys.ListFilter => MessageAgentImageKeys.ListFilter,
            CollectionListImageKeys.ListGroup => MessageAgentImageKeys.ListGroup,
            CollectionListImageKeys.ListView => MessageAgentImageKeys.ListView,
            CollectionListImageKeys.ListImage => MessageAgentImageKeys.ListImage,
            CollectionListImageKeys.ListSearchOptions => MessageAgentImageKeys.ListSearchOptions,
            CollectionListImageKeys.ListSearchModeText => MessageAgentImageKeys.ListSearchModeText,
            CollectionListImageKeys.ListSearchModeFuzzy => MessageAgentImageKeys.ListSearchModeFuzzy,
            CollectionListImageKeys.ListSearchAccuracy => MessageAgentImageKeys.ListSearchAccuracy,
            CollectionListImageKeys.ListSearchCase => MessageAgentImageKeys.ListSearchCase,
            CollectionListImageKeys.ListMore => MessageAgentImageKeys.ListMore,
            CollectionListImageKeys.ListClose => MessageAgentImageKeys.ListClose,
            CollectionListImageKeys.ListBack => MessageAgentImageKeys.ListBack,
            CollectionListImageKeys.ListReset => MessageAgentImageKeys.ListReset,
            CollectionListImageKeys.ListChevronDown => MessageAgentImageKeys.ListChevronDown,
            CollectionListImageKeys.ListChevronRight => MessageAgentImageKeys.ListChevronRight,
            CollectionListImageKeys.ListDelete => MessageAgentImageKeys.ListDelete,
            CollectionListImageKeys.ListClearHistory => MessageAgentImageKeys.ListClearHistory,
            CollectionListImageKeys.ListStateEnabled => MessageAgentImageKeys.ListStateEnabled,
            CollectionListImageKeys.ListStateDisabled => MessageAgentImageKeys.ListStateDisabled,
            CollectionListImageKeys.ListStateAll => MessageAgentImageKeys.ListStateAll,
            CollectionListImageKeys.ListStateYes => MessageAgentImageKeys.ListStateYes,
            CollectionListImageKeys.ListStateNo => MessageAgentImageKeys.ListStateNo,
            CollectionListImageKeys.ListStateAscending => MessageAgentImageKeys.ListStateAscending,
            CollectionListImageKeys.ListStateDescending => MessageAgentImageKeys.ListStateDescending,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(legacyKey)) yield return legacyKey;
    }

    protected override void OnSortRequested() => ShowPopup(PopupKind.Sort);
    protected override void OnFilterRequested() => ShowPopup(PopupKind.Filter);
    protected override void OnGroupRequested() => ShowPopup(PopupKind.Group);
    protected override void OnViewRequested() => ShowPopup(PopupKind.View);
    protected override void OnSearchOptionsRequested() => ShowPopup(PopupKind.SearchOptions);
    protected override void OnCentralContextRequested(Point? position) => ShowPopup(PopupKind.Central, position: position);
    protected override void OnPopupBackdropRequested() => HidePopup();
    protected override void OnItemsSourceReplacing(
        CollectionViewCollection<
            Message, MessageFilterParametr, MessageSearchParametr, GroupMessageParametrs, MessageSortParametr,
            MessageFilterRule, MessageSearchRule, MessageGroupRule, MessageSortRule>? previous,
        CollectionViewCollection<
            Message, MessageFilterParametr, MessageSearchParametr, GroupMessageParametrs, MessageSortParametr,
            MessageFilterRule, MessageSearchRule, MessageGroupRule, MessageSortRule>? current)
    {
        if (previous is not null && typeFilterRule is not null) previous.FilterRules.Remove(typeFilterRule);

        typeFilterRule = null;
        statusFilterText = string.Empty;
        availableTypes.Clear();
        selectedTypeNames.Clear();
        hasInitializedTypes = false;
    }

    protected override void OnItemsSourceReplaced()
    {
        RefreshTypeMetadata();
        RenderTypeFilters();
        RenderSearchFieldIndicators();
        UpdateAppliedIndicators();
    }

    protected override void OnSourceCollectionChanged()
    {
        RefreshTypeMetadata();
        RenderTypeFilters();
        UpdateAppliedIndicators();
    }

    protected override void OnSourceItemPropertyChanged(Message message, PropertyChangedEventArgs e)
    {
        if (ItemsSource?.Contains(message) != true) return;

        if (e.PropertyName == nameof(Message.Type))
        {
            RefreshTypeMetadata();
            RenderTypeFilters();
        }

        UpdateAppliedIndicators();
    }

    private void RefreshTypeMetadata()
    {
        List<(MessageType Type, int Count)> previousTypes = availableTypes;
        bool hadAllSelected = previousTypes.Count > 0 && selectedTypeNames.SetEquals(previousTypes.Select(item => item.Type.Name));

        availableTypes = ItemsSource is null
            ? []
            : ItemsSource
                .GroupBy(message => message.Type.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => (group.First().Type, group.Count()))
                .OrderBy(item => item.Type.DisplayName ?? item.Type.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        HashSet<string> availableNames = new(availableTypes.Select(item => item.Type.Name), StringComparer.OrdinalIgnoreCase);

        if (!hasInitializedTypes || hadAllSelected)
        {
            selectedTypeNames.Clear();

            foreach ((MessageType type, _) in availableTypes) selectedTypeNames.Add(type.Name);

            hasInitializedTypes = true;
        }
        else
        {
            selectedTypeNames.RemoveWhere(name => !availableNames.Contains(name));
        }

        ApplyTypeFilter();
    }

    private void ApplyTypeFilter()
    {
        if (ItemsSource is null) return;

        bool allSelected = availableTypes.Count == selectedTypeNames.Count
            && availableTypes.All(item => selectedTypeNames.Contains(item.Type.Name));

        if (allSelected)
        {
            if (typeFilterRule is not null)
            {
                ItemsSource.FilterRules.Remove(typeFilterRule);
                typeFilterRule = null;
            }

            return;
        }

        string[] selectedNames = selectedTypeNames.ToArray();

        if (typeFilterRule is null)
        {
            typeFilterRule = new MessageFilterRule(MessageFilterParametr.Type, selectedNames);

            ItemsSource.FilterRules.Insert(0, typeFilterRule);
        }
        else
        {
            typeFilterRule.Value = selectedNames;
        }
    }

    private void RenderTypeFilters()
    {
        TypeRowOne.Children.Clear();
        TypeRowTwo.Children.Clear();
        TypeRowTwo.IsVisible = false;

        TypeRowOne.Children.Add(CreateAllTypesButton());

        if (availableTypes.Count == 0)
        {
            lastRenderedTypeWidth = TypeRowsHost.Width;

            return;
        }

        double availableWidth = TypeRowsHost.Width > 0 ? Math.Max(110, TypeRowsHost.Width - 12) : Math.Max(220, Width - 32);
        const double moreButtonWidth = 38;
        double rowOneWidth = 42;
        double rowTwoWidth = 0;
        List<(MessageType Type, int Count, double Width)> firstRow = [];
        List<(MessageType Type, int Count, double Width)> secondRow = [];
        List<(MessageType Type, int Count, double Width)> overflow = [];

        foreach ((MessageType type, int count) in availableTypes)
        {
            double chipWidth = EstimateTypeChipWidth(type);

            if (rowOneWidth + chipWidth + 5 <= availableWidth)
            {
                firstRow.Add((type, count, chipWidth));
                rowOneWidth += chipWidth + 5;
            }
            else if (rowTwoWidth + chipWidth + 5 <= availableWidth)
            {
                secondRow.Add((type, count, chipWidth));
                rowTwoWidth += chipWidth + 5;
            }
            else
            {
                overflow.Add((type, count, chipWidth));
            }
        }

        if (overflow.Count > 0)
        {
            while (secondRow.Count > 0 && rowTwoWidth + moreButtonWidth + 5 > availableWidth)
            {
                (MessageType Type, int Count, double Width) last = secondRow[^1];
                secondRow.RemoveAt(secondRow.Count - 1);
                rowTwoWidth -= last.Width + 5;
                overflow.Insert(0, (last.Type, last.Count, last.Width));
            }

            if (secondRow.Count == 0 && firstRow.Count == 0)
            {
                // The all-types action remains visible even on very narrow layouts.
            }
        }

        foreach ((MessageType Type, int Count, double Width) item in firstRow) 
            TypeRowOne.Children.Add(CreateTypeChip(item.Type, item.Count, selectedTypeNames.Contains(item.Type.Name)));
        foreach ((MessageType Type, int Count, double Width) item in secondRow) 
            TypeRowTwo.Children.Add(CreateTypeChip(item.Type, item.Count, selectedTypeNames.Contains(item.Type.Name)));

        if (overflow.Count > 0)
        {
            ImageButton moreButton = CreateIconButton(
                CollectionListImageKeys.ListMore,
                "list_more.svg",
                "Р‘РѕР»СЊС€Рµ С‚РёРїРѕРІ СЃРѕРѕР±С‰РµРЅРёР№",
                moreButtonWidth);
            moreButton.Clicked += (_, _) => ShowPopup(PopupKind.Types);
            TypeRowTwo.Children.Add(moreButton);
            TypeRowTwo.IsVisible = true;
        }
        else
        {
            TypeRowTwo.IsVisible = secondRow.Count > 0;
        }

        lastRenderedTypeWidth = TypeRowsHost.Width;
    }

    private static double EstimateTypeChipWidth(MessageType type)
    {
        string text = type.DisplayName ?? type.Name;
        double textWidth = Math.Clamp(text.Length * 6.6 + 12, 48, 138);

        return 22 + 24 + textWidth + 18;
    }

    private View CreateAllTypesButton()
    {
        bool allSelected = availableTypes.Count > 0
            && selectedTypeNames.Count == availableTypes.Count
            && availableTypes.All(item => selectedTypeNames.Contains(item.Type.Name));

        MessageTypeActionButtonView button = new();
        button.Configure(
            GetIcon(MessageAgentImageKeys.AllMessageTypes, "all_message_types.svg"),
            "All message types",
            allSelected);
        button.Clicked += (_, _) => ToggleAllTypes();
        return button;
    }

    private View CreateTypeChip(MessageType type, int count, bool selected, bool fullWidth = false)
    {
        MessageTypeChipView chip = new();
        chip.Configure(type, count, selected, fullWidth ? -1 : EstimateTypeChipWidth(type));
        chip.Selected += (_, _) => ToggleType(type.Name);
        return chip;
    }

    private void ToggleAllTypes()
    {
        bool allSelected = availableTypes.Count > 0
            && selectedTypeNames.Count == availableTypes.Count
            && availableTypes.All(item => selectedTypeNames.Contains(item.Type.Name));

        selectedTypeNames.Clear();

        if (!allSelected)
        {
            foreach ((MessageType type, _) in availableTypes) selectedTypeNames.Add(type.Name);
        }

        ApplyTypeFilter();
        RenderTypeFilters();
        RenderItems();
        UpdateAppliedIndicators();
    }

    private void ToggleType(string typeName)
    {
        if (!selectedTypeNames.Add(typeName)) selectedTypeNames.Remove(typeName);

        ApplyTypeFilter();
        RenderTypeFilters();
        RenderItems();
        UpdateAppliedIndicators();

        if (activePopup == PopupKind.Types) ShowPopup(PopupKind.Types);
    }

    private void DeleteMessage(Message message)
    {
        if (ItemsSource?.Contains(message) == true) ItemsSource.Remove(message);
    }

    protected override void OnAdditionalHeaderSizeChanged()
    {
        if (Math.Abs(TypeRowsHost.Width - lastRenderedTypeWidth) > 1) RenderTypeFilters();
    }

    protected override void OnBuildTypesContext()
    {
        if (availableTypes.Count == 0)
        {
            AddPopupText("Р’ РєРѕР»Р»РµРєС†РёРё РїРѕРєР° РЅРµС‚ СЃРѕРѕР±С‰РµРЅРёР№.");

            return;
        }

        foreach ((MessageType type, int count) in availableTypes)
            PopupContent.Children.Add(CreateTypeChip(type, count, selectedTypeNames.Contains(type.Name), fullWidth: true));
    }

    protected override void OnAppendAdditionalFilterOptions()
    {
        AddCollapsibleSection("Статусы", section =>
        {
            section.Children.Add(new Label
            {
                Text = "Название статуса содержит",
                FontSize = 12,
                Margin = new Thickness(2, 4, 2, 2),
                TextColor = PopupTextColor
            });

            Entry statusEntry = new()
            {
                Placeholder = "Текст статуса",
                Text = statusFilterText,
                FontSize = 13,
                ClearButtonVisibility = ClearButtonVisibility.WhileEditing
            };
            statusEntry.TextChanged += (_, args) =>
            {
                statusFilterText = args.NewTextValue ?? string.Empty;
                SetStatusTextFilter(statusFilterText);
            };
            section.Children.Add(statusEntry);
        }, addDivider: false,
        isAvailable: () => HasMessagesWithStatuses() || !string.IsNullOrWhiteSpace(statusFilterText));
    }

    protected override void ClearAdditionalFilterState()
    {
        typeFilterRule = null;
        statusFilterText = string.Empty;

        selectedTypeNames.Clear();

        foreach ((MessageType type, _) in availableTypes) selectedTypeNames.Add(type.Name);

        ApplyTypeFilter();
        RenderTypeFilters();
    }

    protected override void OnAdditionalControlsAvailabilityChanged(bool hasItems)
        => TypeFiltersBorder.IsVisible = hasItems && availableTypes.Count > 0;
    protected override bool IsSearchParameterAvailable(MessageSearchParametr parameter)
    {
        if (ItemsSource is null) return false;

        return parameter switch
        {
            MessageSearchParametr.Title => ItemsSource.Count > 0,
            MessageSearchParametr.Description => selectedSearchParameters.Contains(parameter)
                || ItemsSource.Any(message => !string.IsNullOrWhiteSpace(message.Description)),
            MessageSearchParametr.TextStatuses => selectedSearchParameters.Contains(parameter) || HasMessagesWithStatuses(),
            _ => false
        };
    }

    protected override bool IsSortParameterAvailable(MessageSortParametr parameter)
    {
        if (ItemsSource is null) return false;

        return parameter switch
        {
            MessageSortParametr.StatusCount => ItemsSource.SortRules.Any(rule => rule.SortParametr == parameter) || HasMessagesWithStatuses(),
            MessageSortParametr.ImageCount => ItemsSource.SortRules.Any(rule => rule.SortParametr == parameter) || ItemsSource.Any(message => message.Images.Count > 0),
            MessageSortParametr.LinkCount => ItemsSource.SortRules.Any(rule => rule.SortParametr == parameter) || ItemsSource.Any(message => message.Links.Count > 0),
            _ => ItemsSource.Count > 0
        };
    }

    protected override bool IsFilterParameterAvailable(MessageFilterParametr parameter)
    {
        if (ItemsSource is null) return false;

        return parameter switch
        {
            MessageFilterParametr.IsStatus => ownedFilterRules.ContainsKey(parameter) || HasMessagesWithStatuses(),
            MessageFilterParametr.IsImage => ownedFilterRules.ContainsKey(parameter) || ItemsSource.Any(message => message.Images.Count > 0),
            MessageFilterParametr.IsLink => ownedFilterRules.ContainsKey(parameter) || ItemsSource.Any(message => message.Links.Count > 0),
            _ => ItemsSource.Count > 0
        };
    }

    private bool HasMessagesWithStatuses()
        => ItemsSource?.Any(message => message.Status.Count > 0) ?? false || (statusFilterText.Length > 0 && ItemsSource is not null);

    private void SetStatusTextFilter(string value)
    {
        SetOwnedFilter(MessageFilterParametr.ContainStatus, string.IsNullOrWhiteSpace(value) ? null : value.Trim());
        UpdateAppliedIndicators();
    }

    protected override MessageProvider CreateItemView(Message message) => new MessageProvider
    {
        Message = message,
        MessageShowType = MessageShowType.ShowLableAlways,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Center,
        MinimumHeightRequest = 36
    };

    protected override View WrapItemView(Message message, View itemView)
    {
        bool isMobile = DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS;

        ImageButton deleteButton = CreateIconButton(
            CollectionListImageKeys.ListDelete, "list_delete.svg", "Удалить сообщение", 32);
        deleteButton.Opacity = 0.82;
        deleteButton.IsVisible = !isMobile;
        deleteButton.Clicked += (_, _) => DeleteMessage(message);

        Grid rowContent = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 5,
            VerticalOptions = LayoutOptions.Center
        };

        rowContent.Children.Add(deleteButton);
        Grid.SetColumn(itemView, 1);
        rowContent.Children.Add(itemView);

        SwipeItem deleteSwipeItem = new()
        {
            IconImageSource = GetIcon(CollectionListImageKeys.ListDelete, "list_delete.svg"),
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#713F49") : Color.FromArgb("#F2DDE0")
        };

        deleteSwipeItem.Invoked += (_, _) => DeleteMessage(message);
        SwipeItems rightSwipeItems = new() { Mode = SwipeMode.Reveal };

        rightSwipeItems.Add(deleteSwipeItem);

        return new SwipeView
        {
            Content = rowContent,
            RightItems = rightSwipeItems
        };
    }

}
