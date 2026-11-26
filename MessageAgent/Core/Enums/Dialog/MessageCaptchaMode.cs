namespace MessageAgent.Core.Enums.Dialog
{
    /// <summary>
    /// Visual mode of the CAPTCHA challenge.
    /// </summary>
    public enum MessageCaptchaMode
    {
        /// <summary>
        /// Uses a distorted text challenge that the user must type.
        /// </summary>
        Text,
        /// <summary>
        /// Uses an image puzzle in which the user moves a cutout piece to its target.
        /// </summary>
        Image
    }
}
