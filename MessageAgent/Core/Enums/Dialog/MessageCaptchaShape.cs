namespace MessageAgent.Core.Enums.Dialog
{
    /// <summary>
    /// Shape used to cut the movable piece from the source image.
    /// </summary>
    public enum MessageCaptchaShape
    {
        /// <summary>
        /// Uses a triangular puzzle-piece cutout.
        /// </summary>
        Triangle,
        /// <summary>
        /// Uses a square puzzle-piece cutout.
        /// </summary>
        Square,
        /// <summary>
        /// Uses a circular puzzle-piece cutout.
        /// </summary>
        Circle,
        /// <summary>
        /// Uses a star-shaped puzzle-piece cutout.
        /// </summary>
        Star,
        /// <summary>
        /// Uses a diamond-shaped puzzle-piece cutout.
        /// </summary>
        Diamond,
        /// <summary>
        /// Uses a generated custom puzzle-piece contour.
        /// </summary>
        Custom,
        /// <summary>
        /// Selects one of the supported puzzle-piece shapes at random.
        /// </summary>
        Random
    }
}
