using MessageAgent.Controls.Provider;
using MessageAgent.Core.Models;

namespace MessageAgent.Services.Interfaces;

/// <summary>
/// Creates the modal page used to present full message details.
/// </summary>
public interface IMessageDetailsPageFactory
{
    /// <summary>
    /// Creates a details page for the supplied message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <returns>A configured details page.</returns>
    MessageDetailsPage Create(Message message);

    /// <summary>
    /// Creates a details page and controls whether its read-state badge is visible.
    /// The default implementation preserves compatibility with existing custom factories.
    /// </summary>
    MessageDetailsPage Create(Message message, bool showReadState) => Create(message);
}
