namespace MessageAgent.Core.Enums.Dialog
{
    /// <summary>
    /// Axis or area in which the CAPTCHA piece can be moved.
    /// </summary>
    public enum MessageCaptchaPosition
    {
        /// <summary>
        /// Allows the puzzle piece to move along the horizontal axis.
        /// </summary>
        Horizontal,
        /// <summary>
        /// Allows the puzzle piece to move along the vertical axis.
        /// </summary>
        Vertical,
        /// <summary>
        /// Allows the puzzle piece to move along both axes.
        /// </summary>
        FullArea,
        /// <summary>
        /// Selects one of the supported movement directions at random.
        /// </summary>
        Random
    }
}
