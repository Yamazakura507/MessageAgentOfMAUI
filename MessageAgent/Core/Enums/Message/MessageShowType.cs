namespace MessageAgent.Core.Enums.Message
{
    /// <summary>
    /// Controls when the compact message label is visible and how it responds to input.
    /// </summary>
    public enum MessageShowType
    {
        /// <summary>
        /// Keeps the label visible until the hosting message is removed.
        /// </summary>
        ShowLableAlways,
        /// <summary>
        /// Toggles the label visibility when the notification is clicked.
        /// </summary>
        ShowAndHideByClick,
        /// <summary>
        /// Shows the label on click and hides it after the configured timeout.
        /// </summary>
        ShowByClickAndAutoHide,
        /// <summary>
        /// Keeps the compact label hidden while allowing the details dialog to open.
        /// </summary>
        HideLableAlways
    }
}
