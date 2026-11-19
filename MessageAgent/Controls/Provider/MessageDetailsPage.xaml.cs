using LottieAgent.Controls;
using LottieAgent.Interfaces;
using LottieAgent.Models;
using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Models;
using MessageAgent.Services.Extensions;
using MessageAgent.Services.Interactions;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Modal view over a Message. Its content is bound to the model in XAML.
/// </summary>
public partial class MessageDetailsPage : AnimatedModalPage
{
    private readonly LottiePlaybackInteraction closeAnimationInteraction;

    public ILottieJsonLoader JsonLoader { get; }
    public ILottieModifierProcessor ModifierProcessor { get; }

    public MessageDetailsPage(Message message, ILottieJsonLoader jsonLoader, ILottieModifierProcessor modifierProcessor)
    {
        ArgumentNullException.ThrowIfNull(message);

        JsonLoader = jsonLoader ?? throw new ArgumentNullException(nameof(jsonLoader));
        ModifierProcessor = modifierProcessor ?? throw new ArgumentNullException(nameof(modifierProcessor));

        InitializeComponent();

        BindingContext = message;

        Color closeTextColor = message.Type.GetAccentContent();

        CloseAnimation.Modifiers = new LottieModifierCollection().SetColor("Fill", closeTextColor);
        closeAnimationInteraction = new LottiePlaybackInteraction(CloseAnimation, CloseButton);
    }

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

    private void MessageIconOnLoaded(object? sender, EventArgs e) => LoadedAnimation(MessageIcon);

    private void CloseAnimationOnLoaded(object? sender, EventArgs e) => LoadedAnimation(CloseAnimation);

    private void LoadedAnimation(LottieView lottie)
    {
        lottie.IsPlaying = false;
        lottie.Progress = 1;
    }

    protected override void OnDisappearing()
    {
        closeAnimationInteraction.Dispose();
        base.OnDisappearing();
    }

    private async void CloseButtonOnClicked(object? sender, EventArgs e) => await CloseAsync();

    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 130, Easing.CubicIn),
        DetailsPanel.FadeToAsync(0, 130, Easing.CubicIn),
        DetailsPanel.ScaleToAsync(0.96, 150, Easing.CubicIn));
}
