using System.ComponentModel;
using LottieAgent.Interfaces;
using MessageAgent.Core.Enums.Message;
using MessageAgent.Core.Extensions;
using MessageAgent.Core.Models;
using MessageAgent.Services;
using MessageAgent.Services.Actions;
using MessageAgent.Services.Extensions;
using MessageAgent.Services.Interfaces;
using MessageAgent.Services.Models;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Compact notification view. Visual templates and status rendering live in XAML and handlers.
/// </summary>
public partial class MessageProvider : ContentView
{
    private const int NoLengthLimit = -1;
    private const int DefaultStatusLenght = 5;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LongPressDuration = TimeSpan.FromMilliseconds(650);

    private readonly IMessageLabelAnimator labelAnimator;
    private readonly IMessageAutoHideService autoHideService;
    private readonly IMessageDetailsPageFactory detailsPageFactory;
    private readonly IMessagePresentationService presentationService;

    private bool hasDetails;
    private bool isLabelVisible;
    private bool isLottieLoaded;
    private bool shouldStartAnimation;
    private bool detailsDialogOpened;
    private bool isOpeningDialog;
    private bool isStatusOverflowOpened;
    private bool hasStatusOverflow;
    private IReadOnlyList<StatusPresentationItem> visibleStatuses = Array.Empty<StatusPresentationItem>();

    private readonly NativeLongPressGesture nativeLongPressGesture;

    #if WINDOWS
    private bool ignoreNextLottieTap;
    #endif

    /// <summary>
    /// Bindable property for <see cref="Message"/>.
    /// </summary>
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(Message), typeof(MessageProvider), default(Message), propertyChanged: OnMessageChanged);

    /// <summary>
    /// Bindable property for <see cref="MaxTitleLength"/>.
    /// </summary>
    public static readonly BindableProperty MaxTitleLengthProperty =
        BindableProperty.Create(nameof(MaxTitleLength), typeof(int), typeof(MessageProvider), NoLengthLimit,
            validateValue: (_, value) => value is int length && (length == NoLengthLimit || length >= 3),
            propertyChanged: OnMaxTitleLengthChanged);

    /// <summary>
    /// Bindable property for <see cref="MaxStatusesOnLabel"/>.
    /// </summary>
    public static readonly BindableProperty MaxStatusesOnLabelProperty =
        BindableProperty.Create(nameof(MaxStatusesOnLabel), typeof(int), typeof(MessageProvider), DefaultStatusLenght,
            validateValue: (_, value) => value is int count && count >= 0,
            propertyChanged: OnMaxStatusesOnLabelChanged);

    /// <summary>
    /// Bindable property for <see cref="MessageShowType"/>.
    /// </summary>
    public static readonly BindableProperty MessageShowTypeProperty =
        BindableProperty.Create(nameof(MessageShowType), typeof(MessageShowType), typeof(MessageProvider), MessageShowType.ShowLableAlways, propertyChanged: OnMessageShowTypeChanged);

    /// <summary>
    /// Bindable property for <see cref="Timeout"/>.
    /// </summary>
    public static readonly BindableProperty TimeoutProperty =
        BindableProperty.Create(nameof(Timeout), typeof(TimeSpan), typeof(MessageProvider), DefaultTimeout,
            validateValue: (_, value) => value is TimeSpan timeout && timeout > TimeSpan.Zero,
            propertyChanged: OnTimeoutChanged);

    /// <summary>
    /// Bindable property for <see cref="LabelEffect"/>.
    /// </summary>
    public static readonly BindableProperty LabelEffectProperty =
        BindableProperty.Create(nameof(LabelEffect), typeof(MessageLabelEffect), typeof(MessageProvider), MessageLabelEffect.FadeScale);

    /// <summary>
    /// Bindable property for <see cref="LabelShape"/>.
    /// </summary>
    public static readonly BindableProperty LabelShapeProperty =
        BindableProperty.Create(nameof(LabelShape), typeof(MessageLabelShape), typeof(MessageProvider), MessageLabelShape.Block, propertyChanged: OnLabelShapeChanged);

    /// <summary>
    /// Gets the Lottie JSON loader used by the notification animation.
    /// </summary>
    public ILottieJsonLoader JsonLoader { get; }
    /// <summary>
    /// Gets the modifier processor used by the notification animation.
    /// </summary>
    public ILottieModifierProcessor ModifierProcessor { get; }

    /// <summary>
    /// Gets the ordered subset of statuses currently displayed on the compact label.
    /// </summary>
    public IReadOnlyList<StatusPresentationItem> VisibleStatuses => visibleStatuses;
    /// <summary>
    /// Gets whether the current message has details that can be opened.
    /// </summary>
    public bool HasDetails => hasDetails;
    /// <summary>
    /// Gets whether some statuses are hidden behind the overflow action.
    /// </summary>
    public bool HasStatusOverflow => hasStatusOverflow;

    /// <summary>
    /// Gets or sets the message presented by this control.
    /// </summary>
    public Message? Message
    {
        get => (Message?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum title length; null disables truncation.
    /// </summary>
    public int? MaxTitleLength
    {
        get
        {
            int value = (int)GetValue(MaxTitleLengthProperty);

            return value == NoLengthLimit ? null : value;
        }
        set => SetValue(MaxTitleLengthProperty, value ?? NoLengthLimit);
    }

    /// <summary>
    /// Maximum number of real status badges displayed on the compact label.
    /// Zero moves every status to the overflow context.
    /// </summary>
    public int MaxStatusesOnLabel
    {
        get => (int)GetValue(MaxStatusesOnLabelProperty);
        set => SetValue(MaxStatusesOnLabelProperty, value);
    }

    /// <summary>
    /// Gets or sets the label visibility and interaction mode.
    /// </summary>
    public MessageShowType MessageShowType
    {
        get => (MessageShowType)GetValue(MessageShowTypeProperty);
        set => SetValue(MessageShowTypeProperty, value);
    }

    /// <summary>
    /// Gets or sets the auto-hide delay for click-to-show mode.
    /// </summary>
    public TimeSpan Timeout
    {
        get => (TimeSpan)GetValue(TimeoutProperty);
        set => SetValue(TimeoutProperty, value);
    }

    /// <summary>
    /// Gets or sets the visual transition used when the label appears or disappears.
    /// </summary>
    public MessageLabelEffect LabelEffect
    {
        get => (MessageLabelEffect)GetValue(LabelEffectProperty);
        set => SetValue(LabelEffectProperty, value);
    }

    /// <summary>
    /// Geometry of the compact label. Block preserves the original rounded rectangle.
    /// </summary>
    public MessageLabelShape LabelShape
    {
        get => (MessageLabelShape)GetValue(LabelShapeProperty);
        set => SetValue(LabelShapeProperty, value);
    }

    /// <summary>
    /// Initializes the provider for XAML use. Dependencies are resolved from the application service container.
    /// </summary>
    public MessageProvider() : this(
        MauiServiceProvider.GetRequiredService<ILottieJsonLoader>(),
        MauiServiceProvider.GetRequiredService<ILottieModifierProcessor>(),
        MauiServiceProvider.GetRequiredService<IMessagePresentationService>(),
        MauiServiceProvider.GetRequiredService<IMessageLabelAnimator>(),
        MauiServiceProvider.GetRequiredService<IMessageAutoHideService>(),
        MauiServiceProvider.GetRequiredService<IMessageDetailsPageFactory>())
    {

    }

    /// <summary>
    /// Initializes the provider with explicit rendering, presentation and interaction services.
    /// </summary>
    /// <param name="jsonLoader">Loads Lottie assets.</param>
    /// <param name="modifierProcessor">Applies Lottie modifiers.</param>
    /// <param name="presentationService">Calculates the visible message presentation.</param>
    /// <param name="labelAnimator">Animates the compact label.</param>
    /// <param name="autoHideService">Controls the auto-hide timer.</param>
    /// <param name="detailsPageFactory">Creates the message details page.</param>
    public MessageProvider(
        ILottieJsonLoader jsonLoader,
        ILottieModifierProcessor modifierProcessor,
        IMessagePresentationService presentationService,
        IMessageLabelAnimator labelAnimator,
        IMessageAutoHideService autoHideService,
        IMessageDetailsPageFactory detailsPageFactory)
    {
        JsonLoader = jsonLoader ?? throw new ArgumentNullException(nameof(jsonLoader));
        ModifierProcessor = modifierProcessor ?? throw new ArgumentNullException(nameof(modifierProcessor));
        this.presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));
        this.labelAnimator = labelAnimator ?? throw new ArgumentNullException(nameof(labelAnimator));
        this.autoHideService = autoHideService ?? throw new ArgumentNullException(nameof(autoHideService));
        this.detailsPageFactory = detailsPageFactory ?? throw new ArgumentNullException(nameof(detailsPageFactory));

        InitializeComponent();

        nativeLongPressGesture = new NativeLongPressGesture(LottieProvider, OnLottieLongPressed, LongPressDuration);
        LottieProvider.AnimationFailed += exception => System.Diagnostics.Debug.WriteLine(exception);
        autoHideService.Elapsed += AutoHideServiceOnElapsed;
    }

    private static void OnLabelShapeChanged(BindableObject bindable, object oldValue, object newValue) => ((MessageProvider)bindable).UpdatePresentation();

    private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        MessageProvider provider = (MessageProvider)bindable;

        if (oldValue is Message oldMessage) oldMessage.PropertyChanged -= provider.MessageOnPropertyChanged;
        if (newValue is Message newMessage) newMessage.PropertyChanged += provider.MessageOnPropertyChanged;

        provider.detailsDialogOpened = false;
        provider.isLabelVisible = false;
        provider.shouldStartAnimation = newValue is Message;

        provider.StopAutoHideTimer();
        provider.LottieProvider.Stop();
        provider.UpdateAppearance();
        provider.ApplyLabelMode(false);

        if (provider.isLottieLoaded && provider.shouldStartAnimation) provider.StartNotificationAnimation();
    }

    private static void OnMaxTitleLengthChanged(BindableObject bindable, object oldValue, object newValue)
    {
        MessageProvider provider = (MessageProvider)bindable;

        provider.UpdatePresentation();
    }

    private static void OnMaxStatusesOnLabelChanged(BindableObject bindable, object oldValue, object newValue) => ((MessageProvider)bindable).UpdatePresentation();

    private static void OnMessageShowTypeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        MessageProvider provider = (MessageProvider)bindable;

        provider.ApplyLabelMode(provider.Handler is not null);
        provider.UpdatePresentation();
    }

    private static void OnTimeoutChanged(BindableObject bindable, object oldValue, object newValue)
    {
        MessageProvider provider = (MessageProvider)bindable;

        if (provider.MessageShowType == MessageShowType.ShowByClickAndAutoHide && provider.isLabelVisible) provider.RestartAutoHideTimer();
    }

    private void MessageOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => Dispatcher.Dispatch(() =>
    {
        if (Message is null || !ReferenceEquals(sender, Message)) return;

        bool textChanged = e.PropertyName is nameof(Message.Title) or nameof(Message.Description);

        if (textChanged)
        {
            Message.IsRead = false;
            shouldStartAnimation = true;
        }

        UpdatePresentation();

        if (textChanged && isLottieLoaded) StartNotificationAnimation();
    });

    private void UpdateAppearance()
    {
        IsVisible = Message is not null;

        UpdatePresentation();
    }

    private void ApplyLabelMode(bool animated)
    {
        StopAutoHideTimer();

        bool shouldBeVisible = Message is not null && MessageShowType == MessageShowType.ShowLableAlways;

        _ = SetLabelVisibilityAsync(shouldBeVisible, animated);
    }

    private async ValueTask SetLabelVisibilityAsync(bool visible, bool animated = true)
    {
        if (Message is null || MessageShowType == MessageShowType.HideLableAlways) visible = false;
        else if (MessageShowType == MessageShowType.ShowLableAlways) visible = true;

        if (visible == isLabelVisible && BrMessage.IsVisible == visible) return;

        isLabelVisible = visible;

        await labelAnimator.SetVisibilityAsync(BrMessage, visible, LabelEffect, animated);

        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        MessagePresentationState state = presentationService.Evaluate(
            Message,
            MessageShowType,
            isLabelVisible,
            detailsDialogOpened,
            MaxTitleLength,
            MaxStatusesOnLabel,
            Width,
            BrMessage.Width,
            BrMessage.Padding,
            TitleLabel.Width,
            TitleLabel.FontSize,
            TitleLabel.FontFamily,
            TitleLabel.FontAttributes,
            TitleLabel.CharacterSpacing);

        if (!visibleStatuses.SequenceEqual(state.VisibleStatuses))
        {
            visibleStatuses = state.VisibleStatuses;

            OnPropertyChanged(nameof(VisibleStatuses));
        }

        SetHasDetails(state.HasDetails);
        SetHasStatusOverflow(state.HasStatusOverflow);

        LottieProvider.IsLooping = state.ShouldLoopNotification;
    }

    private void SetHasDetails(bool value)
    {
        if (hasDetails == value) return;

        hasDetails = value;

        OnPropertyChanged(nameof(HasDetails));
    }

    private void SetHasStatusOverflow(bool value)
    {
        if (hasStatusOverflow == value) return;

        hasStatusOverflow = value;

        OnPropertyChanged(nameof(HasStatusOverflow));
    }

    private async void StatusMoreButtonOnClicked(object? sender, EventArgs e)
    {
        if (isStatusOverflowOpened || Message is null || !hasStatusOverflow) return;

        List<Status> orderedStatuses = Message.Status.Distinct<Status>(ReferenceEqualityComparer.Instance).ToList();

        if (orderedStatuses.Count == 0) return;

        Page? page = this.FindHostPage();

        if (page is null) return;

        isStatusOverflowOpened = true;
        try
        {
            Rect anchorBounds = StatusMoreButton.GetBoundsRelativeToPage(page);

            await page.Navigation.PushModalAsync(new StatusOverflowPage(orderedStatuses, Message.Type.GetAccentColor(), anchorBounds), animated: false);
        }
        finally
        {
            isStatusOverflowOpened = false;
        }
    }

    private void LottieProviderOnAnimationLoaded(object? sender, EventArgs e)
    {
        isLottieLoaded = true;

        if (shouldStartAnimation) StartNotificationAnimation();
    }

    private void StartNotificationAnimation()
    {
        shouldStartAnimation = false;

        if (Message is null || Message.IsRead)
        {
            LottieProvider.IsPlaying = false;
            LottieProvider.Progress = 1;

            return;
        }

        UpdatePresentation();

        LottieProvider.Progress = 0;

        LottieProvider.Play();
    }

    private void LottieProviderOnAnimationCompleted(object? sender, EventArgs e)
    {
        if (Message is null || Message.IsRead) return;

        Message.IsRead = true;

        UpdatePresentation();
    }

    private void LottieProviderOnTapped(object? sender, TappedEventArgs e)
    {
        #if WINDOWS
        if (ignoreNextLottieTap)
        {
            ignoreNextLottieTap = false;

            return;
        }
        #endif

        switch (MessageShowType)
        {
            case MessageShowType.ShowLableAlways:
                if (HasDetails) _ = OpenDetailsAsync();
                break;
            case MessageShowType.ShowAndHideByClick:
                _ = SetLabelVisibilityAsync(!isLabelVisible);
                break;
            case MessageShowType.ShowByClickAndAutoHide:
                _ = SetLabelVisibilityAsync(true);
                RestartAutoHideTimer();
                break;
            case MessageShowType.HideLableAlways:
                _ = OpenDetailsAsync(force: true);
                break;
        }
    }

    private void LottieProviderOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (HasDetails || MessageShowType == MessageShowType.HideLableAlways)
            _ = OpenDetailsAsync(force: MessageShowType == MessageShowType.HideLableAlways);
    }

    private void MessageLabelOnTapped(object? sender, TappedEventArgs e)
    {
        if (MessageShowType != MessageShowType.HideLableAlways && HasDetails) _ = OpenDetailsAsync();
    }

    private async Task OpenDetailsAsync(bool force = false)
    {
        if (isOpeningDialog || Message is null) return;

        UpdatePresentation();

        if (!force && !HasDetails) return;

        Page? page = this.FindHostPage();

        if (page is null) return;

        isOpeningDialog = true;
        detailsDialogOpened = true;
        LottieProvider.IsLooping = false;

        try
        {
            MessageDetailsPage detailsPage = detailsPageFactory.Create(Message);

            await page.Navigation.PushModalAsync(detailsPage, animated: false);
        }
        finally
        {
            isOpeningDialog = false;
        }
    }

    private void RestartAutoHideTimer()
    {
        if (MessageShowType == MessageShowType.ShowByClickAndAutoHide && isLabelVisible) autoHideService.Start(Dispatcher, Timeout);
    }

    private void StopAutoHideTimer() => autoHideService.Stop();

    private void AutoHideServiceOnElapsed(object? sender, EventArgs e)
    {
        StopAutoHideTimer();
        _ = SetLabelVisibilityAsync(false);
    }

    private void UpdatePresentationOnSizeChanged(object? sender, EventArgs e) => UpdatePresentation();

    private void OnLottieLongPressed()
    {
        #if WINDOWS
        ignoreNextLottieTap = true;
        #endif

        _ = OpenDetailsAsync(force: MessageShowType == MessageShowType.HideLableAlways);
    }

    /// <inheritdoc />
    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent is null) StopAutoHideTimer();
    }
}
