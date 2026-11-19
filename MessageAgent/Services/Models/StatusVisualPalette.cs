namespace MessageAgent.Services.Models
{
    /// <summary>
    /// A status badge palette derived from its configured outline color.
    /// </summary>
    public readonly record struct StatusVisualPalette(Color BorderColor, Color BackgroundColor, Color TextColor);
}
