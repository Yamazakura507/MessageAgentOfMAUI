using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Models;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Shared XAML shell for message-backed input and question dialogs.
/// </summary>
public partial class MessagePromptPage : AnimatedModalPage
{
    private readonly TaskCompletionSource<object?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ImageSource DialogIcon { get; }
    public Task<object?> Result => completion.Task;
    public Button AcceptButton => AcceptActionButton;

    public MessagePromptPage(
        Message message,
        ImageSource dialogIcon,
        View promptContent,
        bool showFooter = true,
        string acceptText = "Подтвердить",
        string cancelText = "Отмена")
    {
        ArgumentNullException.ThrowIfNull(message);
        DialogIcon = dialogIcon ?? throw new ArgumentNullException(nameof(dialogIcon));
        ArgumentNullException.ThrowIfNull(promptContent);

        InitializeComponent();
        BindingContext = message;
        PromptContent.Content = promptContent;
        FooterButtons.IsVisible = showFooter;
        AcceptActionButton.Text = acceptText;
        CancelActionButton.Text = cancelText;
    }

    public async ValueTask CompleteAsync(object? result)
    {
        if (!completion.TrySetResult(result)) return;

        await CloseAsync();
    }

    private async void CancelButtonOnClicked(object? sender, EventArgs e) => await CompleteAsync(null);
    private async void CloseButtonOnClicked(object? sender, EventArgs e) => await CompleteAsync(null);

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

    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.ScaleToAsync(0.96, 150, Easing.CubicIn));
}