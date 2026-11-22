namespace MessageAgent.Core.Extensions;

/// <summary>
/// Converts visual-element coordinates into the coordinate space of an ancestor page.
/// Scroll offsets and visual translations are included.
/// </summary>
internal static class VisualTreeGeometry
{
    /// <summary>
    /// Finds the page that hosts the specified visual element.
    /// The search checks the visual parent hierarchy first, then the element's window,
    /// and finally the first application window.
    /// </summary>
    /// <param name="element">The visual element whose host page is being resolved.</param>
    /// <returns>The hosting page, or <see langword="null"/> when no page can be resolved.</returns>
    public static Page? FindHostPage(this VisualElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element.FindParentPage() ?? element.Window?.Page ?? Application.Current?.Windows.FirstOrDefault()?.Page;
    }

    /// <summary>
    /// Calculates the element's bounds in the coordinate space of the specified page.
    /// Parent translations are included and scroll offsets are subtracted while walking
    /// through enclosing <see cref="ScrollView"/> instances.
    /// </summary>
    /// <param name="element">The visual element whose bounds are being calculated.</param>
    /// <param name="page">The page that defines the destination coordinate space.</param>
    /// <returns>A rectangle containing the element's page-relative position and its current width and height.</returns>
    public static Rect GetBoundsRelativeToPage(this VisualElement element, Page page)
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
