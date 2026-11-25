using MessageAgent.Core.Models.Dialogs;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Renders the configurable set of question buttons in XAML.
/// </summary>
public partial class MessageQuestionView : ContentView
{
    public IReadOnlyList<MessageQuestionButton> Buttons { get; }
    public Color AccentColor { get; }
    public Color AccentContentColor { get; }
    public event EventHandler<MessageQuestionButton>? ChoiceSelected;

    public MessageQuestionView(
        IReadOnlyList<MessageQuestionButton> buttons,
        Color accentColor,
        Color accentContentColor)
    {
        Buttons = buttons ?? throw new ArgumentNullException(nameof(buttons));
        AccentColor = accentColor;
        AccentContentColor = accentContentColor;

        InitializeComponent();
    }

    private void ChoiceButtonOnClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is MessageQuestionButton button) ChoiceSelected?.Invoke(this, button);
    }
}