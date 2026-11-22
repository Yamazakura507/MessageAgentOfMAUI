using MessageAgent.Core.Models.VisualValues.Abstract;

namespace MessageAgent.Core.Models.VisualValues;

/// <summary>
/// Represents an image-based visual value. The same model can be used as a status image
/// or as a link icon.
/// </summary>
public sealed record ImageVisualValue : VisualValue
{
    /// <summary>
    /// Gets or sets the MAUI image source.
    /// </summary>
    public ImageSource? Source { get; set; }

    /// <summary>
    /// Initializes an empty image visual value for XAML.
    /// </summary>
    public ImageVisualValue() { }

    /// <summary>
    /// Initializes an image visual value with its image source and optional accessible text.
    /// </summary>
    public ImageVisualValue(ImageSource source, string? alternativeText = null)
    {
        Source = source;
        AlternativeText = alternativeText;
    }

    /// <summary>
    /// Creates a file-backed image visual value. SVG MauiImage resources use the generated PNG extension at runtime.
    /// </summary>
    /// <param name="fileName">Image file name.</param>
    /// <param name="alternativeText">Optional accessible text.</param>
    /// <returns>A visual value backed by the specified file.</returns>
    public static ImageVisualValue FromFile(string fileName, string? alternativeText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string extension = Path.GetExtension(fileName);
        string runtimeFileName = 
            extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ? Path.ChangeExtension(fileName, ".png") : fileName;

        return new ImageVisualValue(ImageSource.FromFile(runtimeFileName), alternativeText);
    }
}
