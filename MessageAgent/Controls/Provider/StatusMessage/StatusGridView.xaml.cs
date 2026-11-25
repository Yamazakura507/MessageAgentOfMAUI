using System.Collections.Specialized;
using MessageAgent.Core.Extensions;
using MessageAgent.Core.Models;
using MessageAgent.Core.Views;

namespace MessageAgent.Controls.Provider.StatusMessage;

/// <summary>
/// Responsive status grid. Every child receives an explicit measured width so that
/// native MAUI layouts cannot expand a status beyond the available row width.
/// In paged mode it initially displays two rows and expands/collapses in two-row steps.
/// </summary>
public partial class StatusGridView : ContentView
{
    private int visibleRows = 2;
    private bool showAll;
    private bool isRebuilding;
    private INotifyCollectionChanged? observedCollection;

    /// <summary>
    /// Bindable property for <see cref="Items"/>.
    /// </summary>
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IEnumerable<Status>), typeof(StatusGridView), default(IEnumerable<Status>), propertyChanged: OnItemsChanged);

    /// <summary>
    /// Bindable property for <see cref="AccentColor"/>.
    /// </summary>
    public static readonly BindableProperty AccentColorProperty =
        BindableProperty.Create(nameof(AccentColor), typeof(Color), typeof(StatusGridView), Colors.Gray, propertyChanged: OnAccentColorChanged);

    /// <summary>
    /// Bindable property for <see cref="IsPaged"/>.
    /// </summary>
    public static readonly BindableProperty IsPagedProperty =
        BindableProperty.Create(nameof(IsPaged), typeof(bool), typeof(StatusGridView), true, propertyChanged: OnPagingChanged);

    /// <summary>
    /// Gets or sets the ordered statuses rendered by the grid.
    /// </summary>
    public IEnumerable<Status>? Items
    {
        get => (IEnumerable<Status>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>
    /// Gets or sets the message accent used to color status outlines.
    /// </summary>
    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the grid initially limits itself to two rows.
    /// </summary>
    public bool IsPaged
    {
        get => (bool)GetValue(IsPagedProperty);
        set => SetValue(IsPagedProperty, value);
    }

    /// <summary>
    /// Initializes the responsive status grid.
    /// </summary>
    public StatusGridView()
    {
        InitializeComponent();
        SizeChanged += OnGridSizeChanged;
    }

    private static void OnItemsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        StatusGridView view = (StatusGridView)bindable;

        view.AttachItems(newValue as IEnumerable<Status>);

        view.visibleRows = 2;
        view.showAll = false;

        view.RebuildRows();
    }

    private static void OnAccentColorChanged(BindableObject bindable, object oldValue, object newValue) => ((StatusGridView)bindable).RebuildRows();

    private static void OnPagingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        StatusGridView view = (StatusGridView)bindable;

        view.visibleRows = 2;
        view.showAll = false;
        view.RebuildRows();
    }

    private void AttachItems(IEnumerable<Status>? items)
    {
        if (observedCollection is not null) observedCollection.CollectionChanged -= ItemsOnCollectionChanged;

        observedCollection = items as INotifyCollectionChanged;

        if (observedCollection is not null) observedCollection.CollectionChanged += ItemsOnCollectionChanged;
    }

    private void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildRows();

    private void OnGridSizeChanged(object? sender, EventArgs e) => RebuildRows();

    private List<Status> GetSortedItems() => Items?.ToList() ?? [];

    private void RebuildRows()
    {
        if (isRebuilding || RowsPanel is null || Width <= 0) return;

        isRebuilding = true;

        try
        {
            RowsPanel.Children.Clear();

            List<Status> statuses = GetSortedItems();

            if (statuses.Count == 0) return;

            double availableWidth = Math.Max(80, Width - StatusGridLayoutCalculator.HorizontalSafetyInset);
            List<List<StatusGridItem>> rows;
            int nextIndex = 0;

            void addRows(bool showAll)
            {
                rows = statuses.PackRows(int.MaxValue, availableWidth, 0, out nextIndex);

                AddRows(rows, showMore: false, showCollapse: showAll && rows.Count > 2, availableWidth);
            }

            if (!IsPaged)
            {
                addRows(false);

                return;
            }

            if (showAll)
            {
                addRows(true);

                return;
            }

            int rowLimit = Math.Max(2, visibleRows);
            List<List<StatusGridItem>> preliminaryRows = statuses.PackRows(rowLimit, availableWidth, 0, out int preliminaryNextIndex);

            bool preliminaryHasMore = preliminaryNextIndex < statuses.Count;
            bool preliminaryCanCollapse = rowLimit > 2 && preliminaryRows.Count > 2;
            double reservedWidth =
                (preliminaryHasMore ? StatusGridLayoutCalculator.MoreButtonWidth : 0) +
                (preliminaryCanCollapse ? StatusGridLayoutCalculator.CollapseButtonWidth : 0);

            rows = reservedWidth > 0
                ? statuses.PackRows(rowLimit, availableWidth, reservedWidth, out nextIndex)
                : preliminaryRows;

            if (reservedWidth <= 0) nextIndex = preliminaryNextIndex;

            bool hasMore = nextIndex < statuses.Count;
            bool hasCollapse = rowLimit > 2 && rows.Count > 2;
            double actualReservedWidth =
                (hasMore ? StatusGridLayoutCalculator.MoreButtonWidth : 0) +
                (hasCollapse ? StatusGridLayoutCalculator.CollapseButtonWidth : 0);

            if (Math.Abs(actualReservedWidth - reservedWidth) > 0.5)
            {
                rows = statuses.PackRows(rowLimit, availableWidth, actualReservedWidth, out nextIndex);
                hasMore = nextIndex < statuses.Count;
                hasCollapse = rowLimit > 2 && rows.Count > 2;
            }

            AddRows(rows, hasMore, hasCollapse, availableWidth);
        }
        finally
        {
            isRebuilding = false;
        }
    }

    private StatusActionButton CreateCollapseButton() => new()
        {
            Text = "Свернуть",
            AnimationSource = "Lotties/minimize.json",
            ToolTipText = "Свернуть",
            Margin = new Thickness(0, 0, 0, 6),
            VerticalOptions = LayoutOptions.Center
        };

    private void AddRows(IReadOnlyList<List<StatusGridItem>> rows, bool showMore, bool showCollapse, double availableWidth)
    {
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            bool isLastRow = rowIndex == rows.Count - 1;

            StatusGridRowView row = new();
            row.ItemsHost.Spacing = StatusGridLayoutCalculator.StatusSpacing;
            row.ItemsHost.WidthRequest = availableWidth;

            foreach (StatusGridItem item in rows[rowIndex])
            {
                StatusValueView statusView = new ()
                {
                    Status = item.Status,
                    AlwaysShowTitle = true,
                    MessageAccentColor = AccentColor,
                    HorizontalOptions = LayoutOptions.Start,
                    Margin = new Thickness(0, 0, 0, 6)
                };

                if (item.MustTruncateTitle)
                {
                    statusView.MaxTitleWidth = item.TitleWidth;
                    statusView.WidthRequest = item.Width;
                    statusView.MaximumWidthRequest = item.Width;
                }

                row.ItemsHost.Children.Add(statusView);
            }

            if (isLastRow && showMore)
            {
                StatusActionButton more = new ()
                {
                    Text = "...",
                    ToolTipText = "Показать больше",
                    Margin = new Thickness(0, 0, 0, 6),
                    VerticalOptions = LayoutOptions.Center
                };

                more.Clicked += (_, _) =>
                {
                    visibleRows += 2;

                    RebuildRows();
                };
                more.DoubleOrLongPressed += (_, _) =>
                {
                    showAll = true;

                    RebuildRows();
                };
                row.ItemsHost.Children.Add(more);
            }

            if (isLastRow && showCollapse && !showAll)
            {
                StatusActionButton collapse = CreateCollapseButton();

                collapse.Clicked += (_, _) =>
                {
                    visibleRows = Math.Max(2, visibleRows - 2);

                    RebuildRows();
                };
                collapse.DoubleOrLongPressed += (_, _) =>
                {
                    visibleRows = 2;
                    showAll = false;

                    RebuildRows();
                };
                row.ItemsHost.Children.Add(collapse);
            }

            RowsPanel.Children.Add(row);
        }

        if (showAll && showCollapse && rows.Count > 2)
        {
            StatusActionButton collapse = CreateCollapseButton();

            collapse.Clicked += (_, _) =>
            {
                showAll = false;
                visibleRows = Math.Max(2, rows.Count - 2);
                RebuildRows();
            };
            collapse.DoubleOrLongPressed += (_, _) =>
            {
                showAll = false;
                visibleRows = 2;
                RebuildRows();
            };

            StatusGridRowView actionRow = new();
            actionRow.ItemsHost.Spacing = StatusGridLayoutCalculator.StatusSpacing;
            actionRow.ItemsHost.Children.Add(collapse);
            RowsPanel.Children.Add(actionRow);
        }
    }

    /// <inheritdoc />
    protected override void OnParentSet()
    {
        base.OnParentSet();

        AttachItems(Parent is null ? null : Items);

        if (Parent is not null) RebuildRows();
    }
}
