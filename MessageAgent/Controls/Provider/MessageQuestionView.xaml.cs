using MessageAgent.Core.Models.Dialogs;
using MessageAgent.Core.Models.ExtentionKinds;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Renders the configurable set of question buttons in XAML.
/// </summary>
public partial class MessageQuestionView : ContentView
{
    public IReadOnlyList<MessageQuestionButton> Buttons { get; }
    public MessageType Type { get; }
    public event EventHandler<MessageQuestionButton>? ChoiceSelected;

    public MessageQuestionView(IReadOnlyList<MessageQuestionButton> buttons, MessageType type)
    {
        Buttons = buttons ?? throw new ArgumentNullException(nameof(buttons));
        Type = type;

        InitializeComponent();
    }

    private void ChoiceButtonOnClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is MessageQuestionButton button) ChoiceSelected?.Invoke(this, button);
    }
}
