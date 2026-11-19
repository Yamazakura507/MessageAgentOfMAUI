using MessageAgent.Core.Models;
using MessageAgent.Core.Views;

namespace MessageAgent.Core.Extensions;

/// <summary>
/// Pure width measurement and row-packing rules for responsive status grids.
/// Kept separate from the visual control so layout decisions can be tested independently.
/// </summary>
internal static class StatusGridLayoutCalculator
{
    public const double StatusSpacing = 8;
    public const double MoreButtonWidth = 50;
    public const double CollapseButtonWidth = 90;
    public const double HorizontalSafetyInset = 24;

    public static List<List<StatusGridItem>> PackRows(
        this IReadOnlyList<Status> statuses, 
        int maximumRows, 
        double availableWidth, 
        double reservedEndWidth, 
        out int nextIndex)
    {
        List<List<StatusGridItem>> rows = new ();
        nextIndex = 0;

        while (nextIndex < statuses.Count && rows.Count < maximumRows)
        {
            bool isLastAllowedRow = rows.Count == maximumRows - 1;
            double rowCapacity = Math.Max(40, availableWidth - (isLastAllowedRow ? reservedEndWidth : 0));
            List<StatusGridItem> row = new ();
            double usedWidth = 0;

            while (nextIndex < statuses.Count)
            {
                StatusGridItem item = statuses[nextIndex].MeasureStatus(rowCapacity);
                double spacing = row.Count > 0 ? StatusSpacing : 0;

                if (row.Count > 0 && usedWidth + spacing + item.Width > rowCapacity) break;

                row.Add(item);
                usedWidth += spacing + item.Width;
                nextIndex++;

                if (usedWidth >= rowCapacity) break;
            }

            if (row.Count == 0) break;

            rows.Add(row);
        }

        return rows;
    }

    private static StatusGridItem MeasureStatus(this Status status, double rowCapacity)
    {
        double fixedWidth = status.Value is null ? 18 : 42;
        double naturalTitleWidth = status.MeasureNaturalTitleWidth();
        double estimatedNaturalWidth = fixedWidth + naturalTitleWidth;
        bool mustTruncateTitle = estimatedNaturalWidth > rowCapacity;
        double titleWidth = double.PositiveInfinity;
        double itemWidth = estimatedNaturalWidth;

        if (mustTruncateTitle)
        {
            titleWidth = Math.Max(12, rowCapacity - fixedWidth);
            itemWidth = rowCapacity;
        }
        

        return new StatusGridItem(status, Math.Ceiling(itemWidth), titleWidth, mustTruncateTitle);
    }

    private static double MeasureNaturalTitleWidth(this Status status)
    {
        double width = 0;

        foreach (char character in status.Name)
        {
            if (char.IsWhiteSpace(character)) width += 4.0;
            else if ("ilI.,:;!'|".Contains(character)) width += 4.0;
            else if ("MWЖШЩЮФЫ".Contains(character)) width += 10.0;
            else if (char.IsUpper(character)) width += 8.2;
            else if (char.IsDigit(character)) width += 7.0;
            else width += 7.1;
        }

        return Math.Ceiling(Math.Max(12, width));
    }
}
