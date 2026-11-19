namespace MessageAgent.Core.Models;

/// <summary>
/// Displays any MAUI-supported image source.
/// SVG files included as MauiImage are converted to PNG by the MAUI resource pipeline.
/// </summary>
public sealed record ImageValueStatus : ValueStatus
{
    public ImageSource? Source { get; set; }
    public string? AlternativeText { get; set; }

    public ImageValueStatus() { }

    public ImageValueStatus(ImageSource source, string? alternativeText = null)
    {
        Source = source;
        AlternativeText = alternativeText;
    }

    /// <summary>
    /// Creates a file-backed image status. For an SVG MauiImage resource, the runtime
    /// resource name uses the generated .png extension.
    /// </summary>
    public static ImageValueStatus FromFile(string fileName, string? alternativeText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string extension = Path.GetExtension(fileName);
        string runtimeFileName = 
            extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ? Path.ChangeExtension(fileName, ".png") : fileName;

        return new ImageValueStatus(ImageSource.FromFile(runtimeFileName), alternativeText);
    }
}
