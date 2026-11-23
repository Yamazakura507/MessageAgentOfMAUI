using MessageAgent.Core.Models.Collections;
using MessageAgent.Core.Models.ExtentionKinds;

namespace MessageAgent.Core.Interfaces
{
    /// <summary>
    /// Contract for notification content, importance and read state.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Gets the unique identifier of the message.
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// Gets or sets the explicit display order used by a MessageCollection.
        /// </summary>
        int SortOrder { get; set; }
        /// <summary>
        /// Gets or sets the message title.
        /// </summary>
        string Title { get; set; }
        /// <summary>
        /// Gets or sets the visual message type.
        /// </summary>
        MessageType Type { get; set; }
        /// <summary>
        /// Gets or sets the optional description.
        /// </summary>
        string? Description { get; set; }
        /// <summary>
        /// Gets or sets whether the message is marked as important.
        /// </summary>
        bool IsImportant { get; set; }
        /// <summary>
        /// Gets or sets the sortable status collection.
        /// </summary>
        StatusCollection Status { get; set; }
        /// <summary>
        /// Gets or sets the image attachment collection.
        /// </summary>
        ImagesCollection Images { get; set; }
        /// <summary>
        /// Gets or sets the link collection.
        /// </summary>
        MessageLinksCollection Links { get; set; }
        /// <summary>
        /// Gets or sets whether the message has been read.
        /// </summary>
        bool IsRead { get; set; }
    }
}
