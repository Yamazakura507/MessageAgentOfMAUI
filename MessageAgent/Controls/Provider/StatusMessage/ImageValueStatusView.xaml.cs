using MessageAgent.Core.Models;

namespace MessageAgent.Controls.Provider.StatusMessage;

/// <summary>
/// XAML-styled image renderer for an ImageValueStatus.
/// </summary>
public partial class ImageValueStatusView : ContentView
{
    public ImageValueStatusView(ImageValueStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        InitializeComponent();

        BindingContext = value;
    }
}
