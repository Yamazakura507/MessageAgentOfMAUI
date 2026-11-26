namespace MessageAgent.Controls.Lists;

/// <summary>Displays the action button used to select or clear the message-type filter.</summary>
public partial class MessageTypeActionButtonView : ContentView
{
    /// <summary>Occurs when the message-type action button is activated.</summary>
    public event EventHandler? Clicked;

    /// <summary>Initializes the message-type action button.</summary>
    public MessageTypeActionButtonView() => InitializeComponent();

    /// <summary>Sets the button icon, tooltip, and visual selection state.</summary>
    /// <param name="source">The icon displayed by the button.</param>
    /// <param name="tooltip">The tooltip text shown for the button.</param>
    /// <param name="isSelected">Indicates whether the action is currently selected.</param>
    public void Configure(ImageSource source, string tooltip, bool isSelected)
    {
        ActionButton.Source = source;
        ActionButton.Opacity = isSelected ? 1 : 0.68;
        ToolTipProperties.SetText(ActionButton, tooltip);
    }

    private void OnClicked(object? sender, EventArgs e) => Clicked?.Invoke(this, EventArgs.Empty);
}