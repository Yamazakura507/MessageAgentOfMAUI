namespace MessageAgent.Resources;

/// <summary>
/// Centralized built-in resources and default settings for MessageAgent.
/// Application resources override entries in this dictionary without requiring
/// the application to merge this dictionary manually.
/// </summary>
public partial class DefaultResourceDictionary : ResourceDictionary
{
    private static readonly Lazy<DefaultResourceDictionary> instance = new(() => new DefaultResourceDictionary());

    /// <summary>
    /// Gets the shared library defaults dictionary.
    /// </summary>
    internal static DefaultResourceDictionary Instance => instance.Value;

    /// <summary>
    /// Loads the built-in resource definitions.
    /// </summary>
    public DefaultResourceDictionary()
    {
        InitializeComponent();
    }
}
