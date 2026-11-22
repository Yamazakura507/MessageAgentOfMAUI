namespace MessageAgent.Controls.Provider.Abstract;

/// <summary>
/// Shared close lifecycle for modal pages with page-specific exit animations.
/// </summary>
public abstract class AnimatedModalPage : ContentPage
{
    private bool isClosing;

    /// <summary>
    /// Runs the page-specific exit animation before the modal page is closed.
    /// </summary>
    protected abstract Task AnimateOutAsync();

    /// <summary>
    /// Runs the exit animation and removes this page from the modal navigation stack.
    /// </summary>
    protected async Task CloseAsync()
    {
        if (isClosing) return;

        isClosing = true;

        await AnimateOutAsync();

        if (Navigation.ModalStack.Count > 0) await Navigation.PopModalAsync(animated: false);
    }

    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();

        return true;
    }
}
