using GroupLib.Models;
using MessageAgent.Core.Enums.Message;

namespace MessageAgent.Core.Models.Grouping;

/// <summary>
/// Strongly typed grouping rule for a MessageCollection hierarchy.
/// </summary>
public sealed class MessageGroupRule : GroupRule<GroupMessageParametrs>
{
    /// <summary>
    /// Initializes a rule with its default enum value.
    /// </summary>
    public MessageGroupRule() { }

    /// <summary>
    /// Initializes a rule for the supplied message property and optional priority.
    /// </summary>
    /// <param name="groupParametr">The message property used as a grouping key.</param>
    /// <param name="levelIndex">Optional priority. Lower values are processed as outer levels first.</param>
    public MessageGroupRule(GroupMessageParametrs groupParametr, int? levelIndex = null) : base(groupParametr, levelIndex) { }
}
