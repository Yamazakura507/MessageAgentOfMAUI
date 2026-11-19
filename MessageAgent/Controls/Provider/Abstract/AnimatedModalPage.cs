namespace MessageAgent.Controls.Provider.Abstract;

/// <summary>
/// Shared close lifecycle for modal pages with page-specific exit animations.
/// </summary>
public abstract class AnimatedModalPage : ContentPage
{
    private bool isClosing;

    protected abstract Task AnimateOutAsync();

    protected async Task CloseAsync()
    {
        if (isClosing) return;

        isClosing = true;

        await AnimateOutAsync();

        if (Navigation.ModalStack.Count > 0) await Navigation.PopModalAsync(animated: false);
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();

        return true;
    }
}
