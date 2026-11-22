using System.Collections.ObjectModel;

namespace MessageAgent.Core.Models.Collections;

/// <summary>
/// Observable collection of image sources attached to a message.
/// </summary>
public sealed class ImagesCollection : ObservableCollection<ImageSource>
{
    /// <summary>
    /// Creates an empty observable image collection.
    /// </summary>
    public ImagesCollection() { }

    /// <summary>
    /// Creates an observable image collection from the supplied sources.
    /// </summary>
    /// <param name="images">Image sources to add in their initial order.</param>
    public ImagesCollection(IEnumerable<ImageSource> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        foreach (ImageSource image in images) Add(image);
    }
}
