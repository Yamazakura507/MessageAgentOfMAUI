namespace MessageAgent.Services.Models
{
    /// <summary>
    /// Represents the coordinated colors used to render the status overflow actions.
    /// </summary>
    /// <param name="Surface">The visual palette defining the action surface, border and text colors.</param>
    /// <param name="FirstCircleColor">The derived color used for the first decorative circle in the action animation.</param>
    /// <param name="SecondCircleColor">The base border color used for the second decorative circle in the action animation.</param>
    /// <param name="ThirdCircleColor">The derived color used for the third decorative circle in the action animation.</param>
    internal readonly record struct StatusActionPalette(
        StatusVisualPalette Surface,
        Color FirstCircleColor,
        Color SecondCircleColor,
        Color ThirdCircleColor);
}
