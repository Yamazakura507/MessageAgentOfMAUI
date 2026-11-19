using MessageAgent.Controls.Provider;
using MessageAgent.Core.Models;

namespace MessageAgent.Services.Interfaces;

public interface IMessageDetailsPageFactory
{
    MessageDetailsPage Create(Message message);
}
