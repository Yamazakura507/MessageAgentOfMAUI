namespace MessageAgent.Controls.Lists;

public partial class MessageTypeActionButtonView : ContentView
{
    public event EventHandler? Clicked;

    public MessageTypeActionButtonView() => InitializeComponent();

    public void Configure(ImageSource source, string tooltip, bool isSelected)
    {
        ActionButton.Source = source;
        ActionButton.Opacity = isSelected ? 1 : 0.68;
        ToolTipProperties.SetText(ActionButton, tooltip);
    }

    private void OnClicked(object? sender, EventArgs e) => Clicked?.Invoke(this, EventArgs.Empty);
}