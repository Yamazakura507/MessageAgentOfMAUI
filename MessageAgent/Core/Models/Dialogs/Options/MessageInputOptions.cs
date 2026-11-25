using System.Globalization;
using System.Text.RegularExpressions;

namespace MessageAgent.Core.Models.Dialogs.Options
{
    /// <summary>
    /// Input constraints for a message-backed text prompt.
    /// </summary>
    public sealed class MessageInputOptions
    {
        public string? Placeholder { get; set; } = "Введите значение";
        public string? InitialValue { get; set; }
        public string? AcceptText { get; set; } = "Подтвердить";
        public string? CancelText { get; set; } = "Отмена";
        public int MinimumLength { get; set; }
        public int MaximumLength { get; set; } = 256;
        public bool IsRequired { get; set; } = true;
        public bool IsPassword { get; set; }
        public bool IsMultiline { get; set; }
        public Keyboard Keyboard { get; set; } = Keyboard.Default;
        public string? AllowedCharacters { get; set; }
        public string? DisallowedCharacters { get; set; }
        public string? RegularExpression { get; set; }
        public double? MinimumNumber { get; set; }
        public double? MaximumNumber { get; set; }
        public Func<string, string?>? Validator { get; set; }

        internal string? Validate(string? value)
        {
            value ??= string.Empty;
            if (IsRequired && string.IsNullOrWhiteSpace(value)) return "Это поле обязательно для заполнения.";
            if (!IsRequired && string.IsNullOrWhiteSpace(value)) return Validator?.Invoke(value);
            if (value.Length < MinimumLength) return $"Минимальная длина: {MinimumLength}.";
            if (value.Length > MaximumLength) return $"Максимальная длина: {MaximumLength}.";
            if (!string.IsNullOrEmpty(AllowedCharacters)
                && value.Any(character => !AllowedCharacters.Contains(character))) return "Используются недопустимые символы.";
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
                    && !double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number))
                    return "Введите корректное число.";
                if (MinimumNumber.HasValue && number < MinimumNumber.Value) return $"Минимальное значение: {MinimumNumber.Value}.";
                if (MaximumNumber.HasValue && number > MaximumNumber.Value) return $"Максимальное значение: {MaximumNumber.Value}.";
            }

            return Validator?.Invoke(value);
        }
    }
}
