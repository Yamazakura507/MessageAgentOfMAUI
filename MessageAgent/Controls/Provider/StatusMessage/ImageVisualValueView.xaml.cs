using MessageAgent.Core.Models.VisualValues;

namespace MessageAgent.Controls.Provider.StatusMessage;

/// <summary>
/// Displays an image-based <see cref="ImageVisualValue"/>.
/// </summary>
public partial class ImageVisualValueView : ContentView
{
    /// <summary>
    /// Initializes the image view with its visual value.
    /// </summary>
    /// <param name="value">The image visual value to display.</param>
    public ImageVisualValueView(ImageVisualValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        InitializeComponent();

        BindingContext = value;
    }
}
