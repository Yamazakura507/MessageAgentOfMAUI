using MessageAgent.Controls.Provider.Abstract;

namespace MessageAgent.Controls.Provider;

/// <summary>
/// Bottom sheet used to display a status description on touch devices.
/// </summary>
public partial class StatusDescriptionPage : AnimatedModalPage
{
    /// <summary>
    /// Gets the status title displayed by this description sheet.
    /// </summary>
    public string DescriptionTitle { get; }

    /// <summary>
    /// Gets the explanatory status text.
    /// </summary>
    public string DescriptionText { get; }

    /// <summary>
    /// Initializes the sheet with a title and description.
    /// </summary>
    /// <param name="title">The status title.</param>
    /// <param name="description">The status description.</param>
    public StatusDescriptionPage(string title, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        DescriptionTitle = title;
        DescriptionText = description;

        InitializeComponent();

        BindingContext = this;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Overlay.Opacity = 0;
        BottomSheet.TranslationY = 500;

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 150, Easing.CubicOut),
            BottomSheet.TranslateToAsync(0, 0, 260, Easing.CubicOut));
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e) => _ = CloseAsync();

    private void OnCloseClicked(object? sender, EventArgs e) => _ = CloseAsync();

    /// <inheritdoc />
    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 140, Easing.CubicIn),
        BottomSheet.TranslateToAsync(0, BottomSheet.Height, 180, Easing.CubicIn));
}
