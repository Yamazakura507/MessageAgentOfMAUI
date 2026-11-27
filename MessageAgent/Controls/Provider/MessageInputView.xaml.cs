using MessageAgent.Core.Models.Dialogs.Options;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// XAML input control with centralized validation from MessageInputOptions.
/// </summary>
public partial class MessageInputView : ContentView
{
    private readonly MessageInputOptions options;

    /// <summary>
    /// Gets the accessible name of the active input control.
    /// </summary>
    public string AccessibilityName => options.Placeholder ?? "Введите значение";

    /// <summary>
    /// Gets the keyboard configuration requested by the input options.
    /// </summary>
    public Keyboard RequestedKeyboard => options.Keyboard;

    /// <summary>
    /// Initializes the input view and applies the supplied input and validation settings.
    /// </summary>
    /// <param name="options">The placeholder, keyboard, length, and validation configuration.</param>
    /// <exception cref="ArgumentNullException">The options argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The configured minimum or maximum length is invalid.</exception>
    /// <exception cref="ArgumentException">Password and multiline input are enabled together.</exception>
    public MessageInputView(MessageInputOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        if (options.MaximumLength < 1 || options.MinimumLength < 0 || options.MinimumLength > options.MaximumLength)
            throw new ArgumentOutOfRangeException(nameof(options), "Проверьте ограничения длины ввода.");
        if (options.IsPassword && options.IsMultiline)
            throw new ArgumentException("Режим пароля доступен только для однострочного поля.", nameof(options));

        InitializeComponent();

        SingleLineInput.IsVisible = !options.IsMultiline;
        MultiLineInput.IsVisible = options.IsMultiline;
        SingleLineInput.Placeholder = MultiLineInput.Placeholder = options.Placeholder;
        SingleLineInput.Keyboard = MultiLineInput.Keyboard = options.Keyboard;
        SingleLineInput.MaxLength = MultiLineInput.MaxLength = options.MaximumLength;
        SingleLineInput.IsPassword = options.IsPassword;
        SingleLineInput.Text = MultiLineInput.Text = options.InitialValue;
        MultiLineInput.MinimumHeightRequest = 110;
    }

    /// <summary>
    /// Reads the current input and validates it against the configured constraints.
    /// </summary>
    /// <param name="value">Receives the current input text, or an empty string when the field is empty.</param>
    /// <returns><see langword="true"/> when validation succeeds; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(out string value)
    {
        value = options.IsMultiline ? MultiLineInput.Text ?? string.Empty : SingleLineInput.Text ?? string.Empty;
        string? error = options.Validate(value);
        ValidationLabel.Text = error;
        ValidationLabel.IsVisible = !string.IsNullOrWhiteSpace(error);

        return error is null;
    }

    private void InputTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (ValidationLabel.IsVisible) ValidationLabel.IsVisible = false;
    }
}