namespace MessageAgent.Core.Extensions
{
    /// <summary>
    /// Helpers for traversing MAUI visual-parent relationships.
    /// </summary>
    public static class PageExtension
    {
        /// <summary>
        /// Finds the nearest parent page of an element.
        /// </summary>
        /// <param name="parent">The element from which traversal starts.</param>
        /// <returns>The first parent page, or null when no page is found.</returns>
        public static Page? FindParentPage(this Element? parent)
        {
            Element? current = parent;

            while (current is not null)
            {
                if (current is Page page) return page;

                current = current.Parent;
            }

            return null;
        }
    }
}
