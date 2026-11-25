using LottieAgent.Interfaces;
using LottieAgent.Models;
using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Models;
using MessageAgent.Services;
using MessageAgent.Services.Interactions;
using MessageAgent.Services.Extensions;
using MessageAgent.Resources.ExtensionResources;
using CollectionLib.Resources;
using CollectionLib.Resources.ExtensionResources;
using ResourcesLib.Models;
using ResourcesLib.Resolvers;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Transparent context popup anchored to the overflow action in the compact message label.
/// </summary>
public partial class StatusOverflowPage : AnimatedModalPage
{
    private const double PopupMargin = 12;
    private const double DesiredPopupWidth = 340;
    private const double DesiredPopupHeight = 320;

    private readonly Rect anchorBounds;
    private readonly LottiePlaybackInteraction closeAnimationInteraction;

    /// <summary>
    /// Gets the statuses shown in the overflow popup, in their display order.
    /// </summary>
    public IReadOnlyList<Status> Statuses { get; }
    /// <summary>
    /// Gets the accent color used by the popup.
    /// </summary>
    public Color AccentColor { get; }
    /// <summary>
    /// Gets the theme-aware popup surface color.
    /// </summary>
    public Color PanelBackgroundColor { get; }
    /// <summary>
    /// Gets the theme-aware popup foreground color.
    /// </summary>
    public Color ForegroundColor { get; }

    /// <summary>
    /// Initializes the popup anchored to the status overflow action.
    /// </summary>
    /// <param name="statuses">The statuses to display.</param>
    /// <param name="accentColor">The parent message accent color.</param>
    /// <param name="anchorBounds">The anchor bounds relative to the containing page.</param>
    public StatusOverflowPage(IEnumerable<Status> statuses, Color accentColor, Rect anchorBounds)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        Statuses = statuses.ToList();
        AccentColor = accentColor;
        this.anchorBounds = anchorBounds;

        bool dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        PanelBackgroundColor = MessageAgentResources.GetColor(dark
            ? MessageAgentResources.StatusOverflowSurfaceDarkKey
            : MessageAgentResources.StatusOverflowSurfaceLightKey);
        ForegroundColor = MessageAgentResources.GetColor(dark
            ? MessageAgentResources.StatusOverflowForegroundDarkKey
            : MessageAgentResources.StatusOverflowForegroundLightKey);

        LottieResourceSettings closeSettings = LottieResourceResolver.GetSettings(
            MessageAgentLottieKeys.Close,
            defaultResources: CollectionListDefaultResourceDictionary.Instance,
            defaultRestingProgress: 1d,
            defaultModifiers: new LottieModifierCollection().SetColor("Fill", ForegroundColor),
            applicationResourceKeys: new[] { CollectionListLottieKeys.Close, MessageAgentLottieKeys.Close },
            defaultResourceKeys: new[] { CollectionListLottieKeys.Close },
            mergeModifiers: MessageAgentLottieModifiers.Merge);

        InitializeComponent();
        BindingContext = this;

        CloseAnimation.JsonLoader = MauiServiceProvider.GetRequiredService<ILottieJsonLoader>();
        CloseAnimation.ModifierProcessor = MauiServiceProvider.GetRequiredService<ILottieModifierProcessor>();
        CloseAnimation.Source = closeSettings.Source;
        CloseAnimation.Modifiers = closeSettings.Modifiers as LottieModifierCollection ?? new LottieModifierCollection();
        CloseAnimation.Progress = closeSettings.RestingProgress ?? 1d;
        CloseAnimation.Speed = closeSettings.Speed ?? 1d;
        CloseAnimation.IsReversed = closeSettings.IsReversed ?? false;

        closeAnimationInteraction = new LottiePlaybackInteraction(
            CloseAnimation,
            CloseButton,
            closeSettings.RestingProgress!.Value,
            resetToRestingOnDeactivate: true);

        SizeChanged += OnPopupSizeChanged;
        Overlay.SizeChanged += OnPopupSizeChanged;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(16);
        PositionPopup();
        await Task.WhenAll(
            PopupPanel.FadeToAsync(1, 140, Easing.CubicOut),
            PopupPanel.ScaleToAsync(1, 160, Easing.CubicOut));
    }

    private void OnPopupSizeChanged(object? sender, EventArgs e) => PositionPopup();

    private void PositionPopup()
    {
        double pageWidth = Overlay.Width > 0 ? Overlay.Width : Width;
        double pageHeight = Overlay.Height > 0 ? Overlay.Height : Height;

        if (pageWidth <= 0 || pageHeight <= 0) return;

        double popupWidth = Math.Min(DesiredPopupWidth, Math.Max(120, pageWidth - PopupMargin * 2));
        double popupHeight = Math.Min(DesiredPopupHeight, Math.Max(140, pageHeight - PopupMargin * 2));

        PopupPanel.WidthRequest = popupWidth;
        PopupPanel.HeightRequest = popupHeight;
        double x = anchorBounds.Right - popupWidth;

        if (x < PopupMargin) x = anchorBounds.Left;

        x = Math.Clamp(x, PopupMargin, Math.Max(PopupMargin, pageWidth - popupWidth - PopupMargin));
        double y = anchorBounds.Bottom + 6;

        if (y + popupHeight > pageHeight - PopupMargin) y = anchorBounds.Top - popupHeight - 6;

        y = Math.Clamp(y, PopupMargin, Math.Max(PopupMargin, pageHeight - popupHeight - PopupMargin));

        AbsoluteLayout.SetLayoutBounds(PopupPanel, new Rect(x, y, popupWidth, popupHeight));
    }

    private void CloseAnimationOnLoaded(object? sender, EventArgs e)
    {
        CloseAnimation.IsPlaying = false;
        CloseAnimation.Progress = closeAnimationInteraction.RestingProgress;
        CloseAnimation.IsLooping = closeAnimationInteraction.IsActive;

        if (closeAnimationInteraction.IsActive) CloseAnimation.Play();
    }

    private async void BackdropOnTapped(object? sender, TappedEventArgs e) => await CloseAsync();

    private async void CloseButtonOnClicked(object? sender, EventArgs e) => await CloseAsync();

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        closeAnimationInteraction.Dispose();
        base.OnDisappearing();
    }

    /// <inheritdoc />
    protected override Task AnimateOutAsync() => Task.WhenAll(
        PopupPanel.FadeToAsync(0, 90, Easing.CubicIn),
        PopupPanel.ScaleToAsync(0.97, 110, Easing.CubicIn));
}
