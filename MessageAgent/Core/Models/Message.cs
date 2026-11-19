using MessageAgent.Core.Enums;
using MessageAgent.Core.Interfaces;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MessageAgent.Core.Models
{
    /// <summary>
    /// Notification content and its read state.
    /// </summary>
    public class Message : IMessage, INotifyPropertyChanged
    {
        private string title = string.Empty;
        private MessageType type = MessageType.Error;
        private string? description;
        private StatusCollection status = new();
        private ImagesCollection images = new();
        private bool isRead;
        private INotifyCollectionChanged? statusCollection;
        private INotifyCollectionChanged? imagesCollection;
        private readonly HashSet<Status> observedStatuses = new(ReferenceEqualityComparer.Instance);

        public Guid Id { get; } = Guid.NewGuid();

        public Message()
        {
            SubscribeStatusCollection(status);
            SubscribeImagesCollection(images);
        }

        public string Title
        {
            get => title;
            set => SetField(ref title, value ?? string.Empty);
        }

        public MessageType Type
        {
            get => type;
            set => SetField(ref type, value);
        }

        public string? Description
        {
            get => description;
            set => SetField(ref description, value);
        }

        /// <summary>
        /// Observable collection of statuses. It can be populated directly in XAML
        /// using the Message.Status property element without a List wrapper.
        /// </summary>
        public StatusCollection Status
        {
            get => status;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                if (ReferenceEquals(status, value)) return;

                UnsubscribeStatusCollection();

                status = value;

                SubscribeStatusCollection(status);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Observable collection of image sources attached to the message.
        /// </summary>
        public ImagesCollection Images
        {
            get => images;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                if (ReferenceEquals(images, value)) return;

                UnsubscribeImagesCollection();

                images = value;

                SubscribeImagesCollection(images);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the notification has been read.
        /// MessageProvider sets this to true after its notification animation completes.
        /// </summary>
        public bool IsRead
        {
            get => isRead;
            set => SetField(ref isRead, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SubscribeStatusCollection(StatusCollection collection)
        {
            statusCollection = collection;
            statusCollection.CollectionChanged += StatusCollectionOnCollectionChanged;

            SynchronizeStatusSubscriptions(collection);
        }

        private void SynchronizeStatusSubscriptions(IEnumerable<Status> statuses)
        {
            HashSet<Status> current = new (statuses, ReferenceEqualityComparer.Instance);

            foreach (Status status in observedStatuses)
            {
                if (!current.Contains(status)) status.PropertyChanged -= StatusOnPropertyChanged;
            }

            foreach (Status status in current)
            {
                if (!observedStatuses.Contains(status)) status.PropertyChanged += StatusOnPropertyChanged;
            }

            observedStatuses.Clear();
            observedStatuses.UnionWith(current);
        }

        private void StatusOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(global::MessageAgent.Core.Models.Status.SortOrder)) OnPropertyChanged(nameof(Status));
        }

        private void SubscribeImagesCollection(ImagesCollection collection)
        {
            imagesCollection = collection;
            imagesCollection.CollectionChanged += ImagesCollectionOnCollectionChanged;
        }

        private void UnsubscribeStatusCollection()
        {
            if (statusCollection is not null) statusCollection.CollectionChanged -= StatusCollectionOnCollectionChanged;

            foreach (Status status in observedStatuses) status.PropertyChanged -= StatusOnPropertyChanged;

            observedStatuses.Clear();
            statusCollection = null;
        }

        private void UnsubscribeImagesCollection()
        {
            if (imagesCollection is not null) imagesCollection.CollectionChanged -= ImagesCollectionOnCollectionChanged;

            imagesCollection = null;
        }

        private void StatusCollectionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (Status is not null) SynchronizeStatusSubscriptions(Status);

            OnPropertyChanged(nameof(Status));
        }

        private void ImagesCollectionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Images));

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            field = value;

            OnPropertyChanged(propertyName);

            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
