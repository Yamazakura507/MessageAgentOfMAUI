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
    public static Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions,
        Page hostPage,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
        => ShowLoadingAsyncCore(
            actions, hostPage, animations, executionMode, cancellationToken,
            dialogMode, false, maxVisibleTasks, sortProgress);

    /// <summary>
    /// Displays a modal loading dialog with optional per-action progress.
    /// </summary>
    public static Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions,
        Page hostPage,
        bool isShowProgress,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
        => ShowLoadingAsyncCore(
            actions, hostPage, animations, executionMode, cancellationToken,
            dialogMode, isShowProgress, maxVisibleTasks, sortProgress);

    /// <summary>
    /// Displays a modal loading dialog with one specified animation.
    /// </summary>
    public static Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions,
        Page hostPage,
        LoadingAnimation animation,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
        => actions.ShowLoadingAsync(
            hostPage,
            new[] { animation },
            executionMode,
            cancellationToken,
            dialogMode,
            maxVisibleTasks,
            sortProgress);

    /// <summary>
    /// Displays a modal loading dialog with one animation and optional per-action progress.
    /// </summary>
    public static Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions,
        Page hostPage,
        LoadingAnimation animation,
        bool isShowProgress,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
        => actions.ShowLoadingAsync(
            hostPage,
            isShowProgress,
            new[] { animation },
            executionMode,
            cancellationToken,
            dialogMode,
            maxVisibleTasks,
            sortProgress);

    /// <summary>
    /// Displays a modal loading dialog in the navigation stack owned by a window.
    /// </summary>
    public static Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions,
        Window window,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
    {
        ArgumentNullException.ThrowIfNull(window);

        Page page = window.Page ?? throw new InvalidOperationException("The window does not contain a page.");

        return actions.ShowLoadingAsync(
            page,
            animations,
            executionMode,
            cancellationToken,
            dialogMode,
            maxVisibleTasks,
            sortProgress);
    }

    /// <summary>
    /// Displays a modal loading dialog in a window with optional per-action progress.
    /// </summary>
    public static Task<LoadingResult> ShowLoadingAsync(
        this IEnumerable<LoadingAction> actions,
        Window window,
        bool isShowProgress,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        LoadingDialogMode dialogMode = LoadingDialogMode.Base,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
    {
        ArgumentNullException.ThrowIfNull(window);

        Page page = window.Page ?? throw new InvalidOperationException("The window does not contain a page.");

        return actions.ShowLoadingAsync(
            page,
            isShowProgress,
            animations,
            executionMode,
            cancellationToken,
            dialogMode,
            maxVisibleTasks,
            sortProgress);
    }

    private static async Task<LoadingResult> ShowLoadingAsyncCore(
        IEnumerable<LoadingAction> actions,
        Page hostPage,
        IEnumerable<LoadingAnimation>? animations,
        LoadingExecutionMode executionMode,
        CancellationToken cancellationToken,
        LoadingDialogMode dialogMode,
        bool isShowProgress,
        int maxVisibleTasks,
        LoadingProgressSortMode sortProgress)
    {
        ArgumentNullException.ThrowIfNull(hostPage);
        ArgumentNullException.ThrowIfNull(actions);

        if (maxVisibleTasks < 1) throw new ArgumentOutOfRangeException(nameof(maxVisibleTasks), "At least one task row must remain visible.");

        Page page = hostPage is Shell shell ? shell.CurrentPage ?? shell : hostPage;

        if (page is not ContentPage contentPage) throw new InvalidOperationException("The loading dialog requires a ContentPage host.");

        LoadingDialogPage dialog = new(dialogMode);

        await MainThread.InvokeOnMainThreadAsync(() =>
            contentPage.Navigation.PushModalAsync(dialog, animated: false));

        try
        {
            return await dialog.ExecuteAsync(
                actions,
                animations,
                executionMode,
                isShowProgress,
                cancellationToken,
                maxVisibleTasks,
                sortProgress);
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (dialog.Navigation.ModalStack.Contains(dialog)) await dialog.Navigation.PopModalAsync(animated: false);
            });
        }
    }
}
