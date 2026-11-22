namespace MessageAgent.Services.Extensions;

/// <summary>
/// Resolves a MessageAgent icon from application resources, falling back to the
/// corresponding image packaged with the library.
/// </summary>
public static class MessageAgentImages
{
    /// <summary>
    /// Gets an icon source, preferring an application resource override.
    /// Resource values may be an <see cref="ImageSource"/> or a file-name string.
    /// </summary>
    /// <param name="resourceKey">The key from <see cref="Resources.ExtensionResources.MessageAgentImageKeys"/>.</param>
    /// <param name="defaultFileName">The packaged fallback image file name.</param>
    /// <returns>The application override or the library's default image source.</returns>
    public static ImageSource Get(string resourceKey, string defaultFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultFileName);

        ResourceDictionary? resources = Application.Current?.Resources;

        if (resources is not null && resources.TryFindResource(resourceKey, out object? value))
        {
            ImageSource? applicationSource = ToImageSource(value);

            if (applicationSource is not null) return applicationSource;
        }

        if (MessageAgent.Resources.DefaultResourceDictionary.Instance.TryFindResource(resourceKey, out object? defaultValue))
        {
            ImageSource? defaultSource = ToImageSource(defaultValue);

            if (defaultSource is not null) return defaultSource;
        }

        return ImageSource.FromFile(NormalizeImageFileName(defaultFileName));
    }

    private static ImageSource? ToImageSource(object? value)
    {
        if (value is FileImageSource fileImageSource && !string.IsNullOrWhiteSpace(fileImageSource.File))
            return ImageSource.FromFile(NormalizeImageFileName(fileImageSource.File));
        if (value is ImageSource imageSource) return imageSource;
        if (value is string fileName && !string.IsNullOrWhiteSpace(fileName))
            return ImageSource.FromFile(NormalizeImageFileName(fileName));

        return null;
    }

    private static string NormalizeImageFileName(string fileName)
        => Path.GetExtension(fileName).Equals(".svg", StringComparison.OrdinalIgnoreCase) ? Path.ChangeExtension(fileName, ".png") : fileName;

    internal static bool TryFindResource(this ResourceDictionary dictionary, string key, out object? value)
    {
        if (dictionary.TryGetValue(key, out value)) return true;

        foreach (ResourceDictionary mergedDictionary in dictionary.MergedDictionaries.Reverse())
        {
            if (TryFindResource(mergedDictionary, key, out value)) return true;
        }

        value = null;

        return false;
    }
}
