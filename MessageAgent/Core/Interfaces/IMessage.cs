using MessageAgent.Core.Enums;
using MessageAgent.Core.Models;

namespace MessageAgent.Core.Interfaces
{
    public interface IMessage
    {
        Guid Id { get; }
        string Title { get; set; }
        MessageType Type { get; set; }
        string? Description { get; set; }
        StatusCollection Status { get; set; }
        ImagesCollection Images { get; set; }
        bool IsRead { get; set; }
    }
}
