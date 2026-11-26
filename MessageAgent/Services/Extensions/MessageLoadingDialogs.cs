using MessageAgent.Controls.Loading;
using MessageAgent.Core.Enums.Loading;
using MessageAgent.Core.Models.Loading;

namespace MessageAgent.Services.Extensions;

/// <summary>
/// Provides modal loading-dialog entry points for MAUI pages and windows.
/// </summary>
public static class MessageLoadingDialogs
{
    /// <summary>
    /// Displays a modal loading dialog and awaits the final result.
    /// </summary>
    public static async Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions,
        Page hostPage,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base)
    {
        ArgumentNullException.ThrowIfNull(hostPage);
        ArgumentNullException.ThrowIfNull(actions);

        Page page = hostPage is Shell shell ? shell.CurrentPage ?? shell : hostPage;

        if (page is not ContentPage contentPage) throw new InvalidOperationException("The loading dialog requires a ContentPage host.");

        LoadingDialogPage dialog = new(dialogMode);

        await MainThread.InvokeOnMainThreadAsync(() => contentPage.Navigation.PushModalAsync(dialog, animated: false));

        try
        {
            return await dialog.ExecuteAsync(actions, animations, executionMode, cancellationToken);
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (dialog.Navigation.ModalStack.Contains(dialog)) await dialog.Navigation.PopModalAsync(animated: false);
            });
        }
    }

    /// <summary>
    /// Displays a modal loading dialog with one specified animation.
    /// </summary>
    public static Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions, 
        Page hostPage,
        LoadingAnimation animation,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base)
            => actions.ShowLoadingAsync(hostPage, new[] { animation }, executionMode, cancellationToken, dialogMode);

    /// <summary>
    /// Displays a modal loading dialog in the navigation stack owned by a window.
    /// </summary>
    public static Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions,
        Window window,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base)
    {
        ArgumentNullException.ThrowIfNull(window);

        Page page = window.Page ?? throw new InvalidOperationException("The window does not contain a page.");

        return actions.ShowLoadingAsync(page, animations, executionMode, cancellationToken, dialogMode);
    }
}