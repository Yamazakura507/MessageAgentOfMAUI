using MessageAgent.Controls.Provider;
using MessageAgent.Core.Models;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Factory;

/// <summary>
/// Creates message details pages using the application dependency-injection container.
/// </summary>
/// <param name="services">The application service provider.</param>
public sealed class MessageDetailsPageFactory(IServiceProvider services) : IMessageDetailsPageFactory
{
    /// <inheritdoc />
    public MessageDetailsPage Create(Message message) => Create(message, showReadState: true);

    /// <inheritdoc />
    public MessageDetailsPage Create(Message message, bool showReadState)
    {
        ArgumentNullException.ThrowIfNull(message);

        return ActivatorUtilities.CreateInstance<MessageDetailsPage>(services, message, showReadState);
    }
}
