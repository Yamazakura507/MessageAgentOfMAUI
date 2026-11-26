namespace MessageAgent.Core.Enums.Dialog;

/// <summary>
/// Visual distortion intensity for text CAPTCHA.
/// </summary>
public enum MessageCaptchaDistortionLevel
{
    /// <summary>
    /// No additional distortion or noise.
    /// </summary>
    None,

    /// <summary>
    /// Slight character rotation and a few artifacts.
    /// </summary>
    Low,

    /// <summary>
    /// Moderate rotation, spacing variation and artifacts.
    /// </summary>
    Medium,

    /// <summary>
    /// Strong character deformation, overlap and dense artifacts.
    /// </summary>
    High,

    /// <summary>
    /// Maximum supported deformation and visual noise.
    /// </summary>
    Extreme
}
