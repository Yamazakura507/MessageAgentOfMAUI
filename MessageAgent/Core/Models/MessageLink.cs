using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MessageAgent.Core.Enums.Link;
using MessageAgent.Core.Models.VisualValues.Abstract;

namespace MessageAgent.Core.Models;

/// <summary>
/// A navigable resource attached to a message. It can provide one polymorphic visual value.
/// Clicked and Command are raised before the built-in destination action.
/// </summary>
[ContentProperty(nameof(Path))]
public sealed class MessageLink : INotifyPropertyChanged
{
    private MessageLinkType type = MessageLinkType.Web;
    private string? path;
    private Page? page;
    private Window? window;
    private string? title;
    private string? description;
    private VisualValue? value;
    private ICommand? command;
    private object? commandParameter;
    private int sortOrder;

    /// <summary>
    /// Gets or sets how <see cref="Path"/> is interpreted.
    /// </summary>
    public MessageLinkType Type
    {
        get => type;
        set
        {
            if (SetField(ref type, value)) OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// Gets or sets the destination interpreted according to <see cref="Type"/>:
    /// HTTP/HTTPS URL, a path under AppDataDirectory, or a Shell route.
    /// </summary>
    public string? Path
    {
        get => path;
        set
        {
            if (SetField(ref path, value)) OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// Gets or sets an optional page to push when <see cref="Type"/> is Navigation.
    /// </summary>
    public Page? Page
    {
        get => page;
        set
        {
            if (SetField(ref page, value)) OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// Gets or sets an optional window to open for a navigation link.
    /// </summary>
    public Window? Window
    {
        get => window;
        set
        {
            if (SetField(ref window, value)) OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// Gets or sets the optional text shown in the dialog instead of the destination.
    /// </summary>
    public string? Title
    {
        get => title;
        set
        {
            if (SetField(ref title, value)) OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// Gets or sets the optional tooltip description.
    /// </summary>
    public string? Description
    {
        get => description;
        set => SetField(ref description, value);
    }

    /// <summary>
    /// Gets or sets the image, Lottie animation or custom MAUI view for this link.
    /// </summary>
    public VisualValue? Value
    {
        get => value;
        set => SetField(ref this.value, value);
    }

    /// <summary>
    /// Gets or sets the optional command executed when the link is activated.
    /// </summary>
    public ICommand? Command
    {
        get => command;
        set => SetField(ref command, value);
    }

    /// <summary>
    /// Gets or sets the parameter passed to <see cref="Command"/>.
    /// </summary>
    public object? CommandParameter
    {
        get => commandParameter;
        set => SetField(ref commandParameter, value);
    }

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
    /// Raised when this link is activated, before its built-in destination action.
    /// </summary>
    public event EventHandler? Clicked;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the explicit title or a shortened destination label.
    /// </summary>
    public string DisplayText => !string.IsNullOrWhiteSpace(Title) ? Title : GetShortDestination();

    /// <summary>
    /// Returns a shortened readable label for the current destination.
    /// </summary>
    public string GetShortDestination()
    {
        string destination = Type == MessageLinkType.Navigation && string.IsNullOrWhiteSpace(Path)
            ? Page?.Title ?? Window?.Page?.Title ?? string.Empty
            : Path ?? string.Empty;

        if (string.IsNullOrWhiteSpace(destination)) return "Open";
        if (Type == MessageLinkType.Web && Uri.TryCreate(destination, UriKind.Absolute, out Uri? uri)) destination = uri.Host + uri.PathAndQuery;

        const int maxLength = 52;

        return destination.Length <= maxLength ? destination : destination[..(maxLength - 1)] + "…";
    }

    /// <summary>
    /// Raises the link event and command, then opens or navigates to the configured destination.
    /// </summary>
    public async ValueTask ActivateAsync()
    {
        Clicked?.Invoke(this, EventArgs.Empty);

        if (Command?.CanExecute(CommandParameter) == true) Command.Execute(CommandParameter);

        switch (Type)
        {
            case MessageLinkType.Web:
                if (!string.IsNullOrWhiteSpace(Path)
                    && Uri.TryCreate(Path, UriKind.Absolute, out Uri? webUri)
                    && (webUri.Scheme == Uri.UriSchemeHttp || webUri.Scheme == Uri.UriSchemeHttps)) await Launcher.OpenAsync(webUri);
                break;
            case MessageLinkType.LocalStorage:
                await OpenLocalFileAsync();
                break;
            case MessageLinkType.Navigation:
                await NavigateAsync();
                break;
        }
    }

    private async Task OpenLocalFileAsync()
    {
        if (string.IsNullOrWhiteSpace(Path)) return;

        string storageRoot = System.IO.Path.GetFullPath(FileSystem.AppDataDirectory);
        string relativePath = System.IO.Path.IsPathRooted(Path) ? System.IO.Path.GetRelativePath(storageRoot, Path) : Path;
        string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(storageRoot, relativePath));
        string rootPrefix = storageRoot.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;

        if (fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            await Launcher.OpenAsync(new OpenFileRequest(Title ?? System.IO.Path.GetFileName(fullPath), new ReadOnlyFile(fullPath)));
    }

    private async Task NavigateAsync()
    {
        if (Window is not null) Application.Current?.OpenWindow(Window);
        else if (Page is not null)
        {
            INavigation? navigation = Shell.Current?.Navigation ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;

            if (navigation is not null) await navigation.PushAsync(Page);
        }
        else if (!string.IsNullOrWhiteSpace(Path) && Shell.Current is not null) await Shell.Current.GoToAsync(Path);
    }

    private bool SetField<TValue>(ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TValue>.Default.Equals(field, value)) return false;

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
