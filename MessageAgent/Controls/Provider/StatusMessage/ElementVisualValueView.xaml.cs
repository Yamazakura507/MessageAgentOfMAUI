using MessageAgent.Core.Models.VisualValues;

namespace MessageAgent.Controls.Provider.StatusMessage;

/// <summary>
/// Hosts the custom MAUI view supplied by an <see cref="ElementVisualValue"/>.
/// </summary>
public partial class ElementVisualValueView : ContentView
{
    /// <summary>
    /// Initializes the container with the supplied custom view.
    /// </summary>
    /// <param name="value">The element visual value to display.</param>
    public ElementVisualValueView(ElementVisualValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        InitializeComponent();

        BindingContext = value;
    }
}