namespace MessageAgent.Core.Extensions
{
    public static class PageExtension
    {
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
