using MessageAgent.Core.Enums.Dialog;

namespace MessageAgent.Core.Models.Dialogs.Options
{
    /// <summary>
    /// Configuration for text and image puzzle CAPTCHA dialogs.
    /// </summary>
    public sealed class MessageCaptchaOptions
    {
        public MessageCaptchaMode Mode { get; set; } = MessageCaptchaMode.Text;

        public string? SourceText { get; set; } = "MessageAgent";

        public int TextLength { get; set; } = 6;

        /// <summary>
        /// Visual distortion strength of the generated text CAPTCHA.
        /// </summary>
        public MessageCaptchaDistortionLevel DistortionLevel { get; set; } = MessageCaptchaDistortionLevel.Medium;

        public string Prompt { get; set; } = "Подтвердите, что вы не робот";

        public string VerifyText { get; set; } = "Проверить";

        public string CancelText { get; set; } = "Отмена";

        public ImageSource? Image { get; set; }

        public MessageCaptchaShape Shape { get; set; } = MessageCaptchaShape.Random;

        public MessageCaptchaPosition Position { get; set; } = MessageCaptchaPosition.Random;

        /// <summary>
        /// Accepted overlap loss, from 0 (exact) to 0.5 (more forgiving).
        /// </summary>
        public double Tolerance { get; set; } = 0.22;
    }
}
