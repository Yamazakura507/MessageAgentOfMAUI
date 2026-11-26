using MessageAgent.Core.Enums.Dialog;

namespace MessageAgent.Core.Models.Dialogs.Options
{
    /// <summary>
    /// Configuration for text and image puzzle CAPTCHA dialogs.
    /// </summary>
    public sealed class MessageCaptchaOptions
    {
        /// <summary>
        /// Gets or sets whether the challenge uses text entry or an image puzzle.
        /// </summary>
        public MessageCaptchaMode Mode { get; set; } = MessageCaptchaMode.Text;

        /// <summary>
        /// Gets or sets the character source used to generate the text answer; non-alphanumeric characters are ignored.
        /// </summary>
        public string? SourceText { get; set; } = "MessageAgent";

        /// <summary>
        /// Gets or sets the requested text answer length, which is constrained to the supported range of 4 to 10 characters.
        /// </summary>
        public int TextLength { get; set; } = 6;

        /// <summary>
        /// Visual distortion strength of the generated text CAPTCHA.
        /// </summary>
        public MessageCaptchaDistortionLevel DistortionLevel { get; set; } = MessageCaptchaDistortionLevel.Medium;

        /// <summary>
        /// Gets or sets the instruction displayed above the challenge.
        /// </summary>
        public string Prompt { get; set; } = "Подтвердите, что вы не робот";

        /// <summary>
        /// Gets or sets the caption of the text CAPTCHA verification button.
        /// </summary>
        public string VerifyText { get; set; } = "Проверить";

        /// <summary>
        /// Gets or sets the caption of the cancel button.
        /// </summary>
        public string CancelText { get; set; } = "Отмена";

        /// <summary>
        /// Gets or sets the source image used by image-puzzle mode; it is required when <see cref="Mode"/> is <see cref="MessageCaptchaMode.Image"/>.
        /// </summary>
        public ImageSource? Image { get; set; }

        /// <summary>
        /// Gets or sets the shape of the movable puzzle piece.
        /// </summary>
        public MessageCaptchaShape Shape { get; set; } = MessageCaptchaShape.Random;

        /// <summary>
        /// Gets or sets the axis or area in which the puzzle piece can move.
        /// </summary>
        public MessageCaptchaPosition Position { get; set; } = MessageCaptchaPosition.Random;

        /// <summary>
        /// Accepted overlap loss, from 0 (exact) to 0.5 (more forgiving).
        /// </summary>
        public double Tolerance { get; set; } = 0.22;
    }
}
