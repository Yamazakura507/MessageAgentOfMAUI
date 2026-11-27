using MessageAgent.Controls.Provider.Abstract;
using MessageAgent.Core.Enums.Loading;
using MessageAgent.Core.Models.Loading;
using MessageAgent.Resources.ExtensionResources;
using MessageAgent.Services.Extensions;
using MessageAgent.Services.Factory;
using MessageAgent.Services.Interactions;
using ResourcesLib.Models;

namespace MessageAgent.Controls.Loading;

/// <summary>
/// Modal loading dialog hosting LoadingView.
/// </summary>
public partial class LoadingDialogPage : AnimatedModalPage
{
    private readonly LoadingDialogMode dialogMode;
    private LottiePlaybackInteraction? closeInteraction;
    private bool isClosing;

    private bool HasDecoratedDialog => dialogMode is LoadingDialogMode.Base or LoadingDialogMode.Dynamic;
    private bool HasCloseElement => dialogMode is LoadingDialogMode.Base or LoadingDialogMode.Dynamic or LoadingDialogMode.TransparentAndCancelElement;
    private bool HasCancelButton => dialogMode is LoadingDialogMode.Base or LoadingDialogMode.Dynamic;

    /// <summary>
    /// Gets the accent inferred from the active Lottie animation.
    /// </summary>
    public Color DialogAccentColor { get; private set; }

    /// <summary>
    /// Gets the resource-backed dialog surface tinted by the animation accent.
    /// </summary>
    public Color DialogSurfaceColor
    {
        get
        {
            if (!HasDecoratedDialog) return Colors.Transparent;

            Color surface = MessageColorPalette.GetThemeColor(
                MessageAgentResources.DialogSurfaceLightKey,
                MessageAgentResources.DialogSurfaceDarkKey);
            double tint = MessageColorPalette.GetThemeDouble(
                MessageAgentResources.DialogSurfaceTintLightKey,
                MessageAgentResources.DialogSurfaceTintDarkKey);

            return Blend(surface, DialogAccentColor, (float)tint);
        }
    }

    /// <summary>
    /// Gets the resource-backed foreground color for dialog content.
    /// </summary>
    public Color DialogForegroundColor => MessageColorPalette.GetThemeColor(
        MessageAgentResources.DialogForegroundLightKey,
        MessageAgentResources.DialogForegroundDarkKey);

    /// <summary>
    /// Gets a contrast-safe foreground for the accent cancel button.
    /// </summary>
    public Color DialogForegroundOnAccent => MessageColorPalette.EnsureContrast(
        MessageAgentResources.GetColor(MessageAgentResources.AccentContentFallbackKey),
        DialogAccentColor,
        4.5d);

    /// <summary>
    /// Creates a standard loading dialog.
    /// </summary>
    public LoadingDialogPage() : this(LoadingDialogMode.Base) { }

    /// <summary>
    /// Creates a loading dialog with the requested visual mode.
    /// </summary>
    public LoadingDialogPage(LoadingDialogMode dialogMode)
    {
        this.dialogMode = dialogMode;
        DialogAccentColor = MessageAgentResources.GetColor(MessageAgentColorKeys.LoadingAccent);

        InitializeComponent();

        BindingContext = this;

        ConfigureMode();

        Loading.TrackDominantColor = dialogMode == LoadingDialogMode.Dynamic;
        Loading.PropertyChanged += LoadingOnPropertyChanged;

        if (HasCloseElement) ApplyCloseAnimation(DialogAccentColor);
    }

    private void ConfigureMode()
    {
        Header.IsVisible = HasDecoratedDialog || HasCloseElement;
        TitleLabel.IsVisible = HasDecoratedDialog;
        CloseButton.IsVisible = HasCloseElement;
        CancelButton.IsVisible = HasCancelButton;

        Loading.ShowBackdrop = false;
        Loading.ShowSurface = false;
        Loading.ShowStatus = true;

        if (HasDecoratedDialog) return;

        Overlay.Padding = new Thickness(0);
        Overlay.BackgroundColor = Colors.Transparent;

        DialogPanel.Padding = new Thickness(0);
        DialogPanel.HorizontalOptions = LayoutOptions.Fill;
        DialogPanel.VerticalOptions = LayoutOptions.Fill;
        DialogPanel.MaximumWidthRequest = double.PositiveInfinity;
        DialogPanel.MaximumHeightRequest = double.PositiveInfinity;
        DialogPanel.BackgroundColor = Colors.Transparent;
        DialogPanel.Stroke = Colors.Transparent;
        DialogPanel.StrokeThickness = 0;

        Loading.HorizontalOptions = LayoutOptions.Fill;
        Loading.VerticalOptions = LayoutOptions.Fill;
    }

    /// <summary>
    /// Executes actions and closes the dialog after the result is available.
    /// </summary>
    public Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        IEnumerable<LoadingAnimation>? animations,
        LoadingExecutionMode executionMode,
        CancellationToken cancellationToken = default,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process) => ExecuteAsync(
            actions,
            animations,
            executionMode,
            false,
            cancellationToken,
            maxVisibleTasks,
            sortProgress);

    /// <summary>
    /// Executes actions with optional per-action progress and closes the dialog after completion.
    /// </summary>
    public async Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        IEnumerable<LoadingAnimation>? animations,
        LoadingExecutionMode executionMode,
        bool isShowProgress,
        CancellationToken cancellationToken = default,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
    {
        LoadingResult result = await Loading.ExecuteAsync(
            actions,
            isShowProgress,
            animations,
            executionMode,
            cancellationToken,
            maxVisibleTasks,
            sortProgress);

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

            if (HasCloseElement) ApplyCloseAnimation(DialogAccentColor);
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
}