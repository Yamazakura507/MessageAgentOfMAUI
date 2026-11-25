using MessageAgent.Core.Models.Dialogs.Options;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// XAML input control with centralized validation from MessageInputOptions.
/// </summary>
public partial class MessageInputView : ContentView
{
    private readonly MessageInputOptions options;

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