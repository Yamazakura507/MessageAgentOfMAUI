using System.Diagnostics;
using System.Text.RegularExpressions;
using MessageAgent.Controls.Provider;
using MessageAgent.Core.Enums.Link;
using MessageAgent.Core.Models;
using MessageAgent.Core.Models.Dialogs;
using MessageAgent.Core.Models.Dialogs.Options;
using MessageAgent.Core.Models.ExtentionKinds;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Extensions;

/// <summary>
/// Opens MessageAgent modal dialogs directly from a Message model.
/// </summary>
public static class MessageHandlerExtensions
{
    /// <summary>
    /// Opens the existing full message-details dialog without requiring MessageProvider.
    /// </summary>
    public static ValueTask<MessageDetailsPage> ShowAsync(this Message message, Page? hostPage = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        IMessageDetailsPageFactory factory = MauiServiceProvider.GetRequiredService<IMessageDetailsPageFactory>();

        return PushModalAsync(() => factory.Create(message), hostPage);
    }

    /// <summary>
    /// Alias for <see cref="ShowAsync"/>.
    /// </summary>
    public static ValueTask<MessageDetailsPage> ShowDialogAsync(this Message message, Page? hostPage = null) => message.ShowAsync(hostPage);

    /// <summary>
    /// Opens an input dialog and returns the validated value, or null on cancellation.
    /// </summary>
    public static async ValueTask<string?> ShowInputAsync(
        this Message message,
        MessageInputOptions? options = null,
        Page? hostPage = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        options ??= new MessageInputOptions();
        MessageInputView? input = null;
        MessagePromptPage dialog = await PushModalAsync(() =>
        {
            input = new MessageInputView(options);
            return new MessagePromptPage(
                message,
                ImageSource.FromFile("message_input.svg"),
                input,
                acceptText: options.AcceptText ?? "Подтвердить",
                cancelText: options.CancelText ?? "Отмена");
        }, hostPage, page =>
                        {
                            page.AcceptButton.Clicked += async (_, _) =>
                            {
                                if (input!.TryGetValue(out string value)) await page.CompleteAsync(value);
                            };
                        });

        return await dialog.Result as string;
    }

    /// <summary>
    /// Opens a question dialog and returns the selected button value, or null on cancellation.
    /// </summary>
    public static async ValueTask<string?> AskAsync(this Message message, MessageQuestionOptions? options = null, Page? hostPage = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        options ??= new MessageQuestionOptions();
        IReadOnlyList<MessageQuestionButton> buttons = options.ResolveButtons();
        MessageQuestionView? choices = null;
        MessagePromptPage dialog = await PushModalAsync(() =>
        {
            choices = new MessageQuestionView(buttons, message.Type.GetDialogAccent(), message.Type.GetAccentContent());

            return new MessagePromptPage(message, ImageSource.FromFile("message_question.svg"), choices, showFooter: false);
        }, hostPage, page =>
        {
            choices!.ChoiceSelected += async (_, selected) => await page.CompleteAsync(selected.Value);
        });

        return await dialog.Result as string;
    }

    /// <summary>
    /// Alias for <see cref="AskAsync"/>.
    /// </summary>
    public static ValueTask<string?> ShowQuestionAsync(
        this Message message,
        MessageQuestionOptions? options = null,
        Page? hostPage = null) => message.AskAsync(options, hostPage);

    private static async ValueTask<TPage> PushModalAsync<TPage>(
        Func<TPage> createDialog, Page? hostPage, Action<TPage>? configure = null) where TPage : Page
    {
        TPage? dialog = null;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            dialog = createDialog();
            configure?.Invoke(dialog);
            Page page = hostPage ?? ResolveHostPage()
                ?? throw new InvalidOperationException("Не удалось определить страницу для MessageAgent-диалога. Передайте hostPage явно.");
            
            await page.Navigation.PushModalAsync(dialog, animated: false);
        });

        return dialog!;
    }

    private static Page? ResolveHostPage()
    {
        Page? page = Application.Current?.Windows.FirstOrDefault(window => window.Page is not null)?.Page;

        return page is Shell shell ? shell.CurrentPage ?? shell : page;
    }
}
