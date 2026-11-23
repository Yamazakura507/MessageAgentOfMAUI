namespace MessageAgent.Core.Enums.Message;

/// <summary>
/// Message properties available for hierarchical grouping.
/// </summary>
public enum GroupMessageParametrs
{
    /// <summary>
    /// Groups messages by their message type name.
    /// </summary>
    Type,

    /// <summary>
    /// Groups messages by their read state.
    /// </summary>
    IsRead,

    /// <summary>
    /// Groups messages by their importance state.
    /// </summary>
    IsImportant
}
