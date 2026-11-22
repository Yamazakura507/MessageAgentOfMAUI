namespace MessageAgent.Services.Extensions;

/// <summary>
/// Resolves a MessageAgent Lottie asset from application resources, falling back to
/// the corresponding animation packaged with the library.
/// </summary>
public static class MessageAgentLotties
{
    /// <summary>
    /// Gets an animation source, preferring an application resource override.
    /// Resource values must be strings containing a Lottie asset path or supported source identifier.
    /// </summary>
    /// <param name="resourceKey">The key from <see cref="Resources.ExtensionResources.MessageAgentLottieKeys"/>.</param>
    /// <param name="defaultSource">The packaged fallback animation source.</param>
    /// <returns>The application override or the library's default animation source.</returns>
    public static string Get(string resourceKey, string defaultSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultSource);

        ResourceDictionary? resources = Application.Current?.Resources;

        if (resources is not null && resources.TryFindResource(resourceKey, out object? value)
            && value is string source && !string.IsNullOrWhiteSpace(source))
        {
            return source;
        }

        return defaultSource;
    }
}
