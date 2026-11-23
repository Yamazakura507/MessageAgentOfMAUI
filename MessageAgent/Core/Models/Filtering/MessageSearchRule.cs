using MessageAgent.Core.Enums.Message;
using WhereLib.Enums;
using WhereLib.Models;

namespace MessageAgent.Core.Models.Filtering;

/// <summary>
/// Strongly typed text-search condition for a MessageCollection.
/// </summary>
public sealed class MessageSearchRule : SearchRule<MessageSearchParametr>
{
    /// <summary>
    /// Initializes an empty search rule for XAML object construction.
    /// </summary>
    public MessageSearchRule() { }

    /// <summary>
    /// Initializes a search rule for a message text property and query.
    /// </summary>
    public MessageSearchRule(MessageSearchParametr whereParametr, string? searchText = null, LogicalOperator? @operator = null)
        : base(whereParametr, searchText, @operator) { }
}
