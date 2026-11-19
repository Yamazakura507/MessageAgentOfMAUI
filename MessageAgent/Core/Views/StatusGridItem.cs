using MessageAgent.Core.Models;

namespace MessageAgent.Core.Views
{
    /// <summary>
    /// Result of measuring one status badge for a constrained grid row.
    /// </summary>
    internal sealed record StatusGridItem(Status Status, double Width, double TitleWidth, bool MustTruncateTitle);
}
