using MessageAgent.Controls.Provider;
using MessageAgent.Core.Models;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Factory;

public sealed class MessageDetailsPageFactory(IServiceProvider services) : IMessageDetailsPageFactory
{
    public MessageDetailsPage Create(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return ActivatorUtilities.CreateInstance<MessageDetailsPage>(services, message);
    }
}
