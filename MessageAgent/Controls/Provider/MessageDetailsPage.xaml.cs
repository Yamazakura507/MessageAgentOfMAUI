using LottieAgent.Interfaces;
using LottieAgent.Models;
using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Models;
using MessageAgent.Services.Extensions;
using MessageAgent.Services.Factory;
using MessageAgent.Services.Interactions;
using MessageAgent.Services.Interfaces;
using MessageAgent.Core.Enums.Link;
using MessageAgent.Resources;
using MessageAgent.Resources.ExtensionResources;
using ResourcesLib.Models;
using ResourcesLib.Resolvers;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Modal view over a message, including its statuses, links and attachments.
/// </summary>
public partial class MessageDetailsPage : AnimatedModalPage
{
    private readonly LottiePlaybackInteraction closeAnimationInteraction;
    private readonly LottiePlaybackInteraction headerCloseAnimationInteraction;
    private readonly LottiePlaybackInteraction previousArrowInteraction;
    private readonly LottiePlaybackInteraction nextArrowInteraction;
    private readonly IVisualValueViewFactory visualValueViewFactory;

    /// <summary>
    /// Gets the JSON loader used by dialog Lottie views.
    /// </summary>
    public ILottieJsonLoader JsonLoader { get; }

    /// <summary>
    /// Gets the modifier processor used by dialog Lottie views.
    /// </summary>
    public ILottieModifierProcessor ModifierProcessor { get; }

    /// <summary>
    /// Gets the contrast-adjusted accent color used for dialog details and links.
    /// </summary>
    public Color DialogAccentColor { get; }

    /// <summary>
    /// Gets whether the read-state badge is displayed. Provider-backed dialogs show it;
    /// standalone message dialogs hide it because they do not participate in a notification lifecycle.
    /// </summary>
    public bool ShowReadState { get; }

    /// <summary>
    /// Initializes the message details dialog and its rendering dependencies.
    /// </summary>
    public MessageDetailsPage(
        Message message,
        ILottieJsonLoader jsonLoader,
        ILottieModifierProcessor modifierProcessor,
        IVisualValueViewFactory visualValueViewFactory,
        bool showReadState = true)
    {
        ArgumentNullException.ThrowIfNull(message);

        JsonLoader = jsonLoader ?? throw new ArgumentNullException(nameof(jsonLoader));
        ModifierProcessor = modifierProcessor ?? throw new ArgumentNullException(nameof(modifierProcessor));
        this.visualValueViewFactory = visualValueViewFactory ?? throw new ArgumentNullException(nameof(visualValueViewFactory));

        DialogAccentColor = message.Type.GetDialogAccent();
        ShowReadState = showReadState;

        LottieResourceSettings arrowSettings = LottieResourceResolver.GetSettings(
            MessageAgentLottieKeys.Arrow,
            defaultResources: DefaultResourceDictionary.Instance,
            defaultRestingProgress: 0.6622d,
            defaultModifiers: new LottieModifierCollection().SetColor("Stroke 1", DialogAccentColor),
            defaultSpeed: 1.25d,
            mergeModifiers: MessageAgentLottieModifiers.Merge);

        LottieResourceSettings closeSettings = MessageAgentLottieSettingsFactory.GetCloseSettings(message.Type.GetAccentContent());
        LottieResourceSettings headerCloseSettings = MessageAgentLottieSettingsFactory.GetCloseSettings(DialogAccentColor);

        InitializeComponent();
        BindingContext = message;

        MessageAgentLottieSettingsFactory.Apply(PreviousImageArrow, arrowSettings);
        MessageAgentLottieSettingsFactory.Apply(NextImageArrow, arrowSettings);
        NextImageArrow.RotationY = 180;

        MessageAgentLottieSettingsFactory.Apply(CloseAnimation, closeSettings);
        MessageAgentLottieSettingsFactory.Apply(HeaderCloseAnimation, headerCloseSettings);

        closeAnimationInteraction = new LottiePlaybackInteraction(
            CloseAnimation,
            CloseButton,
            closeSettings.RestingProgress!.Value,
            resetToRestingOnDeactivate: true);

        headerCloseAnimationInteraction = new LottiePlaybackInteraction(
            HeaderCloseAnimation,
            HeaderCloseButton,
            headerCloseSettings.RestingProgress!.Value,
            resetToRestingOnDeactivate: true);

        previousArrowInteraction = new LottiePlaybackInteraction(
            PreviousImageArrow,
            restingProgress: arrowSettings.RestingProgress!.Value,
            activateOnHover: true,
            resetToRestingOnDeactivate: true);

        nextArrowInteraction = new LottiePlaybackInteraction(
            NextImageArrow,
            restingProgress: arrowSettings.RestingProgress!.Value,
            activateOnHover: true,
            resetToRestingOnDeactivate: true);

        UpdateCarouselNavigation(0);
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Overlay.Opacity = 0;
        DetailsPanel.Opacity = 0;
        DetailsPanel.Scale = 0.94;

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 160, Easing.CubicOut),
            DetailsPanel.FadeToAsync(1, 210, Easing.CubicOut),
            DetailsPanel.ScaleToAsync(1, 230, Easing.CubicOut));
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        closeAnimationInteraction.Dispose();
        headerCloseAnimationInteraction.Dispose();
        previousArrowInteraction.Dispose();
        nextArrowInteraction.Dispose();
        base.OnDisappearing();
    }

    private async void CloseButtonOnClicked(object? sender, EventArgs e) => await CloseAsync();

    private void ImagesCarouselOnPositionChanged(object? sender, PositionChangedEventArgs e) => UpdateCarouselNavigation(e.CurrentPosition);

    private void UpdateCarouselNavigation(int position)
    {
        if (PreviousImageArrow is null || NextImageArrow is null || ImagesCarousel is null) return;

        int count = (BindingContext as Message)?.Images.Count ?? 0;
        int currentPosition = count == 0 ? 0 : Math.Clamp(position, 0, count - 1);
        bool canGoPrevious = currentPosition > 0;
        bool canGoNext = currentPosition < count - 1;

        PreviousImageArrow.Opacity = canGoPrevious ? 1 : 0.28;
        PreviousImageArrow.IsEnabled = canGoPrevious;
        NextImageArrow.Opacity = canGoNext ? 1 : 0.28;
        NextImageArrow.IsEnabled = canGoNext;

        if (!canGoPrevious) previousArrowInteraction.SetActive(false);
        if (!canGoNext) nextArrowInteraction.SetActive(false);
    }

    private void PreviousImageOnTapped(object? sender, TappedEventArgs e)
    {
        if (ImagesCarousel.Position > 0) ImagesCarousel.Position = Math.Max(0, ImagesCarousel.Position - 1);
    }

    private void NextImageOnTapped(object? sender, TappedEventArgs e)
    {
        int count = (BindingContext as Message)?.Images.Count ?? 0;

        if (count > 0 && ImagesCarousel.Position < count - 1) ImagesCarousel.Position = Math.Min(count - 1, ImagesCarousel.Position + 1);
    }

    private async void LinkOnTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Element { BindingContext: MessageLink link }) return;

        try
        {
            await link.ActivateAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Message link activation failed: {exception}");
        }
    }

    private void LinkRowOnBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not Grid { BindingContext: MessageLink link } row) return;

        ToolTipProperties.SetText(row, link.Description ?? string.Empty);

        ContentView? host = row.FindByName<ContentView>("LinkVisualHost");

        if (host is null) return;

        host.Content = null;

        if (link.Value is not null)
        {
            host.Content = visualValueViewFactory.Create(link.Value);

            return;
        }

        if (link.Type != MessageLinkType.Web
            || !Uri.TryCreate(link.Path, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return;

        Image favicon = new()
        {
            WidthRequest = 24,
            HeightRequest = 24,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center,
            Source = ImageSource.FromUri(new Uri($"https://www.google.com/s2/favicons?domain={uri.Host}&sz=64"))
        };

        SemanticProperties.SetDescription(favicon, link.Title ?? uri.Host);

        host.Content = favicon;
    }

    /// <inheritdoc />
    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 130, Easing.CubicIn),
        DetailsPanel.FadeToAsync(0, 130, Easing.CubicIn),
        DetailsPanel.ScaleToAsync(0.96, 150, Easing.CubicIn));
}
