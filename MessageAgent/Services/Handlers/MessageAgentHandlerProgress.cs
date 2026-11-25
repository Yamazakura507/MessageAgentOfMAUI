using CollectionLib.Models.Handlers;
using MessageAgent.Services.Extensions;
using System.Diagnostics;

namespace MessageAgent.Services.Handlers
{
    /// <summary>
    /// Routes CollectionLib notification reports through the MessageAgent Error dialog.
    /// </summary>
    internal sealed class MessageAgentHandlerProgress : IProgress<Message>
    {
        public void Report(Message value)
        {
            ArgumentNullException.ThrowIfNull(value);
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await MessageDialogs.ErrorAsync(value.Title, value.Text, value.Page);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"Unable to display CollectionLib Error message: {exception}");
                }
            });
        }
    }
}
