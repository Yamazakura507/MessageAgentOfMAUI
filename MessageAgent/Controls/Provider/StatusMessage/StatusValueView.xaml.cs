using System.ComponentModel;
using MessageAgent.Core.Enums.Status;
using MessageAgent.Core.Extensions;
using MessageAgent.Core.Models;
using MessageAgent.Services;
using MessageAgent.Services.Extensions;
using MessageAgent.Services.Interfaces;
using MessageAgent.Services.Models;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Displays one status value, its optional title and a color-coded outline.
/// A compact presentation can override ShowTitle without changing the model.
/// </summary>
public partial class StatusValueView : ContentView
{
    private readonly IVisualValueViewFactory viewFactory;
    private readonly IStatusBorderColorResolver borderColorResolver;
    private bool isDescriptionOpening;
    private int borderColorRequestVersion;

    #if ANDROID || IOS
    private NativeLongPressGesture? descriptionLongPressGesture;
    #endif

    /// <summary>
    /// Bindable property for <see cref="Status"/>.
    /// </summary>
    public static readonly BindableProperty StatusProperty =
        BindableProperty.Create(
            nameof(Status),
            typeof(Status),
            typeof(StatusValueView),
            default(Status),
            propertyChanged: OnStatusChanged);

    /// <summary>
    /// Bindable property for <see cref="AlwaysShowTitle"/>.
    /// </summary>
    public static readonly BindableProperty AlwaysShowTitleProperty =
        BindableProperty.Create(nameof(AlwaysShowTitle), typeof(bool), typeof(StatusValueView), false, propertyChanged: OnPresentationPropertyChanged);

    /// <summary>
    /// Bindable property for <see cref="ShowTitleOverride"/>.
    /// </summary>
    public static readonly BindableProperty ShowTitleOverrideProperty =
        BindableProperty.Create(nameof(ShowTitleOverride), typeof(bool?), typeof(StatusValueView), default(bool?), propertyChanged: OnPresentationPropertyChanged);

    /// <summary>
    /// Bindable property for <see cref="MessageAccentColor"/>.
    /// </summary>
    public static readonly BindableProperty MessageAccentColorProperty =
        BindableProperty.Create(nameof(MessageAccentColor), typeof(Color), typeof(StatusValueView), Colors.Gray, propertyChanged: OnAccentColorChanged);

    /// <summary>
    /// Bindable property for <see cref="MaxTitleWidth"/>.
    /// </summary>
    public static readonly BindableProperty MaxTitleWidthProperty =
        BindableProperty.Create(nameof(MaxTitleWidth), typeof(double), typeof(StatusValueView), double.PositiveInfinity, propertyChanged: OnTitleWidthChanged);

    /// <summary>
    /// Gets or sets the status whose visual value and title are displayed.
    /// </summary>
    public Status? Status
    {
        get => (Status?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>
    /// Forces the status title to be visible. Used by the details dialog.
    /// </summary>
    public bool AlwaysShowTitle
    {
        get => (bool)GetValue(AlwaysShowTitleProperty);
        set => SetValue(AlwaysShowTitleProperty, value);
    }

    /// <summary>
    /// Optional compact-layout override. Null uses Status.ShowTitle.
    /// </summary>
    public bool? ShowTitleOverride
    {
        get => (bool?)GetValue(ShowTitleOverrideProperty);
        set => SetValue(ShowTitleOverrideProperty, value);
    }

    /// <summary>
    /// Accent color of the containing message, used by StatusBorderColorMode.Label.
    /// </summary>
    public Color MessageAccentColor
    {
        get => (Color)GetValue(MessageAccentColorProperty);
        set => SetValue(MessageAccentColorProperty, value);
    }

    /// <summary>
    /// Maximum width of the status title. Used by wrapping status grids to avoid horizontal overflow.
    /// </summary>
    public double MaxTitleWidth
    {
        get => (double)GetValue(MaxTitleWidthProperty);
        set => SetValue(MaxTitleWidthProperty, value);
    }

    /// <summary>
    /// Constructor used when this control is created from XAML.
    /// </summary>
    public StatusValueView() : this(
        MauiServiceProvider.GetRequiredService<IVisualValueViewFactory>(),
        MauiServiceProvider.GetRequiredService<IStatusBorderColorResolver>())
    {

    }

    /// <summary>
    /// Initializes the status view with its visual-value factory and color resolver.
    /// </summary>
    /// <param name="viewFactory">Creates views for polymorphic visual values.</param>
    /// <param name="borderColorResolver">Resolves an optional color from the visual value.</param>
    public StatusValueView(IVisualValueViewFactory viewFactory, IStatusBorderColorResolver borderColorResolver)
    {
        this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        this.borderColorResolver = borderColorResolver ?? throw new ArgumentNullException(nameof(borderColorResolver));

        InitializeComponent();

        HandlerChanged += OnStatusViewHandlerChanged;

        RenderStatus();
    }

    private static void OnStatusChanged(BindableObject bindable, object oldValue, object newValue)
    {
        StatusValueView view = (StatusValueView)bindable;

        if (oldValue is Status oldStatus) oldStatus.PropertyChanged -= view.StatusOnPropertyChanged;
        if (newValue is Status newStatus) newStatus.PropertyChanged += view.StatusOnPropertyChanged;

        view.RenderStatus();
    }

    private void StatusOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Status.BorderColorMode)) Dispatcher.Dispatch(UpdateBorderColor);
    }

    private static void OnPresentationPropertyChanged(BindableObject bindable, object oldValue, object newValue) => ((StatusValueView)bindable).UpdateTitleVisibility();

    private static void OnAccentColorChanged(BindableObject bindable, object oldValue, object newValue) => ((StatusValueView)bindable).UpdateBorderColor();

    private static void OnTitleWidthChanged(BindableObject bindable, object oldValue, object newValue) => ((StatusValueView)bindable).UpdateTitleWidth();

    private void RenderStatus()
    {
        ValueHost.Content = null;
        NameLabel.Text = Status?.Name;

        bool hasValue = Status?.Value is not null;

        ValueHost.IsVisible = hasValue;

        Grid.SetColumn(NameLabel, hasValue ? 1 : 0);

        if (hasValue) ValueHost.Content = viewFactory.Create(Status!.Value!);

        UpdateTitleVisibility();
        UpdateDescriptionHint();
        UpdateBorderColor();
        RefreshNativeInput();
    }

    private void UpdateTitleVisibility()
    {
        bool hasValue = Status?.Value is not null;
        bool showName = Status is not null && (!hasValue || AlwaysShowTitle || (ShowTitleOverride ?? Status.ShowTitle));

        NameLabel.Text = Status?.Name;
        NameLabel.IsVisible = showName;

        Grid.SetColumn(NameLabel, hasValue ? 1 : 0);

        UpdateTitleWidth();
    }

    private void UpdateTitleWidth()
    {
        if (double.IsPositiveInfinity(MaxTitleWidth) || MaxTitleWidth <= 0)
        {
            NameLabel.WidthRequest = -1;
            NameLabel.MaximumWidthRequest = double.PositiveInfinity;

            return;
        }

        NameLabel.WidthRequest = MaxTitleWidth;
        NameLabel.MaximumWidthRequest = MaxTitleWidth;
    }

    private void UpdateBorderColor()
    {
        Status? status = Status;

        int requestVersion = ++borderColorRequestVersion;

        if (status is null || status.BorderColorMode == StatusBorderColorMode.None)
        {
            ApplyNeutralStatusPalette();

            return;
        }

        StatusBorder.StrokeThickness = 1;

        switch (status.BorderColorMode)
        {
            case StatusBorderColorMode.Label:
                ApplyStatusPalette(MessageAccentColor);
                return;
            case StatusBorderColorMode.Random:
                ApplyStatusPalette(StatusRandomColorStore.GetOrCreate(status));
                return;
            case StatusBorderColorMode.VisualElement:
                ApplyStatusPalette(StatusRandomColorStore.GetOrCreate(status));
                _ = ResolveVisualBorderColorAsync(status, requestVersion);
                return;
            default:
                ApplyStatusPalette(MessageAccentColor);
                return;
        }
    }

    private void ApplyNeutralStatusPalette()
    {
        StatusBorder.StrokeThickness = 0;
        StatusBorder.Stroke = new SolidColorBrush(Colors.Transparent);
        StatusBorder.BackgroundColor = Colors.Transparent;
        NameLabel.TextColor = MessageColorPalette.GetNeutralStatusTextColor();
    }

    private void ApplyStatusPalette(Color sourceColor)
    {
        StatusVisualPalette palette = sourceColor.CreateStatusPalette();

        StatusBorder.Stroke = new SolidColorBrush(palette.BorderColor);
        StatusBorder.BackgroundColor = palette.BackgroundColor;
        NameLabel.TextColor = palette.TextColor;
    }

    private async Task ResolveVisualBorderColorAsync(Status status, int requestVersion)
    {
        Color? color;

        try
        {
            color = await borderColorResolver.ResolveVisualColorAsync(status);
        }
        catch
        {
            color = null;
        }

        if (requestVersion != borderColorRequestVersion || !ReferenceEquals(Status, status)) return;

        Dispatcher.Dispatch(() =>
        {
            if (requestVersion != borderColorRequestVersion || !ReferenceEquals(Status, status)) return;

            ApplyStatusPalette(color ?? StatusRandomColorStore.GetOrCreate(status));
        });
    }

    private void UpdateDescriptionHint()
    {
        string description = Status?.Description ?? string.Empty;

        #if WINDOWS || MACCATALYST
        ToolTipHelper.SetText(this, description);
        ToolTipHelper.SetText(StatusBorder, description);
        ToolTipHelper.SetText(StatusLayout, description);
        ToolTipHelper.SetText(ValueHost, description);
        ToolTipHelper.SetText(NameLabel, description);
        #endif
    }

    private void OnStatusViewHandlerChanged(object? sender, EventArgs e)
    {
        UpdateDescriptionHint();
        RefreshNativeInput();
    }

    private void RefreshNativeInput()
    {
        #if ANDROID || IOS
        if (string.IsNullOrWhiteSpace(Status?.Description))
        {
            descriptionLongPressGesture?.Dispose();
            descriptionLongPressGesture = null;
            return;
        }

        descriptionLongPressGesture ??= new NativeLongPressGesture(
            this,
            () => _ = ShowDescriptionAsync(),
            TimeSpan.FromMilliseconds(550),
            cancelsTouchesInView: false);
        #endif
    }

    private async ValueTask ShowDescriptionAsync()
    {
        if (isDescriptionOpening || string.IsNullOrWhiteSpace(Status?.Description)) return;

        Page? page = this.FindHostPage();

        if (page is null) return;

        isDescriptionOpening = true;

        try
        {
            await page.Navigation.PushModalAsync(new StatusDescriptionPage(Status!.Name, Status.Description), animated: false);
        }
        finally
        {
            isDescriptionOpening = false;
        }
    }

    /// <inheritdoc />
    protected override void OnParentSet()
    {
        base.OnParentSet();

        #if ANDROID || IOS
        if (Parent is null)
        {
            descriptionLongPressGesture?.Dispose();
            descriptionLongPressGesture = null;
        }
        else
        {
            RefreshNativeInput();
        }
        #endif
    }
}
