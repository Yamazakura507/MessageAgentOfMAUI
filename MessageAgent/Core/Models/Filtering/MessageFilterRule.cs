using MessageAgent.Core.Enums.Message;
using WhereLib.Enums;
using WhereLib.Models;

namespace MessageAgent.Core.Models.Filtering;

/// <summary>
/// Strongly typed filter condition for a MessageCollection.
/// </summary>
public sealed class MessageFilterRule : FilterRule<MessageFilterParametr>
{
    /// <summary>
    /// Initializes an empty filter rule for XAML object construction.
    /// </summary>
    public MessageFilterRule() { }

    /// <summary>
    /// Initializes a filter rule for a message property and expected value.
    /// </summary>
    public MessageFilterRule(MessageFilterParametr whereParametr, object? value = null, LogicalOperator? @operator = null)
        : base(whereParametr, value, @operator) { }
}
