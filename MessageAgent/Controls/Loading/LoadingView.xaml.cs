using System.Diagnostics;
using System.Text.Json;
using LottieAgent.Controls;
using LottieAgent.Services;
using MessageAgent.Core.Enums.Loading;
using MessageAgent.Core.Models.Loading;
using MessageAgent.Services;

namespace MessageAgent.Controls.Loading;

/// <summary>
/// Displays a blocking Lottie overlay while executing cancellable actions.
/// The hosted content is restored to its original enabled state after every outcome.
/// </summary>
public partial class LoadingView : ContentView, IDisposable
{
    /// <summary>
    /// Identifies the view hosted beneath the loading overlay.
    /// </summary>
    public static readonly BindableProperty HostContentProperty = BindableProperty.Create(
        nameof(HostContent),
        typeof(View),
        typeof(LoadingView),
        default(View),
        propertyChanged: (bindable, _, newValue) => ((LoadingView)bindable).originalContent = newValue as View);

    /// <summary>
    /// Gets or sets the content displayed beneath the loading overlay.
    /// </summary>
    public View? HostContent
    {
        get => (View?)GetValue(HostContentProperty);
        set => SetValue(HostContentProperty, value);
    }
    private static readonly LoadingAnimation DefaultAnimation = new("Lotties/loading.json", accentColor: Color.FromArgb("#2785D8"));

    private readonly Stopwatch stopwatch = new();
    private readonly Dictionary<VisualElement, bool> originalEnabledStates = new(ReferenceEqualityComparer.Instance);
    private CancellationTokenSource? executionSource;
    private TimeSpan lastExecutionTime;
    private bool showBackdrop = true;
    private bool showSurface = true;
    private bool showStatus = true;
    private bool isRunning;
    private bool isDisposed;
    private View? originalContent;
    private Action? restoreHost;
    private Action? restoreAnimationView;
    private LottieView? activeExternalView;
    private bool externalPreviousIsPlaying;
    private bool externalPreviousIsLooping;
    private double externalPreviousWidth;
    private double externalPreviousHeight;
    private double animationAspectRatio = 1d;
    private LoadingStatus status = LoadingStatus.Pending;

    /// <summary>
    /// Gets the duration of the current operation at the moment of access, or the last duration after completion.
    /// </summary>
    public TimeSpan LastExecutionTime => stopwatch.IsRunning ? stopwatch.Elapsed : lastExecutionTime;

    /// <summary>
    /// Gets whether an action set is currently being executed.
    /// </summary>
    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            if (isRunning == value) return;

            isRunning = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(LastExecutionTime));
        }
    }

    /// <summary>
    /// Gets the current or most recently completed operation state.
    /// </summary>
    public LoadingStatus Status
    {
        get => status;
        private set
        {
            if (status == value) return;

            status = value;

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the accent color inferred from the active Lottie animation.
    /// </summary>
    public Color AccentColor { get; private set; } = DefaultAnimation.AccentColor!;

    /// <summary>
    /// Gets or sets whether the translucent backdrop is visible.
    /// </summary>
    public bool ShowBackdrop
    {
        get => showBackdrop;
        set
        {
            if (showBackdrop == value) return;

            showBackdrop = value;

            OnPropertyChanged(nameof(OverlayColor));
        }
    }

    /// <summary>
    /// Gets or sets whether the animation is surrounded by a surface.
    /// </summary>
    public bool ShowSurface
    {
        get => showSurface;
        set
        {
            if (showSurface == value) return;

            showSurface = value;

            OnPropertyChanged(nameof(SurfaceColor));
            OnPropertyChanged(nameof(SurfaceStrokeColor));
            OnPropertyChanged(nameof(SurfacePadding));
            OnPropertyChanged(nameof(SurfaceStrokeThickness));
        }
    }

    /// <summary>
    /// Gets or sets whether the action description is displayed below the animation.
    /// </summary>
    public bool ShowStatus
    {
        get => showStatus;
        set
        {
            if (showStatus == value) return;

            showStatus = value;

            OnPropertyChanged(nameof(IsStatusVisible));
        }
    }

    /// <summary>
    /// Gets the backdrop color used by XAML.
    /// </summary>
    public Color OverlayColor => ShowBackdrop ? Color.FromArgb("#99000000") : Colors.Transparent;

    /// <summary>
    /// Gets the outer overlay padding.
    /// </summary>
    public Thickness OverlayPadding => ShowBackdrop || ShowSurface ? new Thickness(18) : new Thickness(0);

    /// <summary>
    /// Gets the accent-tinted surface background used by XAML.
    /// </summary>
    public Color SurfaceColor => ShowSurface ? AccentColor.WithAlpha(0.1f) : Colors.Transparent;

    /// <summary>
    /// Gets the surface stroke color used by XAML.
    /// </summary>
    public Color SurfaceStrokeColor => ShowSurface ? AccentColor : Colors.Transparent;

    /// <summary>
    /// Gets the surface padding used by XAML.
    /// </summary>
    public Thickness SurfacePadding => ShowSurface ? new Thickness(16) : new Thickness(0);

    /// <summary>
    /// Gets the surface stroke thickness used by XAML.
    /// </summary>
    public double SurfaceStrokeThickness => ShowSurface ? 1.5d : 0d;

    /// <summary>
    /// Gets whether the status caption is visible.
    /// </summary>
    public bool IsStatusVisible => ShowStatus;

    /// <summary>
    /// Gets the foreground color used by the status caption.
    /// </summary>
    public Color StatusColor => Color.FromArgb("#20242B");

    /// <summary>
    /// Initializes the loading overlay.
    /// </summary>
    public LoadingView()
    {
        InitializeComponent();

        Animation.AnimationLoaded += AnimationOnLoaded;
        SizeChanged += LoadingViewOnSizeChanged;
    }

    /// <summary>
    /// Wraps a view so it can be placed in a layout with a loading overlay.
    /// </summary>
    /// <param name="content">The view hosted beneath the overlay.</param>
    /// <returns>A loading container hosting the supplied view.</returns>
    public static LoadingView Wrap(View content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return new LoadingView 
        { 
            HostContent = content 
        };
    }

    /// <summary>
    /// Attaches a blocking overlay above a page and disables its original content while running.
    /// </summary>
    /// <param name="page">The page whose content should be covered.</param>
    /// <returns>The attached loading view.</returns>
    public static LoadingView AttachTo(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);

        ContentPage contentPage = page as ContentPage ?? throw new NotSupportedException("LoadingView.AttachTo(Page) requires a ContentPage.");
        View content = contentPage.Content ?? throw new InvalidOperationException("The page must have content before a loading overlay can be attached.");

        contentPage.Content = null;

        LoadingView loading = new() { HostContent = content };

        contentPage.Content = loading;

        loading.pageDisappearingHandler = (_, _) => loading.Cancel();
        contentPage.Disappearing += loading.pageDisappearingHandler;
        loading.restoreHost = () =>
        {
            loading.HostContent = null;

            if (ReferenceEquals(contentPage.Content, loading)) contentPage.Content = content;

            contentPage.Disappearing -= loading.pageDisappearingHandler;
        };

        return loading;
    }

    /// <summary>
    /// Attaches a blocking overlay above a ContentView and disables its original content while running.
    /// </summary>
    /// <param name="control">The control whose content should be covered.</param>
    /// <returns>The attached loading view.</returns>
    public static LoadingView AttachTo(ContentView control)
    {
        ArgumentNullException.ThrowIfNull(control);
        View content = control.Content ?? throw new InvalidOperationException("The ContentView must contain content before a loading overlay can be attached.");

        control.Content = null;

        LoadingView loading = new() { HostContent = content };

        control.Content = loading;

        loading.controlUnloadedHandler = (_, _) => loading.Cancel();
        control.Unloaded += loading.controlUnloadedHandler;
        loading.restoreHost = () =>
        {
            loading.HostContent = null;

            if (ReferenceEquals(control.Content, loading)) control.Content = content;

            control.Unloaded -= loading.controlUnloadedHandler;
        };

        return loading;
    }

    /// <summary>
    /// Attaches a blocking overlay to the active content page displayed by a window.
    /// </summary>
    /// <param name="window">The window whose active page should be covered.</param>
    /// <returns>The attached loading view.</returns>
    public static LoadingView AttachTo(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        Page page = window.Page switch
        {
            ContentPage contentPage => contentPage,
            Shell shell when shell.CurrentPage is ContentPage currentPage => currentPage,
            _ => throw new NotSupportedException("The window must host a ContentPage or a Shell with an active ContentPage.")
        };

        LoadingView loading = AttachTo(page);

        loading.restoreHost += () => window.Destroying -= loading.windowDestroyingHandler;
        loading.windowDestroyingHandler = (_, _) => loading.Cancel();
        window.Destroying += loading.windowDestroyingHandler;

        return loading;
    }

    /// <summary>
    /// Executes an action set using a random supplied animation or the built-in default.
    /// </summary>
    /// <param name="actions">The actions to execute.</param>
    /// <param name="animations">Optional animations; one is selected randomly for each execution.</param>
    /// <param name="executionMode">Parallel by default, or sequential.</param>
    /// <param name="cancellationToken">An external cancellation token.</param>
    /// <returns>The operation status, measured duration, and any exception.</returns>
    public Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default) => RunAsync(actions, animations, executionMode, cancellationToken);

    /// <summary>
    /// Executes an action set using one specified animation.
    /// </summary>
    /// <param name="actions">The actions to execute.</param>
    /// <param name="animation">The animation to display.</param>
    /// <param name="executionMode">Parallel by default, or sequential.</param>
    /// <param name="cancellationToken">An external cancellation token.</param>
    /// <returns>The operation status, measured duration, and any exception.</returns>
    public Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        LoadingAnimation animation,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(animation);

        return RunAsync(actions, new[] { animation }, executionMode, cancellationToken);
    }

    /// <summary>
    /// Requests cancellation of the current action set.
    /// </summary>
    public void Cancel()
    {
        try 
        {
            executionSource?.Cancel(); 
        }
        catch (ObjectDisposedException) 
        { 
        }
    }

    internal async Task<LoadingResult> RunAsync(
        IEnumerable<LoadingAction> actions,
        IEnumerable<LoadingAnimation>? animations,
        LoadingExecutionMode executionMode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actions);

        if (isDisposed) throw new ObjectDisposedException(nameof(LoadingView));

        LoadingAction[] work = actions.ToArray();

        if (work.Length == 0) throw new ArgumentException("At least one action is required.", nameof(actions));
        if (executionSource is not null) throw new InvalidOperationException("This LoadingView is already executing an action set.");

        LoadingAnimation[] choices = animations?.ToArray() ?? Array.Empty<LoadingAnimation>();
        LoadingAnimation selected = choices.Length == 0 ? DefaultAnimation : choices[Random.Shared.Next(choices.Length)];

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        executionSource = linked;
        stopwatch.Restart();
        Status = LoadingStatus.Running;

        Exception? failure = null;
        bool cancelled = false;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsRunning = true;
                IsVisible = true;

                DisableHostedVisualTree();

                StatusLabel.Text = string.Join(" · ", work.Select(action => action.Description)
                                                        .Where(description => !string.IsNullOrWhiteSpace(description)));

                if (string.IsNullOrWhiteSpace(StatusLabel.Text)) StatusLabel.Text = "Загрузка...";

                StatusLabel.IsVisible = ShowStatus;
                OverlayLayer.BackgroundColor = OverlayColor;
                LoadingPanel.BackgroundColor = SurfaceColor;
                LoadingPanel.Stroke = SurfaceStrokeColor;
                LoadingPanel.StrokeThickness = SurfaceStrokeThickness;
                LoadingPanel.Padding = SurfacePadding;
            });

            linked.Token.ThrowIfCancellationRequested();

            await ConfigureAnimationAsync(selected, linked.Token);

            if (executionMode == LoadingExecutionMode.Sequential) await ExecuteSequentialAsync(work, linked);
            else await ExecuteParallelAsync(work, linked);

            linked.Token.ThrowIfCancellationRequested();

            Status = LoadingStatus.Completed;
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            cancelled = true;

            Status = LoadingStatus.Cancelled;
        }
        catch (Exception exception)
        {
            failure = exception;

            linked.Cancel();

            Status = LoadingStatus.Failed;
        }
        finally
        {
            cancelled |= linked.IsCancellationRequested && failure is null;

            stopwatch.Stop();

            lastExecutionTime = stopwatch.Elapsed;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RestoreExternalAnimationView();
                Animation.Stop();
                RestoreHostedVisualTree();
                IsRunning = false;
                IsVisible = false;
                StatusLabel.IsVisible = ShowStatus;
                OnPropertyChanged(nameof(LastExecutionTime));
            });

            if (ReferenceEquals(executionSource, linked)) executionSource = null;
        }

        if (cancelled && failure is null) Status = LoadingStatus.Cancelled;

        return new LoadingResult(Status, lastExecutionTime, failure);
    }

    private async Task ExecuteSequentialAsync(LoadingAction[] actions, CancellationTokenSource source)
    {
        foreach (LoadingAction action in actions)
        {
            source.Token.ThrowIfCancellationRequested();

            try
            {
                await action.Execute(source.Token);

                source.Token.ThrowIfCancellationRequested();
            }
            catch
            {
                source.Cancel();

                throw;
            }
        }
    }

    private async Task ExecuteParallelAsync(LoadingAction[] actions, CancellationTokenSource source)
    {
        async Task RunOneAsync(LoadingAction action)
        {
            try
            {
                source.Token.ThrowIfCancellationRequested();

                await action.Execute(source.Token);

                source.Token.ThrowIfCancellationRequested();
            }
            catch
            {
                source.Cancel();

                throw;
            }
        }

        await Task.WhenAll(actions.Select(RunOneAsync));
    }

    private async Task ConfigureAnimationAsync(LoadingAnimation selected, CancellationToken cancellationToken)
    {
        if (selected.View is LottieView externalView)
        {
            Color externalAccent = selected.AccentColor ?? DefaultAnimation.AccentColor!;

            if (selected.AccentColor is null && !string.IsNullOrWhiteSpace(externalView.Source))
            {
                try
                {
                    string externalJson = await new LottieJsonLoader().LoadAsync(externalView.Source, cancellationToken);

                    externalAccent = TryExtractAccentColor(externalJson) ?? externalAccent;

                    using JsonDocument document = JsonDocument.Parse(externalJson);

                    if (document.RootElement.TryGetProperty("w", out JsonElement width)
                        && document.RootElement.TryGetProperty("h", out JsonElement height)
                        && width.TryGetDouble(out double w)
                        && height.TryGetDouble(out double h)
                        && w > 0d && h > 0d)
                        animationAspectRatio = w / h;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // A preconfigured Lottie view may use its own JSON loader; keep the supplied view intact.
                }
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RestoreExternalAnimationView();
                AnimationHost.Children.Remove(Animation);

                activeExternalView = externalView;
                externalPreviousIsPlaying = externalView.IsPlaying;
                externalPreviousIsLooping = externalView.IsLooping;
                externalPreviousWidth = externalView.WidthRequest;
                externalPreviousHeight = externalView.HeightRequest;
                restoreAnimationView = CaptureAndDetachParent(externalView);

                AnimationHost.Children.Insert(0, externalView);

                externalView.HorizontalOptions = LayoutOptions.Fill;
                externalView.VerticalOptions = LayoutOptions.Fill;
                externalView.IsLooping = true;
                externalView.IsPlaying = true;

                SetAccentColor(externalAccent);
                FitAnimation(externalView);
            });

            return;
        }

        string json = selected.RawJson ?? await new LottieJsonLoader().LoadAsync(selected.Source!, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        Color accent = selected.AccentColor ?? TryExtractAccentColor(json) ?? DefaultAnimation.AccentColor!;
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RestoreExternalAnimationView();
            if (!AnimationHost.Children.Contains(Animation))
                AnimationHost.Children.Insert(0, Animation);

            Animation.IsPlaying = false;
            Animation.JsonLoader = new RawLottieJsonLoader(json);
            Animation.Modifiers = selected.Modifiers;
            Animation.Source = "messageagent-loading-inline";
            Animation.IsLooping = true;
            Animation.Progress = selected.Progress;
            Animation.Speed = selected.Speed;
            Animation.IsReversed = selected.IsReversed;

            SetAccentColor(accent);
            UpdateAnimationAspectRatio(json);
            ConfigureSurfaceColors();
            Animation.Play();
        });
    }

    private void ConfigureSurfaceColors()
    {
        OnPropertyChanged(nameof(SurfaceColor));
        OnPropertyChanged(nameof(SurfaceStrokeColor));
        OnPropertyChanged(nameof(StatusColor));

        LoadingPanel.Stroke = SurfaceStrokeColor;
        LoadingPanel.BackgroundColor = SurfaceColor;
    }

    private void SetAccentColor(Color color)
    {
        if (AccentColor.Equals(color)) return;

        AccentColor = color;

        OnPropertyChanged(nameof(AccentColor));
        ConfigureSurfaceColors();
    }

    private static Color? TryExtractAccentColor(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);

            return FindLottieColor(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Color? FindLottieColor(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("c", out JsonElement color)
                && color.ValueKind == JsonValueKind.Object
                && color.TryGetProperty("k", out JsonElement keyframes)
                && TryReadColor(keyframes, out Color result)) return result;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                Color? found = FindLottieColor(property.Value);

                if (found is not null) return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                Color? found = FindLottieColor(child);

                if (found is not null) return found;
            }
        }

        return null;
    }

    private static bool TryReadColor(JsonElement value, out Color color)
    {
        color = Colors.Transparent;

        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() < 3) return false;
        if (value[0].ValueKind == JsonValueKind.Array) return TryReadColor(value[0], out color);
        if (value[0].ValueKind != JsonValueKind.Number || value[1].ValueKind != JsonValueKind.Number || value[2].ValueKind != JsonValueKind.Number) return false;

        double red = value[0].GetDouble();
        double green = value[1].GetDouble();
        double blue = value[2].GetDouble();

        if (red is < 0 or > 1 || green is < 0 or > 1 || blue is < 0 or > 1) return false;

        double alpha = value.GetArrayLength() > 3 && value[3].ValueKind == JsonValueKind.Number ? Math.Clamp(value[3].GetDouble(), 0d, 1d) : 1d;
        color = new Color((float)red, (float)green, (float)blue, (float)alpha);

        return true;
    }

    private void UpdateAnimationAspectRatio(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("w", out JsonElement width)
                && document.RootElement.TryGetProperty("h", out JsonElement height)
                && width.TryGetDouble(out double w)
                && height.TryGetDouble(out double h)
                && w > 0d && h > 0d)
            {
                animationAspectRatio = w / h;

                ApplyAnimationAspectRatio();
            }
        }
        catch (JsonException)
        {
            animationAspectRatio = 1d;
        }
    }

    private void ApplyAnimationAspectRatio()
    {
        if (Width <= 0 || Height <= 0 || animationAspectRatio <= 0) return;

        double maxWidth = Math.Max(80d, Math.Min(Width - 40d, 320d));
        double maxHeight = Math.Max(80d, Math.Min(Height - 100d, 300d));
        double fittedWidth = Math.Min(maxWidth, maxHeight * animationAspectRatio);
        double fittedHeight = fittedWidth / animationAspectRatio;

        FitAnimation(Animation, fittedWidth, fittedHeight);
    }

    private void LoadingViewOnSizeChanged(object? sender, EventArgs e)
    {
        if (activeExternalView is not null) FitAnimation(activeExternalView);
        else ApplyAnimationAspectRatio();
    }

    private void FitAnimation(LottieView view)
    {
        if (Width <= 0 || Height <= 0) return;

        double maxWidth = Math.Max(80d, Math.Min(Width - 40d, 320d));
        double maxHeight = Math.Max(80d, Math.Min(Height - 100d, 300d));
        double fittedWidth = Math.Min(maxWidth, maxHeight * animationAspectRatio);

        FitAnimation(view, fittedWidth, fittedWidth / animationAspectRatio);
    }

    private static void FitAnimation(LottieView view, double width, double height)
    {
        view.WidthRequest = width;
        view.HeightRequest = height;
    }

    private Action? CaptureAndDetachParent(LottieView view)
    {
        switch (view.Parent)
        {
            case ContentView contentView when ReferenceEquals(contentView.Content, view):
                contentView.Content = null;

                return () =>
                {
                    if (contentView.Content is null) contentView.Content = view;
                };
            case Border border when ReferenceEquals(border.Content, view):
                border.Content = null;

                return () =>
                {
                    if (border.Content is null) border.Content = view;
                };
            case ScrollView scrollView when ReferenceEquals(scrollView.Content, view):
                scrollView.Content = null;

                return () =>
                {
                    if (scrollView.Content is null) scrollView.Content = view;
                };
            case Layout layout:
                int index = layout.Children.IndexOf(view);
                layout.Children.Remove(view);

                return () =>
                {
                    if (view.Parent is null) layout.Children.Insert(Math.Clamp(index, 0, layout.Children.Count), view);
                };
            default:
                return null;
        }
    }

    private void RestoreExternalAnimationView()
    {
        if (activeExternalView is null) return;

        AnimationHost.Children.Remove(activeExternalView);
        activeExternalView.IsPlaying = externalPreviousIsPlaying;
        activeExternalView.IsLooping = externalPreviousIsLooping;
        activeExternalView.WidthRequest = externalPreviousWidth;
        activeExternalView.HeightRequest = externalPreviousHeight;

        Action? restore = restoreAnimationView;
        restoreAnimationView = null;

        restore?.Invoke();

        activeExternalView = null;

        if (!AnimationHost.Children.Contains(Animation)) AnimationHost.Children.Insert(0, Animation);
    }

    private void DisableHostedVisualTree()
    {
        if (originalContent is null) return;

        CaptureAndDisable(originalContent);
    }

    private void CaptureAndDisable(VisualElement element)
    {
        if (!originalEnabledStates.ContainsKey(element)) originalEnabledStates[element] = element.IsEnabled;
        if (element is IVisualTreeElement visualTree)
        {
            foreach (IVisualTreeElement child in visualTree.GetVisualChildren())
            {
                if (child is VisualElement visualChild) CaptureAndDisable(visualChild);
            }
        }

        element.IsEnabled = false;
    }

    private void RestoreHostedVisualTree()
    {
        foreach ((VisualElement element, bool wasEnabled) in originalEnabledStates) element.IsEnabled = wasEnabled;

        originalEnabledStates.Clear();
    }

    private EventHandler? pageDisappearingHandler;
    private EventHandler? controlUnloadedHandler;
    private EventHandler? windowDestroyingHandler;

    private void AnimationOnLoaded(object? sender, EventArgs e) { }

    /// <summary>
    /// Restores the original host content and requests cancellation of any running action set.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed) return;

        isDisposed = true;

        Cancel();
        RestoreExternalAnimationView();
        RestoreHostedVisualTree();

        Action? restore = restoreHost;

        restoreHost = null;

        if (restore is not null) restore();
        else HostContent = null;

        SizeChanged -= LoadingViewOnSizeChanged;
        Animation.AnimationLoaded -= AnimationOnLoaded;
    }
}
