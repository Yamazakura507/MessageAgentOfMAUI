using MessageAgent.Core.Interfaces;
using MessageAgent.Core.Models.Collections;
using MessageAgent.Core.Models.ExtentionKinds;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MessageAgent.Core.Models;

/// <summary>
/// Notification content, associated collections, importance and read state.
/// </summary>
public class Message : IMessage, INotifyPropertyChanged
{
    private string title = string.Empty;
    private MessageType type = MessageTypeConverter.Resolve("Error");
    private string? description;
    private bool isImportant;
    private StatusCollection status = new();
    private ImagesCollection images = new();
    private MessageLinksCollection links = new();
    private INotifyCollectionChanged? statusCollection;
    private INotifyCollectionChanged? imagesCollection;
    private INotifyCollectionChanged? linksCollection;
    private bool isRead;
    private int sortOrder;

    /// <summary>
    /// Initializes a message and observes its default collections.
    /// </summary>
    public Message()
    {
        SubscribeCollection(ref statusCollection, status, StatusCollectionOnCollectionChanged);
        SubscribeCollection(ref imagesCollection, images, ImagesCollectionOnCollectionChanged);
        SubscribeCollection(ref linksCollection, links, LinksCollectionOnCollectionChanged);
    }

    /// <summary>
    /// Gets the immutable unique identifier of this message.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the explicit display order. Lower values appear first when no explicit
    /// sorting rules are configured. Equal values preserve their current order.
    /// </summary>
    public int SortOrder
    {
        get => sortOrder;
        set => SetField(ref sortOrder, value);
    }

    /// <summary>
    /// Gets or sets the message title.
    /// </summary>
    public string Title
    {
        get => title;
        set => SetField(ref title, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the message visual definition. Use a built-in name (Error/Alert/Info)
    /// or reference a MessageType object from the application resources.
    /// </summary>
    [AllowNull]
    public MessageType Type
    {
        get => type;
        set => SetField(ref type, value ?? MessageTypeConverter.Resolve("Info"));
    }

    /// <summary>
    /// Gets or sets the optional message description.
    /// </summary>
    public string? Description
    {
        get => description;
        set => SetField(ref description, value);
    }

    /// <summary>
    /// Gets or sets whether this message is marked as important.
    /// </summary>
    public bool IsImportant
    {
        get => isImportant;
        set => SetField(ref isImportant, value);
    }

    /// <summary>
    /// Gets or sets the observable status collection. Its sorting rules are configured
    /// through <c>StatusCollection.SortRules</c>.
    /// </summary>
    public StatusCollection Status
    {
        get => status;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(status, value)) return;

            UnsubscribeCollection(ref statusCollection, StatusCollectionOnCollectionChanged);

            status = value;

            SubscribeCollection(ref statusCollection, status, StatusCollectionOnCollectionChanged);

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the observable image attachment collection.
    /// </summary>
    public ImagesCollection Images
    {
        get => images;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(images, value)) return;

            UnsubscribeCollection(ref imagesCollection, ImagesCollectionOnCollectionChanged);

            images = value;

            SubscribeCollection(ref imagesCollection, images, ImagesCollectionOnCollectionChanged);

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the observable link collection. Its sorting rules are configured
    /// through <c>MessageLinksCollection.SortRules</c>.
    /// </summary>
    public MessageLinksCollection Links
    {
        get => links;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(links, value)) return;
            UnsubscribeCollection(ref linksCollection, LinksCollectionOnCollectionChanged);
            links = value;
            SubscribeCollection(ref linksCollection, links, LinksCollectionOnCollectionChanged);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets whether the notification has been read. MessageProvider sets this
    /// after its notification animation completes.
    /// </summary>
    public bool IsRead
    {
        get => isRead;
        set => SetField(ref isRead, value);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private static void SubscribeCollection(
        ref INotifyCollectionChanged? observedCollection,
        INotifyCollectionChanged collection,
        NotifyCollectionChangedEventHandler handler)
    {
        observedCollection = collection;
        observedCollection.CollectionChanged += handler;
    }

    private static void UnsubscribeCollection(ref INotifyCollectionChanged? observedCollection, NotifyCollectionChangedEventHandler handler)
    {
        if (observedCollection is not null) observedCollection.CollectionChanged -= handler;

        observedCollection = null;
    }

    private void StatusCollectionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Status));

    private void ImagesCollectionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Images));

    private void LinksCollectionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Links));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
