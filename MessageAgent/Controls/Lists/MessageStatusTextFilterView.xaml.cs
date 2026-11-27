namespace MessageAgent.Controls.Lists;

/// <summary>
/// XAML-defined status-name filter editor used in the message-list popup.
/// </summary>
public partial class MessageStatusTextFilterView : ContentView
{
    /// <summary>
    /// Identifies the text entered by the user.
    /// </summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(MessageStatusTextFilterView), string.Empty,
        BindingMode.TwoWay,
        propertyChanged: (bindable, oldValue, newValue) =>
            ((MessageStatusTextFilterView)bindable).OnTextChanged(oldValue as string, newValue as string));

    /// <summary>
    /// Identifies the label color supplied by the owning message list.
    /// </summary>
    public static readonly BindableProperty LabelColorProperty = BindableProperty.Create(
        nameof(LabelColor), typeof(Color), typeof(MessageStatusTextFilterView), Colors.Gray);

    /// <summary>
    /// Gets or sets the status name filter text.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the label color from the owning list theme.
    /// </summary>
    public Color LabelColor
    {
        get => (Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    /// <summary>
    /// Raised when the user changes the status filter text.
    /// </summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>
    /// Initializes the status text filter view.
    /// </summary>
    public MessageStatusTextFilterView()
    {
        InitializeComponent();
    }

    private void OnTextChanged(string? oldValue, string? newValue)
        => TextChanged?.Invoke(this, new TextChangedEventArgs(oldValue ?? string.Empty, newValue ?? string.Empty));
}
