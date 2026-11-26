using LottieAgent.Interfaces;
using LottieAgent.Models;
using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Models;
using MessageAgent.Resources;
using MessageAgent.Resources.ExtensionResources;
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

    public ILottieJsonLoader JsonLoader { get; }
    public ILottieModifierProcessor ModifierProcessor { get; }
    public string DialogIconKey { get; }
    public bool ShowFooter { get; }
    public string AcceptText { get; }
    public string CancelText { get; }
    public Task<object?> Result => completion.Task;
    public Button AcceptButton => AcceptActionButton;

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

    public async ValueTask CompleteAsync(object? result)
    {
        if (!completion.TrySetResult(result)) return;

        await CloseAsync();
    }

    private async void CancelButtonOnClicked(object? sender, EventArgs e) => await CompleteAsync(null);
    private async void CloseButtonOnClicked(object? sender, TappedEventArgs e) => await CompleteAsync(null);

    protected override bool OnBackButtonPressed()
    {
        _ = CompleteAsync(null);

        return true;
    }

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

    protected override void OnDisappearing()
    {
        closeAnimationInteraction.Dispose();
        base.OnDisappearing();
    }

    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.ScaleToAsync(0.96, 150, Easing.CubicIn));
}
