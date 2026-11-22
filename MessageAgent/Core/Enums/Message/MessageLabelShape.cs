namespace MessageAgent.Core.Enums.Message;

/// <summary>
/// Visual geometry used by the compact notification label.
/// </summary>
public enum MessageLabelShape
{
    /// <summary>
    /// The original rounded rectangular label.
    /// </summary>
    Block,

    /// <summary>
    /// A speech bubble with a small tail.
    /// </summary>
    Message,

    /// <summary>
    /// A fully rounded pill.
    /// </summary>
    Pill
}
