using System.Collections.ObjectModel;

namespace MessageAgent.Core.Models;

/// <summary>
/// Observable collection of image sources attached to a message.
/// </summary>
public sealed class ImagesCollection : ObservableCollection<ImageSource>
{
    public ImagesCollection() { }

    public ImagesCollection(IEnumerable<ImageSource> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        foreach (ImageSource image in images) Add(image);
    }
}
