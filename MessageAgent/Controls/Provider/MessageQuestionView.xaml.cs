using MessageAgent.Core.Models.Dialogs;
using MessageAgent.Core.Models.ExtentionKinds;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Renders the configurable set of question buttons in XAML.
/// </summary>
public partial class MessageQuestionView : ContentView
{
    /// <summary>
    /// Gets the buttons displayed by the question view.
    /// </summary>
    public IReadOnlyList<MessageQuestionButton> Buttons { get; }
    /// <summary>
    /// Gets the message type that determines the question button palette.
    /// </summary>
    public MessageType Type { get; }
    /// <summary>
    /// Occurs when a question button is selected.
    /// </summary>
    public event EventHandler<MessageQuestionButton>? ChoiceSelected;

    /// <summary>
    /// Initializes the question view with its buttons and message type.
    /// </summary>
    /// <param name="buttons">The buttons to render in the question dialog.</param>
    /// <param name="type">The message type used to resolve the button colors.</param>
    /// <exception cref="ArgumentNullException">The buttons collection is <see langword="null"/>.</exception>
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
