using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Enums.Loading;
using MessageAgent.Core.Models.Loading;
using MessageAgent.Resources.ExtensionResources;
using MessageAgent.Services.Factory;
using MessageAgent.Services.Interactions;
using ResourcesLib.Models;

namespace MessageAgent.Controls.Loading;

/// <summary>
/// Modal loading dialog that hosts <see cref="LoadingView"/> and cancels its actions
/// when the close, cancel, or back controls are activated.
/// </summary>
public partial class LoadingDialogPage : AnimatedModalPage
{
    private LottiePlaybackInteraction? closeInteraction;
    private bool isClosing;

    /// <summary>
    /// Gets the accent color inferred from the active Lottie animation.
    /// </summary>
    public Color DialogAccentColor { get; private set; }

    /// <summary>
    /// Gets the tinted surface color for the dialog panel.
    /// </summary>
    public Color DialogSurfaceColor => Blend(
        Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#1B1F25") : Color.FromArgb("#FBFAF8"),
        DialogAccentColor,
        0.12f);

    /// <summary>
    /// Gets the foreground color used on the dialog surface.
    /// </summary>
    public Color DialogForegroundColor => Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#F5F6FA") : Color.FromArgb("#1B1C21");

    /// <summary>
    /// Gets a contrasting foreground color for the accent-colored cancel button.
    /// </summary>
    public Color DialogForegroundOnAccent => GetLuminance(DialogAccentColor) > 0.58f ? Colors.Black : Colors.White;

    /// <summary>
    /// Initializes the modal loading dialog and its interactive close animation.
    /// </summary>
    public LoadingDialogPage()
    {
        DialogAccentColor = MessageAgentResources.GetColor(MessageAgentColorKeys.LoadingAccent);

        InitializeComponent();

        BindingContext = this;

        Loading.PropertyChanged += LoadingOnPropertyChanged;

        ApplyCloseAnimation(DialogAccentColor);
    }

    /// <summary>
    /// Executes the action set, then closes the dialog after the result is available.
    /// </summary>
    /// <param name="actions">The actions to execute.</param>
    /// <param name="animations">Optional animations; one is selected randomly.</param>
    /// <param name="executionMode">Parallel by default, or sequential.</param>
    /// <param name="cancellationToken">An external cancellation token.</param>
    /// <returns>The operation status, elapsed time, and any exception.</returns>
    public async Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        IEnumerable<LoadingAnimation>? animations,
        LoadingExecutionMode executionMode,
        CancellationToken cancellationToken = default)
    {
        LoadingResult result = await Loading.ExecuteAsync(actions, animations, executionMode, cancellationToken);

        if (!isClosing)
        {
            isClosing = true;

            await CloseAsync();
        }

        return result;
    }

    private void LoadingOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LoadingView.AccentColor)) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            DialogAccentColor = Loading.AccentColor;

            OnPropertyChanged(nameof(DialogAccentColor));
            OnPropertyChanged(nameof(DialogSurfaceColor));
            OnPropertyChanged(nameof(DialogForegroundOnAccent));
            ApplyCloseAnimation(DialogAccentColor);
        });
    }

    private void ApplyCloseAnimation(Color accentColor)
    {
        LottieResourceSettings settings = MessageAgentLottieSettingsFactory.GetCloseSettings(accentColor);

        MessageAgentLottieSettingsFactory.Apply(CloseAnimation, settings);

        closeInteraction?.Dispose();
        closeInteraction = new LottiePlaybackInteraction(
            CloseAnimation,
            CloseButton,
            settings.RestingProgress ?? 1d,
            activateOnHover: true,
            resetToRestingOnDeactivate: true);
    }

    private async void CancelOnClicked(object? sender, EventArgs e)
    {
        Loading.Cancel();

        if (!isClosing)
        {
            isClosing = true;

            await CloseAsync();
        }
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Overlay.Opacity = 0;
        DialogPanel.Opacity = 0;
        DialogPanel.Scale = 0.94;

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 160, Easing.CubicOut),
            DialogPanel.FadeToAsync(1, 210, Easing.CubicOut),
            DialogPanel.ScaleToAsync(1, 230, Easing.CubicOut));
    }

    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        Loading.Cancel();

        if (!isClosing)
        {
            isClosing = true;

            _ = CloseAsync();
        }

        return true;
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        Loading.Cancel();

        Loading.PropertyChanged -= LoadingOnPropertyChanged;

        closeInteraction?.Dispose();

        closeInteraction = null;

        base.OnDisappearing();
    }

    /// <inheritdoc />
    protected override Task AnimateOutAsync() => Task.WhenAll(
        Overlay.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.FadeToAsync(0, 130, Easing.CubicIn),
        DialogPanel.ScaleToAsync(0.96, 150, Easing.CubicIn));

    private static Color Blend(Color background, Color foreground, float amount)
    {
        float factor = Math.Clamp(amount, 0f, 1f);

        return new Color(
            background.Red + (foreground.Red - background.Red) * factor,
            background.Green + (foreground.Green - background.Green) * factor,
            background.Blue + (foreground.Blue - background.Blue) * factor,
            1f);
    }

    private static float GetLuminance(Color color) => 0.2126f * color.Red + 0.7152f * color.Green + 0.0722f * color.Blue;
}
