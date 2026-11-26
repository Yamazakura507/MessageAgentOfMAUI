using System.Globalization;
using System.Text.RegularExpressions;

namespace MessageAgent.Core.Models.Dialogs.Options
{
    /// <summary>
    /// Input constraints for a message-backed text prompt.
    /// </summary>
    public sealed class MessageInputOptions
    {
        /// <summary>
        /// Gets or sets the placeholder shown while the input is empty.
        /// </summary>
        public string? Placeholder { get; set; } = "Введите значение";
        /// <summary>
        /// Gets or sets the text initially placed in the input field.
        /// </summary>
        public string? InitialValue { get; set; }
        /// <summary>
        /// Gets or sets the caption of the accept button.
        /// </summary>
        public string? AcceptText { get; set; } = "Подтвердить";
        /// <summary>
        /// Gets or sets the caption of the cancel button.
        /// </summary>
        public string? CancelText { get; set; } = "Отмена";
        /// <summary>
        /// Gets or sets the minimum permitted input length.
        /// </summary>
        public int MinimumLength { get; set; }
        /// <summary>
        /// Gets or sets the maximum permitted input length and the input control character limit.
        /// </summary>
        public int MaximumLength { get; set; } = 256;
        /// <summary>
        /// Gets or sets whether an empty or whitespace-only value is rejected.
        /// </summary>
        public bool IsRequired { get; set; } = true;
        /// <summary>
        /// Gets or sets whether the single-line input masks the entered characters.
        /// </summary>
        public bool IsPassword { get; set; }
        /// <summary>
        /// Gets or sets whether the input uses a multiline editor instead of a single-line entry.
        /// </summary>
        public bool IsMultiline { get; set; }
        /// <summary>
        /// Gets or sets the keyboard type requested for the input field.
        /// </summary>
        public Keyboard Keyboard { get; set; } = Keyboard.Default;
        /// <summary>
        /// Gets or sets the set of characters permitted in the value; when specified, every entered character must belong to this set.
        /// </summary>
        public string? AllowedCharacters { get; set; }
        /// <summary>
        /// Gets or sets the set of characters that must not occur in the value.
        /// </summary>
        public string? DisallowedCharacters { get; set; }
        /// <summary>
        /// Gets or sets a regular expression that the complete input value must match.
        /// </summary>
        public string? RegularExpression { get; set; }
        /// <summary>
        /// Gets or sets the inclusive minimum numeric value when numeric validation is enabled.
        /// </summary>
        public double? MinimumNumber { get; set; }
        /// <summary>
        /// Gets or sets the inclusive maximum numeric value when numeric validation is enabled.
        /// </summary>
        public double? MaximumNumber { get; set; }
        /// <summary>
        /// Gets or sets an additional validator that returns <see langword="null"/> for a valid value or an error message when validation fails.
        /// </summary>
        public Func<string, string?>? Validator { get; set; }

        internal string? Validate(string? value)
        {
            value ??= string.Empty;
            if (IsRequired && string.IsNullOrWhiteSpace(value)) return "Это поле обязательно для заполнения.";
            if (!IsRequired && string.IsNullOrWhiteSpace(value)) return Validator?.Invoke(value);
            if (value.Length < MinimumLength) return $"Минимальная длина: {MinimumLength}.";
            if (value.Length > MaximumLength) return $"Максимальная длина: {MaximumLength}.";
            if (!string.IsNullOrEmpty(AllowedCharacters) && value.Any(character => !AllowedCharacters.Contains(character))) return "Используются недопустимые символы.";
            if (!string.IsNullOrEmpty(DisallowedCharacters) && value.Any(DisallowedCharacters.Contains)) return "Значение содержит запрещённые символы.";
            if (!string.IsNullOrWhiteSpace(RegularExpression))
            {
                try
                {
                    if (!Regex.IsMatch(value, RegularExpression, RegexOptions.CultureInvariant)) return "Значение не соответствует требуемому формату.";
                }
                catch (ArgumentException)
                {
                    return "Настроено некорректное регулярное выражение.";
                }
            }

            if (MinimumNumber.HasValue || MaximumNumber.HasValue)
            {
                if (!double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out double number)
                    && !double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number)) return "Введите корректное число.";
                if (MinimumNumber.HasValue && number < MinimumNumber.Value) return $"Минимальное значение: {MinimumNumber.Value}.";
                if (MaximumNumber.HasValue && number > MaximumNumber.Value) return $"Максимальное значение: {MaximumNumber.Value}.";
            }

            return Validator?.Invoke(value);
        }
    }
}
