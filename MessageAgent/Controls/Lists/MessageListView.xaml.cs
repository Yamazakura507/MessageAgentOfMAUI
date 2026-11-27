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
using MessageAgent.Controls.Lists.Abstract;
using CollectionLib.Enums;

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
        RegisterThemedBackgroundSurface(
            TypeFiltersBorder,
            MessageAgentResources.GetColor(MessageAgentResources.TypeFiltersSurfaceLightKey),
            MessageAgentResources.GetColor(MessageAgentResources.TypeFiltersSurfaceDarkKey));
        UpdateViewOptionStates();
        UpdateAppliedIndicators();
    }

    /// <summary>
    /// Provides application-level image resource keys as fallbacks for shared list icons.
    /// </summary>
    /// <param name="resourceKey">The resource key requested by the shared collection-list control.</param>
    /// <returns>The resource keys to try when resolving the image.</returns>
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

    /// <summary>
    /// Opens the sorting options popup.
    /// </summary>
    protected override void OnSortRequested() => ShowPopup(PopupKind.Sort);

    /// <summary>
    /// Opens the filtering options popup.
    /// </summary>
    protected override void OnFilterRequested() => ShowPopup(PopupKind.Filter);

    /// <summary>
    /// Opens the grouping options popup.
    /// </summary>
    protected override void OnGroupRequested() => ShowPopup(PopupKind.Group);

    /// <summary>
    /// Opens the view-mode options popup.
    /// </summary>
    protected override void OnViewRequested() => ShowPopup(PopupKind.View);

    /// <summary>
    /// Opens the search configuration popup.
    /// </summary>
    protected override void OnSearchOptionsRequested() => ShowPopup(PopupKind.SearchOptions);

    /// <summary>
    /// Opens the central context popup at the requested location.
    /// </summary>
    /// <param name="position">The optional location used to position the popup.</param>
    protected override void OnCentralContextRequested(Point? position) => ShowPopup(PopupKind.Central, position: position);

    /// <summary>
    /// Closes the active popup when its backdrop is activated.
    /// </summary>
    protected override void OnPopupBackdropRequested() => HidePopup();

    /// <summary>
    /// Clears collection-specific type and status filter state before the source collection is replaced.
    /// </summary>
    /// <param name="previous">The collection currently assigned to the control, if any.</param>
    /// <param name="current">The collection that is about to be assigned, if any.</param>
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

    /// <summary>
    /// Refreshes type metadata, type filters, search indicators, and applied-filter indicators after source replacement.
    /// </summary>
    protected override void OnItemsSourceReplaced()
    {
        RefreshTypeMetadata();
        RenderTypeFilters();
        RenderSearchFieldIndicators();
        UpdateAppliedIndicators();
    }

    /// <summary>
    /// Refreshes the available message types and applied-filter indicators after the source collection changes.
    /// </summary>
    protected override void OnSourceCollectionChanged()
    {
        RefreshTypeMetadata();
        RenderTypeFilters();
        UpdateAppliedIndicators();
    }

    /// <summary>
    /// Updates type metadata and filter indicators when a source message property changes.
    /// </summary>
    /// <param name="message">The message whose property changed.</param>
    /// <param name="e">Details about the changed property.</param>
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

    private bool AreAllTypesSelected() =>
        availableTypes.Count > 0
        && selectedTypeNames.Count == availableTypes.Count
        && availableTypes.All(item => selectedTypeNames.Contains(item.Type.Name));

    private void ApplyTypeFilter()
    {
        if (ItemsSource is null) return;

        bool allSelected = AreAllTypesSelected();

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
        double rowSpacing = TypeRowOne.Spacing;
        double rowOneWidth = 42;
        double rowTwoWidth = 0;
        List<(MessageType Type, int Count, double Width)> firstRow = [];
        List<(MessageType Type, int Count, double Width)> secondRow = [];
        List<(MessageType Type, int Count, double Width)> overflow = [];

        foreach ((MessageType type, int count) in availableTypes)
        {
            double chipWidth = EstimateTypeChipWidth(type);

            if (rowOneWidth + chipWidth + rowSpacing <= availableWidth)
            {
                firstRow.Add((type, count, chipWidth));

                rowOneWidth += chipWidth + rowSpacing;
            }
            else if (rowTwoWidth + chipWidth + rowSpacing <= availableWidth)
            {
                secondRow.Add((type, count, chipWidth));

                rowTwoWidth += chipWidth + rowSpacing;
            }
            else
            {
                overflow.Add((type, count, chipWidth));
            }
        }

        if (overflow.Count > 0)
        {
            while (secondRow.Count > 0 && rowTwoWidth + moreButtonWidth + rowSpacing > availableWidth)
            {
                (MessageType Type, int Count, double Width) last = secondRow[^1];

                secondRow.RemoveAt(secondRow.Count - 1);

                rowTwoWidth -= last.Width + rowSpacing;

                overflow.Insert(0, (last.Type, last.Count, last.Width));
            }

        }

        foreach ((MessageType Type, int Count, double Width) item in firstRow) 
            TypeRowOne.Children.Add(CreateTypeChip(item.Type, item.Count, selectedTypeNames.Contains(item.Type.Name)));
        foreach ((MessageType Type, int Count, double Width) item in secondRow) 
            TypeRowTwo.Children.Add(CreateTypeChip(item.Type, item.Count, selectedTypeNames.Contains(item.Type.Name)));

        if (overflow.Count > 0)
        {
            ImageButton moreButton = CreateIconButton(CollectionListImageKeys.ListMore, "list_more.svg", "Больше типов сообщений", moreButtonWidth);
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
        bool allSelected = AreAllTypesSelected();

        MessageTypeActionButtonView button = new();

        button.Configure(GetIcon(MessageAgentImageKeys.AllMessageTypes, "all_message_types.svg"), "Все типы сообщений", allSelected);

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
        bool allSelected = AreAllTypesSelected();

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

    /// <summary>
    /// Recalculates the message-type chip layout when the available header width changes.
    /// </summary>
    protected override void OnAdditionalHeaderSizeChanged()
    {
        if (Math.Abs(TypeRowsHost.Width - lastRenderedTypeWidth) > 1) RenderTypeFilters();
    }

    /// <summary>
    /// Populates the overflow popup with the available message types.
    /// </summary>
    protected override void OnBuildTypesContext()
    {
        if (availableTypes.Count == 0)
        {
            AddPopupText("В коллекции пока нет сообщений.");

            return;
        }

        foreach ((MessageType type, int count) in availableTypes)
            PopupContent.Children.Add(CreateTypeChip(type, count, selectedTypeNames.Contains(type.Name), fullWidth: true));
    }

    /// <summary>
    /// Adds the status-text filter section to the shared filtering popup.
    /// </summary>
    protected override void OnAppendAdditionalFilterOptions()
    {
        AddCollapsibleSection("Статусы", section =>
        {
            MessageStatusTextFilterView statusFilterView = new()
            {
                Text = statusFilterText,
                LabelColor = PopupTextColor
            };
            statusFilterView.TextChanged += (_, args) =>
            {
                statusFilterText = args.NewTextValue ?? string.Empty;
                SetStatusTextFilter(statusFilterText);
            };

            section.Children.Add(statusFilterView);
        }, addDivider: false,
        isAvailable: () => HasMessagesWithStatuses() || !string.IsNullOrWhiteSpace(statusFilterText));
    }

    /// <summary>
    /// Resets the message-type and status-text filters maintained by this control.
    /// </summary>
    protected override void ClearAdditionalFilterState()
    {
        typeFilterRule = null;
        statusFilterText = string.Empty;

        selectedTypeNames.Clear();

        foreach ((MessageType type, _) in availableTypes) selectedTypeNames.Add(type.Name);

        ApplyTypeFilter();
        RenderTypeFilters();
    }

    /// <summary>
    /// Shows the message-type filter row only when the collection contains messages and available types.
    /// </summary>
    /// <param name="hasItems">Indicates whether the source collection contains items.</param>
    protected override void OnAdditionalControlsAvailabilityChanged(bool hasItems)
        => TypeFiltersBorder.IsVisible = hasItems && availableTypes.Count > 0;

    /// <summary>
    /// Determines whether the specified message search field can currently be used.
    /// </summary>
    /// <param name="parameter">The search field to check.</param>
    /// <returns><see langword="true"/> when the field is available for the current collection.</returns>
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

    /// <summary>
    /// Determines whether the specified message sort field can currently be used.
    /// </summary>
    /// <param name="parameter">The sort field to check.</param>
    /// <returns><see langword="true"/> when the field is available for the current collection.</returns>
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

    /// <summary>
    /// Determines whether the specified message filter can currently be used.
    /// </summary>
    /// <param name="parameter">The filter field to check.</param>
    /// <returns><see langword="true"/> when the filter is available for the current collection.</returns>
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

    /// <summary>
    /// Creates the message provider used to render an individual collection item.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <returns>A provider configured to render the specified message.</returns>
    protected override MessageProvider CreateItemView(Message message) => new MessageProvider
    {
        Message = message,
        MessageShowType = MessageShowType.ShowLableAlways,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Center,
        MinimumHeightRequest = 36
    };

    /// <summary>
    /// Wraps a rendered message with desktop delete controls and a swipe-to-delete action.
    /// </summary>
    /// <param name="message">The message associated with the rendered view.</param>
    /// <param name="itemView">The view that renders the message.</param>
    /// <returns>The container that hosts the message view and its delete actions.</returns>
    protected override View WrapItemView(Message message, View itemView)
    {
        bool isMobile = DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS;

        ImageButton deleteButton = CreateIconButton(CollectionListImageKeys.ListDelete, "list_delete.svg", "Удалить сообщение", 32);
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
            BackgroundColor = MessageAgentResources.GetColor(Application.Current?.RequestedTheme == AppTheme.Dark
                ? MessageAgentResources.SwipeDeleteSurfaceDarkKey
                : MessageAgentResources.SwipeDeleteSurfaceLightKey)
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
