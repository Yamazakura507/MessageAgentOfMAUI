using GroupLib.Models;
using MessageAgent.Core.Enums.Message;

namespace MessageAgent.Core.Models.Grouping;

/// <summary>
/// One node in a hierarchical message grouping result.
/// A node contains either child groups or messages belonging to a leaf group.
/// </summary>
public sealed class MessageGroup
{
    /// <summary>
    /// Gets the grouping property represented by this node.
    /// </summary>
    public GroupMessageParametrs Parameter { get; }

    /// <summary>
    /// Gets the key shared by messages in this group. Null keys are supported.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the child groups for the next grouping level.
    /// </summary>
    public IReadOnlyList<MessageGroup> Groups { get; }

    /// <summary>
    /// Gets the messages in this leaf group. Intermediate groups return an empty collection.
    /// </summary>
    public IReadOnlyList<Message> Messages { get; }

    internal MessageGroup(GroupNode<Message, GroupMessageParametrs> node)
    {
        ArgumentNullException.ThrowIfNull(node);

        Parameter = node.Parameter;
        Value = node.Value;
        Groups = node.Children.Select(child => new MessageGroup(child)).ToList();
        Messages = node.Items;
    }
}
