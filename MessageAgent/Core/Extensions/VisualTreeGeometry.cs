namespace MessageAgent.Core.Extensions;

/// <summary>
/// Converts visual-element coordinates into the coordinate space of an ancestor page.
/// Scroll offsets and visual translations are included.
/// </summary>
internal static class VisualTreeGeometry
{
    public static Page? FindHostPage(VisualElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element.FindParentPage() ?? element.Window?.Page ?? Application.Current?.Windows.FirstOrDefault()?.Page;
    }

    public static Rect GetBoundsRelativeToPage(VisualElement element, Page page)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(page);

        double x = element.X + element.TranslationX;
        double y = element.Y + element.TranslationY;
        Element? current = element.Parent;

        while (current is not null && !ReferenceEquals(current, page))
        {
            if (current is ScrollView scrollView)
            {
                x -= scrollView.ScrollX;
                y -= scrollView.ScrollY;
            }

            if (current is VisualElement visual)
            {
                x += visual.X + visual.TranslationX;
                y += visual.Y + visual.TranslationY;
            }

            current = current.Parent;
        }

        return new Rect(x, y, element.Width, element.Height);
    }
}
