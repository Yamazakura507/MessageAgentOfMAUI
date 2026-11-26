using LottieAgent.Interfaces;
using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Models;
using MessageAgent.Services;
using MessageAgent.Services.Extensions;
using MessageAgent.Services.Factory;
using MessageAgent.Services.Interactions;
using ResourcesLib.Models;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Shared XAML shell for message-backed input and question dialogs.
/// </summary>
public partial class MessagePromptPage : AnimatedModalPage
{
    private readonly TaskCompletionSource<object?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly LottiePlaybackInteraction closeAnimationInteraction;

    /// <summary>
    /// Gets the service used to load the close-button Lottie animation.
    /// </summary>
    public ILottieJsonLoader JsonLoader { get; }
    /// <summary>
    /// Gets the service used to process Lottie animation modifiers.
    /// </summary>
    public ILottieModifierProcessor ModifierProcessor { get; }
    /// <summary>
    /// Gets the resource key of the dialog icon.
    /// </summary>
    public string DialogIconKey { get; }
    /// <summary>
    /// Gets a value indicating whether the dialog footer and its action buttons are displayed.
    /// </summary>
    public bool ShowFooter { get; }
    /// <summary>
    /// Gets the caption of the accept button.
    /// </summary>
    public string AcceptText { get; }
    /// <summary>
    /// Gets the caption of the cancel button.
    /// </summary>
    public string CancelText { get; }
    /// <summary>
    /// Gets the asynchronous dialog result; a <see langword="null"/> result represents cancellation.
    /// </summary>
    public Task<object?> Result => completion.Task;
    /// <summary>
    /// Gets the accept button hosted by the dialog.
    /// </summary>
    public Button AcceptButton => AcceptActionButton;

    /// <summary>
    /// Creates a modal prompt page that hosts the supplied prompt content.
    /// </summary>
    /// <param name="message">The message whose text and type define the dialog context.</param>
    /// <param name="dialogIconKey">The resource key used to resolve the dialog icon.</param>
    /// <param name="promptContent">The view displayed inside the dialog body.</param>
    /// <param name="showFooter">Indicates whether the footer action buttons are visible.</param>
    /// <param name="acceptText">The caption of the accept button.</param>
    /// <param name="cancelText">The caption of the cancel button.</param>
    /// <exception cref="ArgumentNullException">The message or prompt content is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The dialog icon key is empty or consists only of whitespace.</exception>
    public MessagePromptPage(
        Message message,
        string dialogIconKey,
        View promptContent,
        bool showFooter = true,
        string acceptText = "Подтвердить",
        string cancelText = "Отмена")
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(dialogIconKey);

        DialogIconKey = dialogIconKey;
        ShowFooter = showFooter;
        AcceptText = acceptText;
        CancelText = cancelText;

        ArgumentNullException.ThrowIfNull(promptContent);

        JsonLoader = MauiServiceProvider.GetRequiredService<ILottieJsonLoader>();
        ModifierProcessor = MauiServiceProvider.GetRequiredService<ILottieModifierProcessor>();

        InitializeComponent();

        BindingContext = message;
        PromptContent.Content = promptContent;

        LottieResourceSettings closeSettings = MessageAgentLottieSettingsFactory.GetCloseSettings(message.Type.GetDialogAccent());

        MessageAgentLottieSettingsFactory.Apply(HeaderCloseAnimation, closeSettings);
        closeAnimationInteraction = new LottiePlaybackInteraction(
            HeaderCloseAnimation,
            HeaderCloseButton,
            closeSettings.RestingProgress!.Value,
            resetToRestingOnDeactivate: true);
    }

    /// <summary>
    /// Completes the dialog with the supplied result and begins closing the page.
    /// </summary>
    /// <param name="result">The value returned through <see cref="Result"/>, or <see langword="null"/> when cancelled.</param>
    public async ValueTask CompleteAsync(object? result)
    {
        if (!completion.TrySetResult(result)) return;

        await CloseAsync();
    }

    private async void CancelButtonOnClicked(object? sender, EventArgs e) => await CompleteAsync(null);
    private async void CloseButtonOnClicked(object? sender, TappedEventArgs e) => await CompleteAsync(null);

    /// <summary>
    /// Completes the dialog as cancelled when the user navigates back.
    /// </summary>
    /// <returns><see langword="true"/> to indicate that the page handled the back action.</returns>
    protected override bool OnBackButtonPressed()
    {
        _ = CompleteAsync(null);

        return true;
    }

    /// <summary>
    /// Starts the entrance animation when the prompt page becomes visible.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Overlay.Opacity = 0;
        DialogPanel.Opacity = 0;
        DialogPanel.Scale = 0.94;

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 160, Easing.CubicOut),
            DialogPanel.FadeToAsync(1, 210, Easing.CubicOut),
            DialogPanel.ScaleToAsync(1, 230, Easing.CubicOut));
    }

    /// <summary>
    /// Releases the close-animation interaction when the prompt page disappears.
    /// </summary>
    protected override void OnDisappearing()
    {
        closeAnimationInteraction.Dispose();
        base.OnDisappearing();
    }

    /// <summary>
    /// Runs the exit animation for the prompt overlay and dialog panel.
    /// </summary>
    /// <returns>A task that completes when all exit animations finish.</returns>
    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.ScaleToAsync(0.96, 150, Easing.CubicIn));
}
