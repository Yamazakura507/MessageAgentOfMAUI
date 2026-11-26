namespace MessageAgent.Core.Models.Dialogs.Options
{
    /// <summary>
    /// Button configuration for a message-backed question dialog.
    /// </summary>
    public sealed class MessageQuestionOptions
    {
        /// <summary>
        /// When null, the built-in Yes/No buttons are used.
        /// </summary>
        public IReadOnlyList<MessageQuestionButton>? Buttons { get; set; }
        /// <summary>
        /// Gets or sets whether the default button set includes an additional OK button.
        /// </summary>
        public bool IncludeOk { get; set; }
        /// <summary>
        /// Gets or sets the caption of the default Yes button.
        /// </summary>
        public string YesText { get; set; } = "Да";
        /// <summary>
        /// Gets or sets the caption of the default No button.
        /// </summary>
        public string NoText { get; set; } = "Нет";
        /// <summary>
        /// Gets or sets the caption of the optional default OK button.
        /// </summary>
        public string OkText { get; set; } = "ОК";

        internal IReadOnlyList<MessageQuestionButton> ResolveButtons()
        {
            IReadOnlyList<MessageQuestionButton> result = Buttons
                ?? (IncludeOk
                    ? new[]
                    {
                        new MessageQuestionButton(YesText, "Yes", true),
                        new MessageQuestionButton(NoText, "No"),
                        new MessageQuestionButton(OkText, "OK")
                    }
                    : new[]
                    {
                        new MessageQuestionButton(YesText, "Yes", true),
                        new MessageQuestionButton(NoText, "No")
                    });

            if (result.Count is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(Buttons), "В вопросительном диалоге должно быть от 1 до 5 кнопок.");
            if (result.Any(button => string.IsNullOrWhiteSpace(button.Text) || string.IsNullOrWhiteSpace(button.Value)))
                throw new ArgumentException("У каждой кнопки должны быть заданы Text и Value.", nameof(Buttons));

            return result;
        }
    }
}
