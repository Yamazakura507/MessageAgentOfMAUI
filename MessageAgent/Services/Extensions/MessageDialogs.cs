using MessageAgent.Controls.Provider;
using MessageAgent.Core.Enums.Link;
using MessageAgent.Core.Models;
using MessageAgent.Core.Models.Dialogs.Options;
using MessageAgent.Core.Models.ExtentionKinds;
using System.Text.RegularExpressions;

namespace MessageAgent.Services.Extensions
{
    /// <summary>
    /// Convenience entry points for built-in message categories and CAPTCHA dialogs.
    /// </summary>
    public static class MessageDialogs
    {
        /// <summary>
        /// Shows a red Error message-details dialog.
        /// </summary>
        public static ValueTask<MessageDetailsPage> ErrorAsync(string title, string? description = null, Page? hostPage = null)
            => MessageType.Error.CreateMessage(title, description).ShowAsync(hostPage);

        /// <summary>
        /// Shows an Alert message-details dialog.
        /// </summary>
        public static ValueTask<MessageDetailsPage> AlertAsync(string title, string? description = null, Page? hostPage = null)
            => MessageType.Alert.CreateMessage(title, description).ShowAsync(hostPage);

        /// <summary>
        /// Shows an Info message-details dialog.
        /// </summary>
        public static ValueTask<MessageDetailsPage> InfoAsync(string title, string? description = null, Page? hostPage = null)
            => MessageType.Info.CreateMessage(title, description).ShowAsync(hostPage);

        /// <summary>
        /// Shows the CAPTCHA modal. Returns false on cancellation.
        /// </summary>
        public static async ValueTask<bool> ShowCaptchaAsync(MessageCaptchaOptions? options = null, Page? hostPage = null)
        {
            options ??= new MessageCaptchaOptions();
            MessageCaptchaPage? dialog = null;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                dialog = await MessageCaptchaPage.CreateAsync(options);
                Page page = hostPage
                    ?? Application.Current?.Windows.FirstOrDefault(window => window.Page is not null)?.Page
                    ?? throw new InvalidOperationException("Не удалось определить страницу для CAPTCHA. Передайте hostPage явно.");

                if (page is Shell shell) page = shell.CurrentPage ?? shell;

                await page.Navigation.PushModalAsync(dialog, animated: false);
            });

            return await dialog!.Result;
        }

        private static Message CreateMessage(this MessageType type, string title, string? description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            Message message = new()
            {
                Type = type,
                Title = title,
                Description = description
            };

            message.ExtractLinksFromDescription();

            return message;
        }

        /// <summary>
        /// Finds HTTP(S) links in a message description and moves them to Message.Links.
        /// </summary>
        private static void ExtractLinksFromDescription(this Message message)
        {
            if (string.IsNullOrWhiteSpace(message.Description)) return;

            const string pattern = @"(?i)\b(?:https?://|www\.)[^\s<>""']+";

            MatchCollection matches = Regex.Matches(message.Description, pattern);

            if (matches.Count == 0) return;

            string description = message.Description;

            List<(Match Match, MessageLink Link)> extractedLinks = new();

            foreach (Match match in matches.Cast<Match>())
            {
                string candidate = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}');

                if (candidate.Length == 0) continue;

                string url = candidate.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? $"https://{candidate}" : candidate;

                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) continue;

                extractedLinks.Add((match, new MessageLink
                {
                    Type = MessageLinkType.Web,
                    Path = uri.AbsoluteUri,
                    Title = uri.Host
                }));
            }

            foreach ((Match match, _) in extractedLinks.AsEnumerable().Reverse())
            {
                string candidate = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}');

                description = description.Remove(match.Index, candidate.Length);
            }

            foreach ((_, MessageLink link) in extractedLinks) message.Links.Add(link);

            message.Description = Regex.Replace(description, @"[ \t]{2,}", " ").Trim();
        }
    }
}
