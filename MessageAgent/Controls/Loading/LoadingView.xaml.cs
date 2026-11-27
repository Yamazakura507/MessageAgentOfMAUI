using System.Collections.ObjectModel;
using System.Diagnostics;
using LottieAgent.Controls;
using LottieAgent.Extensions;
using LottieAgent.Services;
using MessageAgent.Core.Enums.Loading;
using MessageAgent.Core.Models.Loading;
using MessageAgent.Resources.ExtensionResources;
using MessageAgent.Services.Extensions;

namespace MessageAgent.Controls.Loading;

/// <summary>
/// Displays a blocking Lottie overlay while executing cancellable actions.
/// The hosted content is restored to its original enabled state after every outcome.
/// </summary>
public partial class LoadingView : ContentView, IDisposable
{
    private readonly Stopwatch stopwatch = new();
    private readonly Dictionary<VisualElement, bool> originalEnabledStates = new(ReferenceEqualityComparer.Instance);
    private readonly ObservableCollection<LoadingActionState> actionStates = new();
    private readonly ObservableCollection<LoadingActionState> sortedActionStates = new();
    private readonly ObservableCollection<LoadingActionState> visibleActionStates = new();
    private readonly Dictionary<LoadingActionState, int> sortProgressBuckets = new();
    private CancellationTokenSource? executionSource;
    private bool isActionListRefreshQueued;
    private TimeSpan lastExecutionTime;
    private bool showBackdrop = true;
    private bool showSurface = true;
    private bool showStatus = true;
    private bool isShowProgress;
    private int maxVisibleTasks = 2;
    private LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process;
    private bool isActionPopupVisible;
    private bool trackDominantColor;
    private string currentActionDescription = string.Empty;
    private string? externalPreviousToolTip;
    private long lastDynamicAccentTimestamp;
    private bool isRunning;
    private bool isDisposed;
    private View? originalContent;
    private Button? hostedButton;
    private string? originalButtonText;
    private Action? restoreHost;
    private Action? restoreAnimationView;
    private LottieView? activeExternalView;
    private bool externalPreviousIsPlaying;
    private bool externalPreviousIsLooping;
    private double externalPreviousWidth;
    private double externalPreviousHeight;
    private double externalPreviousScale = 1d;
    private LayoutOptions externalPreviousHorizontalOptions;
    private LayoutOptions externalPreviousVerticalOptions;
    private Thickness externalPreviousMargin;
    private double animationAspectRatio = 1d;
    private LoadingStatus status = LoadingStatus.Pending;

    /// <summary>
    /// Gets or sets the content displayed beneath the loading overlay.
    /// </summary>
    public View? HostContent
    {
        get => (View?)GetValue(HostContentProperty);
        set => SetValue(HostContentProperty, value);
    }

    /// <summary>
    /// Gets the duration of the current operation at the moment of access, or the last duration after completion.
    /// </summary>
    public TimeSpan LastExecutionTime => stopwatch.IsRunning ? stopwatch.Elapsed : lastExecutionTime;

    /// <summary>
    /// Gets the observable rows displayed for the current action or parallel action set.
    /// </summary>
    public ObservableCollection<LoadingActionState> ActionStates => actionStates;

    /// <summary>
    /// Gets the complete, sorted action list used by the expanded task context.
    /// </summary>
    public ObservableCollection<LoadingActionState> SortedActionStates => sortedActionStates;

    /// <summary>
    /// Gets the limited, sorted action list displayed beside the animation.
    /// </summary>
    public ObservableCollection<LoadingActionState> VisibleActionStates => visibleActionStates;

    /// <summary>
    /// Gets whether the full action list contains more items than the visible limit.
    /// </summary>
    public bool HasOverflowActions => sortedActionStates.Count > MaxVisibleTasks;

    /// <summary>
    /// Gets the maximum height reserved for the visible task list.
    /// </summary>
    public double VisibleListHeight => Math.Max(44d, MaxVisibleTasks * 48d);

    /// <summary>
    /// Gets or sets the maximum number of task descriptions displayed beside the animation.
    /// The value is clamped to at least one. The default is two.
    /// </summary>
    public int MaxVisibleTasks
    {
        get => maxVisibleTasks;
        set
        {
            int normalized = Math.Max(1, value);
            if (maxVisibleTasks == normalized) return;
            maxVisibleTasks = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasOverflowActions));
            OnPropertyChanged(nameof(VisibleListHeight));
            RefreshActionLists();
        }
    }

    /// <summary>
    /// Gets or sets the sort order shared by the visible and expanded task lists.
    /// </summary>
    public LoadingProgressSortMode SortProgress
    {
        get => sortProgress;
        set
        {
            if (sortProgress == value) return;
            sortProgress = value;
            OnPropertyChanged();
            RefreshActionLists();
        }
    }

    /// <summary>
    /// Gets or sets whether the expanded task context is open.
    /// </summary>
    public bool IsActionPopupVisible
    {
        get => isActionPopupVisible;
        private set
        {
            if (isActionPopupVisible == value) return;
            isActionPopupVisible = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets whether each action shows its progress percentage and progress bar.
    /// Actions with no progress reporter move from 0% to 100% when they finish.
    /// </summary>
    public bool IsShowProgress
    {
        get => isShowProgress;
        set
        {
            if (isShowProgress == value) return;
            isShowProgress = value;
            OnPropertyChanged();
        }
    }

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
    public Color AccentColor { get; private set; } = MessageAgentResources.GetColor(MessageAgentColorKeys.LoadingAccent);

    /// <summary>
    /// Gets or sets whether the accent follows the dominant color of the current Lottie frame.
    /// </summary>
    public bool TrackDominantColor
    {
        get => trackDominantColor;
        set
        {
            trackDominantColor = value;

            if (value) UpdateDominantAccent();
        }
    }

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
            OnPropertyChanged(nameof(PanelHorizontalOptions));
            OnPropertyChanged(nameof(PanelVerticalOptions));
        }
    }

    /// <summary>
    /// Gets or sets whether action descriptions and their progress are displayed below the animation.
    /// </summary>
    public bool ShowStatus
    {
        get => showStatus;
        set
        {
            if (showStatus == value) return;

            showStatus = value;

            OnPropertyChanged(nameof(IsStatusVisible));
            OnPropertyChanged(nameof(AnimationRowSpacing));
        }
    }

    /// <summary>
    /// Gets the backdrop color used by XAML.
    /// </summary>
    public Color OverlayColor => ShowBackdrop ? MessageAgentResources.GetColor(MessageAgentColorKeys.LoadingOverlay) : Colors.Transparent;

    /// <summary>
    /// Gets the outer overlay padding.
    /// </summary>
    public Thickness OverlayPadding => new Thickness(ShowBackdrop || ShowSurface ? 18 : 0);

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
    public Thickness SurfacePadding => new Thickness(ShowSurface ? 16 : 0);

    /// <summary>
    /// Gets the surface stroke thickness used by XAML.
    /// </summary>
    public double SurfaceStrokeThickness => ShowSurface ? 1.5d : 0d;

    /// <summary>
    /// Gets the horizontal alignment used by the loading panel.
    /// </summary>
    public LayoutOptions PanelHorizontalOptions => ShowSurface ? LayoutOptions.Center : LayoutOptions.Fill;

    /// <summary>
    /// Gets the vertical alignment used by the loading panel.
    /// </summary>
    public LayoutOptions PanelVerticalOptions => ShowSurface ? LayoutOptions.Center : LayoutOptions.Fill;

    /// <summary>
    /// Gets whether the status caption is visible.
    /// </summary>
    public bool IsStatusVisible => ShowStatus;

    /// <summary>
    /// Gets the spacing reserved between the animation and its caption.
    /// </summary>
    public double AnimationRowSpacing => ShowStatus ? 8d : 0d;

    /// <summary>
    /// Gets the foreground color used by the status caption, adapted to the active theme.
    /// </summary>
    public Color StatusColor => MessageColorPalette.GetThemeColor(
        MessageAgentResources.DialogSecondaryForegroundLightKey,
        MessageAgentResources.DialogSecondaryForegroundDarkKey);

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
    /// Initializes the loading overlay.
    /// </summary>
    public LoadingView()
    {
        InitializeComponent();

        Animation.PropertyChanged += AnimationOnPropertyChanged;
        Animation.AnimationLoaded += AnimationOnLoaded;
        SizeChanged += LoadingViewOnSizeChanged;
        AnimationHost.SizeChanged += AnimationHostOnSizeChanged;
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

        if (page is not ContentPage contentPage) throw new NotSupportedException("LoadingView.AttachTo(Page) requires a ContentPage.");

        return AttachCore(
            getContent: () => contentPage.Content,
            setContent: v => contentPage.Content = v,
            subscribeCancel: cancel =>
            {
                EventHandler handler = (_, _) => cancel();
                contentPage.Disappearing += handler;

                return () => contentPage.Disappearing -= handler;
            });
    }

    /// <summary>
    /// Attaches a blocking overlay above a ContentView and disables its original content while running.
    /// </summary>
    /// <param name="control">The control whose content should be covered.</param>
    /// <returns>The attached loading view.</returns>
    public static LoadingView AttachTo(ContentView control)
    {
        ArgumentNullException.ThrowIfNull(control);

        return AttachCore(
            getContent: () => control.Content,
            setContent: v => control.Content = v,
            subscribeCancel: cancel =>
            {
                EventHandler handler = (_, _) => cancel();
                control.Unloaded += handler;

                return () => control.Unloaded -= handler;
            });
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
            Shell { CurrentPage: ContentPage currentPage } => currentPage,
            _ => throw new NotSupportedException("The window must host a ContentPage or a Shell with an active ContentPage.")
        };

        LoadingView loading = AttachTo(page);

        EventHandler handler = (_, _) => loading.Cancel();
        window.Destroying += handler;
        loading.restoreHost += () => window.Destroying -= handler;

        return loading;
    }

    /// <summary>
    /// Executes an action set using a random supplied animation or the built-in default.
    /// </summary>
    /// <param name="actions">The actions to execute.</param>
    /// <param name="animations">Optional animations; one is selected randomly for each execution.</param>
    /// <param name="executionMode">Parallel by default, or sequential.</param>
    /// <param name="cancellationToken">An external cancellation token.</param>
    /// <param name="maxVisibleTasks">The maximum number of action rows displayed before the overflow list is used. Defaults to two.</param>
    /// <param name="sortProgress">The sort order shared by the visible and expanded action lists. Defaults to Process.</param>
    /// <returns>The operation status, measured duration, and any exception.</returns>
    public Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
    {
        MaxVisibleTasks = maxVisibleTasks;
        SortProgress = sortProgress;

        return RunAsync(actions, animations, executionMode, cancellationToken);
    }

    /// <summary>
    /// Executes an action set and optionally displays per-action progress.
    /// </summary>
    /// <param name="actions">The actions to execute.</param>
    /// <param name="isShowProgress">Whether each action displays its progress.</param>
    /// <param name="animations">Optional animations; one is selected randomly for each execution.</param>
    /// <param name="executionMode">Parallel by default, or sequential.</param>
    /// <param name="cancellationToken">An external cancellation token.</param>
    /// <param name="maxVisibleTasks">The maximum number of action rows displayed before the overflow list is used. Defaults to two.</param>
    /// <param name="sortProgress">The sort order shared by the visible and expanded action lists. Defaults to Process.</param>
    /// <returns>The operation status, measured duration, and any exception.</returns>
    public Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        bool isShowProgress,
        IEnumerable<LoadingAnimation>? animations = null,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
    {
        IsShowProgress = isShowProgress;
        MaxVisibleTasks = maxVisibleTasks;
        SortProgress = sortProgress;

        return RunAsync(actions, animations, executionMode, cancellationToken);
    }

    /// <summary>
    /// Executes an action set using one specified animation.
    /// </summary>
    /// <param name="actions">The actions to execute.</param>
    /// <param name="animation">The animation to display.</param>
    /// <param name="executionMode">Parallel by default, or sequential.</param>
    /// <param name="cancellationToken">An external cancellation token.</param>
    /// <param name="maxVisibleTasks">The maximum number of action rows displayed before the overflow list is used. Defaults to two.</param>
    /// <param name="sortProgress">The sort order shared by the visible and expanded action lists. Defaults to Process.</param>
    /// <returns>The operation status, measured duration, and any exception.</returns>
    public Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        LoadingAnimation animation,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
    {
        ArgumentNullException.ThrowIfNull(animation);

        MaxVisibleTasks = maxVisibleTasks;
        SortProgress = sortProgress;

        return RunAsync(actions, new[] { animation }, executionMode, cancellationToken);
    }

    /// <summary>
    /// Executes an action set with one specified animation and optional per-action progress.
    /// </summary>
    /// <param name="actions">The actions to execute.</param>
    /// <param name="animation">The animation to display.</param>
    /// <param name="isShowProgress">Whether each action displays its progress.</param>
    /// <param name="executionMode">Parallel by default, or sequential.</param>
    /// <param name="cancellationToken">An external cancellation token.</param>
    /// <param name="maxVisibleTasks">The maximum number of action rows displayed before the overflow list is used. Defaults to two.</param>
    /// <param name="sortProgress">The sort order shared by the visible and expanded action lists. Defaults to Process.</param>
    /// <returns>The operation status, measured duration, and any exception.</returns>
    public Task<LoadingResult> ExecuteAsync(
        IEnumerable<LoadingAction> actions,
        LoadingAnimation animation,
        bool isShowProgress,
        LoadingExecutionMode executionMode = LoadingExecutionMode.Parallel,
        CancellationToken cancellationToken = default,
        int maxVisibleTasks = 2,
        LoadingProgressSortMode sortProgress = LoadingProgressSortMode.Process)
    {
        ArgumentNullException.ThrowIfNull(animation);

        IsShowProgress = isShowProgress;
        MaxVisibleTasks = maxVisibleTasks;
        SortProgress = sortProgress;

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

        LoadingActionState[] states = work
            .Select((action, index) => new LoadingActionState(action.Description, IsShowProgress, index))
            .ToArray();

        LoadingAnimation[] choices = animations?.ToArray() ?? Array.Empty<LoadingAnimation>();
        LoadingAnimation selected = choices.Length == 0 ? CreateDefaultAnimation() : choices[Random.Shared.Next(choices.Length)];

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
                IsActionPopupVisible = false;

                foreach (LoadingActionState previousState in ActionStates)
                    previousState.PropertyChanged -= ActionStateOnPropertyChanged;

                ActionStates.Clear();
                sortedActionStates.Clear();
                visibleActionStates.Clear();
                sortProgressBuckets.Clear();
                isActionListRefreshQueued = false;

                for (int index = 0; index < states.Length; index++)
                {
                    LoadingActionState state = states[index];
                    sortProgressBuckets[state] = 0;
                    state.PropertyChanged += ActionStateOnPropertyChanged;

                    if (executionMode == LoadingExecutionMode.Parallel || index == 0)
                        state.MarkRunning();

                    ActionStates.Add(state);
                }

                currentActionDescription = executionMode == LoadingExecutionMode.Parallel
                    ? string.Join(" · ", states.Select(state => state.Description))
                    : states[0].Description;

                RefreshActionLists();
                HideHostedButtonText();
                DisableHostedVisualTree();
                ToolTipHelper.SetText(Animation, currentActionDescription);
                OverlayLayer.BackgroundColor = OverlayColor;
                LoadingPanel.BackgroundColor = SurfaceColor;
                LoadingPanel.Stroke = SurfaceStrokeColor;
                LoadingPanel.StrokeThickness = SurfaceStrokeThickness;
                LoadingPanel.Padding = SurfacePadding;
            });

            linked.Token.ThrowIfCancellationRequested();

            await ConfigureAnimationAsync(selected, linked.Token);

            if (executionMode == LoadingExecutionMode.Sequential)
            {
                failure = await ExecuteSequentialAsync(work, states, linked.Token);
            }
            else
            {
                Exception[] errors = await ExecuteParallelAsync(work, states, linked.Token);
                if (errors.Length == 1) failure = errors[0];
                else if (errors.Length > 1) failure = new AggregateException(errors);

                await Task.Delay(350);
            }

            cancelled = linked.IsCancellationRequested;

            Status = failure is not null
                ? LoadingStatus.Failed
                : cancelled 
                    ? LoadingStatus.Cancelled 
                    : LoadingStatus.Completed;
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            cancelled = true;
            Status = LoadingStatus.Cancelled;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (LoadingActionState state in states)
                {
                    if (state.Status is LoadingActionStatus.Pending or LoadingActionStatus.Running) state.MarkCancelled();
                }
            });
        }
        catch (Exception exception)
        {
            failure = exception;
            Status = LoadingStatus.Failed;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (LoadingActionState state in states)
                {
                    if (state.Status is LoadingActionStatus.Pending or LoadingActionStatus.Running)
                        state.MarkFailed(exception);
                }

                if (executionMode == LoadingExecutionMode.Sequential && ActionStates.Count == 0 && states.Length > 0) ActionStates.Add(states[0]);
            });
        }
        finally
        {
            stopwatch.Stop();
            lastExecutionTime = stopwatch.Elapsed;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RestoreExternalAnimationView();

                Animation.Stop();

                ToolTipHelper.SetText(Animation, null);

                RestoreHostedVisualTree();
                RestoreHostedButtonText();

                IsActionPopupVisible = false;

                IsRunning = false;

                OnPropertyChanged(nameof(LastExecutionTime));
            });

            if (ReferenceEquals(executionSource, linked)) executionSource = null;
        }

        return new LoadingResult(Status, lastExecutionTime, failure);
    }

    private void ShowAllTasksOnClicked(object? sender, EventArgs e) => IsActionPopupVisible = true;

    private void CloseAllTasksOnClicked(object? sender, EventArgs e) => IsActionPopupVisible = false;

    private void ActionStateOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not LoadingActionState state) return;
        if (e.PropertyName == nameof(LoadingActionState.Progress))
        {
            if (SortProgress != LoadingProgressSortMode.Process) return;

            int bucket = (int)Math.Floor(state.Progress * 20d);

            if (sortProgressBuckets.TryGetValue(state, out int previousBucket) && previousBucket == bucket) return;

            sortProgressBuckets[state] = bucket;

            QueueActionListRefresh();

            return;
        }

        if (e.PropertyName is nameof(LoadingActionState.Status) or nameof(LoadingActionState.FinishedAt)) QueueActionListRefresh();
    }

    private void QueueActionListRefresh()
    {
        if (isActionListRefreshQueued) return;

        isActionListRefreshQueued = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            isActionListRefreshQueued = false;

            RefreshActionLists();
        });
    }

    private void RefreshActionLists()
    {
        IEnumerable<LoadingActionState> ordered = SortProgress switch
        {
            LoadingProgressSortMode.Access => ActionStates
                .OrderBy(state => state.Status switch
                {
                    LoadingActionStatus.Completed => 0,
                    LoadingActionStatus.Failed => 1,
                    _ => 2
                })
                .ThenBy(state =>
                state.Status is LoadingActionStatus.Completed or LoadingActionStatus.Failed
                    ? state.FinishedAt ?? DateTimeOffset.MaxValue
                    : DateTimeOffset.MaxValue)
                .ThenByDescending(state => state.Progress)
                .ThenBy(state => ActionStatusPriority(state.Status))
                .ThenBy(state => state.Sequence),
            LoadingProgressSortMode.Error => ActionStates
                .OrderBy(state => state.Status switch
                {
                    LoadingActionStatus.Failed => 0,
                    LoadingActionStatus.Completed => 1,
                    _ => 2
                })
                .ThenBy(state => state.Status is LoadingActionStatus.Completed or LoadingActionStatus.Failed
                    ? state.FinishedAt ?? DateTimeOffset.MaxValue
                    : DateTimeOffset.MaxValue)
                .ThenByDescending(state => state.Progress)
                .ThenBy(state => ActionStatusPriority(state.Status))
                .ThenBy(state => state.Sequence),
            LoadingProgressSortMode.Process => ActionStates
                .OrderBy(state => state.Status == LoadingActionStatus.Completed ? 1 : 0)
                .ThenByDescending(state => state.Status == LoadingActionStatus.Completed ? 0d : state.Progress)
                .ThenBy(state => state.Status == LoadingActionStatus.Completed
                    ? state.FinishedAt ?? DateTimeOffset.MaxValue
                    : DateTimeOffset.MaxValue)
                .ThenBy(state => ActionStatusPriority(state.Status))
                .ThenBy(state => state.Sequence),
            _ => ActionStates.OrderBy(state => state.Sequence)
        };

        LoadingActionState[] all = ordered.ToArray();

        ReplaceActionCollection(sortedActionStates, all);
        ReplaceActionCollection(visibleActionStates, all.Take(MaxVisibleTasks));

        OnPropertyChanged(nameof(HasOverflowActions));
    }

    private static int ActionStatusPriority(LoadingActionStatus status) => status switch
    {
        LoadingActionStatus.Running => 0,
        LoadingActionStatus.Pending => 1,
        LoadingActionStatus.Failed => 2,
        LoadingActionStatus.Cancelled => 3,
        LoadingActionStatus.Completed => 4,
        _ => 5
    };

    private static void ReplaceActionCollection(ObservableCollection<LoadingActionState> target, IEnumerable<LoadingActionState> source)
    {
        LoadingActionState[] items = source.ToArray();

        for (int desiredIndex = 0; desiredIndex < items.Length; desiredIndex++)
        {
            LoadingActionState desired = items[desiredIndex];

            if (desiredIndex < target.Count && ReferenceEquals(target[desiredIndex], desired)) continue;

            int existingIndex = target.IndexOf(desired);

            if (existingIndex >= 0) target.Move(existingIndex, desiredIndex);
            else target.Insert(desiredIndex, desired);
        }

        while (target.Count > items.Length) target.RemoveAt(target.Count - 1);
    }

    private void HideHostedButtonText()
    {
        if (ShowStatus || originalContent is not Button button) return;

        hostedButton = button;
        originalButtonText = button.Text;
        button.Text = string.Empty;
    }

    private void RestoreHostedButtonText()
    {
        if (hostedButton is null) return;

        hostedButton.Text = originalButtonText;
        hostedButton = null;
        originalButtonText = null;
    }

    private static LoadingView AttachCore(Func<View?> getContent, Action<View?> setContent, Func<Action, Action> subscribeCancel)
    {
        View content = getContent() ?? throw new InvalidOperationException("The host must contain content before a loading overlay can be attached.");

        setContent(null);

        LoadingView loading = new()
        {
            HostContent = content
        };

        setContent(loading);

        Action unsubscribe = subscribeCancel(loading.Cancel);

        loading.restoreHost = () =>
        {
            loading.HostContent = null;

            if (ReferenceEquals(getContent(), loading)) setContent(content);

            unsubscribe();
        };

        return loading;
    }

    private async Task<Exception?> ExecuteSequentialAsync(
        LoadingAction[] actions,
        LoadingActionState[] states,
        CancellationToken cancellationToken)
    {
        for (int index = 0; index < actions.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LoadingAction action = actions[index];
            LoadingActionState state = states[index];

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                state.MarkRunning();

                currentActionDescription = state.Description;
                ToolTipHelper.SetText(Animation, currentActionDescription);
            });

            try
            {
                await action.ExecuteAsync(new LoadingActionProgress(state), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await MainThread.InvokeOnMainThreadAsync(state.MarkCompleted);
                await Task.Delay(240, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(state.MarkCancelled);
                throw;
            }
            catch (Exception exception)
            {
                await MainThread.InvokeOnMainThreadAsync(() => state.MarkFailed(exception));

                if (action.OnError is not null)
                {
                    try
                    {
                        await action.OnError(exception);
                    }
                    catch (Exception handlerException)
                    {
                        exception = new AggregateException(exception, handlerException);
                    }
                }

                await Task.Delay(400);

                return exception;
            }
        }

        return null;
    }

    private async Task<Exception[]> ExecuteParallelAsync(
        LoadingAction[] actions,
        LoadingActionState[] states,
        CancellationToken cancellationToken)
    {
        async Task<Exception?> RunOneAsync(LoadingAction action, LoadingActionState state)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action.ExecuteAsync(new LoadingActionProgress(state), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await MainThread.InvokeOnMainThreadAsync(state.MarkCompleted);

                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(state.MarkCancelled);

                return null;
            }
            catch (Exception exception)
            {
                await MainThread.InvokeOnMainThreadAsync(() => state.MarkFailed(exception));

                if (action.OnError is null) return exception;

                try
                {
                    await action.OnError(exception);
                }
                catch (Exception handlerException)
                {
                    return new AggregateException(exception, handlerException);
                }

                return exception;
            }
        }

        Exception?[] results = await Task.WhenAll(actions.Select((action, index) => RunOneAsync(action, states[index])));

        return results.Where(exception => exception is not null).Cast<Exception>().ToArray();
    } 

    private async Task ConfigureAnimationAsync(LoadingAnimation selected, CancellationToken cancellationToken)
    {
        if (selected.View is LottieView externalView)
        {
            Color externalAccent = selected.AccentColor ?? CreateDefaultAnimation().AccentColor!;

            string? externalJson = externalView.SourceJson;

            if (string.IsNullOrWhiteSpace(externalJson) && !string.IsNullOrWhiteSpace(externalView.Source))
            {
                try
                {
                    externalJson = await externalView.JsonLoader.LoadAsync(externalView.Source, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // Keep external views intact when their source cannot be inspected.
                }
            }
            if (!string.IsNullOrWhiteSpace(externalJson))
            {
                externalAccent = selected.AccentColor ?? externalJson.GetDominantColor(externalView.Progress) ?? externalAccent;

                animationAspectRatio = externalJson.GetCompositionAspectRatio();
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
                externalPreviousScale = externalView.Scale;
                externalPreviousHorizontalOptions = externalView.HorizontalOptions;
                externalPreviousVerticalOptions = externalView.VerticalOptions;
                externalPreviousMargin = externalView.Margin;
                externalPreviousToolTip = ToolTipProperties.GetText(externalView)?.ToString();
                restoreAnimationView = CaptureAndDetachParent(externalView);

                AnimationHost.Children.Insert(0, externalView);

                externalView.HorizontalOptions = LayoutOptions.Fill;
                externalView.VerticalOptions = LayoutOptions.Fill;
                externalView.IsLooping = true;
                externalView.IsPlaying = true;

                ToolTipHelper.SetText(externalView, currentActionDescription);

                externalView.PropertyChanged += AnimationOnPropertyChanged;

                SetAccentColor(externalAccent);
                FitAnimation(externalView);
            });

            return;
        }

        string json = selected.RawJson ?? await new LottieJsonLoader().LoadAsync(selected.Source!, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        Color accent = selected.AccentColor ?? TryExtractAccentColor(json) ?? CreateDefaultAnimation().AccentColor!;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RestoreExternalAnimationView();

            if (!AnimationHost.Children.Contains(Animation)) AnimationHost.Children.Insert(0, Animation);

            Animation.IsPlaying = false;
            Animation.Modifiers = selected.Modifiers;
            Animation.Source = null;
            Animation.Json = json;
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

    private static Color? TryExtractAccentColor(string json) => json.GetDominantColor();

    private void UpdateAnimationAspectRatio(string json)
    {
        animationAspectRatio = json.GetCompositionAspectRatio();

        ApplyAnimationAspectRatio();
    }

    private void ApplyAnimationAspectRatio()
    {
        if (animationAspectRatio <= 0) return;

        FitAnimation(Animation);
    }

    private void LoadingViewOnSizeChanged(object? sender, EventArgs e)
    {
        if (activeExternalView is not null) FitAnimation(activeExternalView);
        else ApplyAnimationAspectRatio();
    }

    private void AnimationHostOnSizeChanged(object? sender, EventArgs e)
    {
        if (activeExternalView is not null) FitAnimation(activeExternalView);
        else ApplyAnimationAspectRatio();
    }

    private void FitAnimation(LottieView view)
    {
        double availableWidth = AnimationHost.Width > 0 ? AnimationHost.Width : Width;
        double availableHeight = AnimationHost.Height > 0 ? AnimationHost.Height : Height;

        if (availableWidth <= 0 || availableHeight <= 0 || animationAspectRatio <= 0) return;

        double maxWidth;
        double maxHeight;

        bool isButtonHost = originalContent is Button && !ShowStatus;

        if (isButtonHost)
        {
            maxHeight = Math.Max(1d, Math.Min(availableHeight - 2d, 64d));
            maxWidth = Math.Max(1d, availableWidth - 8d);
        }
        else
        {
            maxWidth = Math.Max(1d, Math.Min(availableWidth - 4d, 320d));
            maxHeight = Math.Max(1d, Math.Min(availableHeight - 4d, 300d));
        }

        view.HorizontalOptions = LayoutOptions.Center;
        view.VerticalOptions = LayoutOptions.Center;
        view.Margin = Thickness.Zero;
        view.Scale = isButtonHost ? 1.5d : 1d;

        double fittedHeight = Math.Min(maxHeight, maxWidth / animationAspectRatio);
        double fittedWidth = fittedHeight * animationAspectRatio;

        FitAnimation(view, fittedWidth, fittedHeight);
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
            default: return null;
        }
    }

    private void RestoreExternalAnimationView()
    {
        if (activeExternalView is null) return;

        AnimationHost.Children.Remove(activeExternalView);
        activeExternalView.PropertyChanged -= AnimationOnPropertyChanged;

        ToolTipHelper.SetText(activeExternalView, externalPreviousToolTip);

        externalPreviousToolTip = null;
        activeExternalView.IsPlaying = externalPreviousIsPlaying;
        activeExternalView.IsLooping = externalPreviousIsLooping;
        activeExternalView.WidthRequest = externalPreviousWidth;
        activeExternalView.HeightRequest = externalPreviousHeight;
        activeExternalView.Scale = externalPreviousScale;
        activeExternalView.HorizontalOptions = externalPreviousHorizontalOptions;
        activeExternalView.VerticalOptions = externalPreviousVerticalOptions;
        activeExternalView.Margin = externalPreviousMargin;

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

    private void AnimationOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (TrackDominantColor && e.PropertyName == nameof(LottieView.Progress)) UpdateDominantAccent();
    }

    private void UpdateDominantAccent()
    {
        if (!TrackDominantColor) return;

        long now = Stopwatch.GetTimestamp();
        long previous = Interlocked.Read(ref lastDynamicAccentTimestamp);

        if (now - previous < Stopwatch.Frequency / 8) return;

        Interlocked.Exchange(ref lastDynamicAccentTimestamp, now);

        LottieView current = activeExternalView ?? Animation;

        if (current.SourceJson?.GetDominantColor(current.Progress) is Color dominant) SetAccentColor(dominant);
    }

    private void AnimationOnLoaded(object? sender, EventArgs e)
    {
        if (TrackDominantColor) UpdateDominantAccent();
    }

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
        RestoreHostedButtonText();
        IsActionPopupVisible = false;

        foreach (LoadingActionState state in ActionStates) state.PropertyChanged -= ActionStateOnPropertyChanged;

        Action? restore = restoreHost;

        restoreHost = null;

        if (restore is not null) restore();
        else HostContent = null;

        SizeChanged -= LoadingViewOnSizeChanged;
        AnimationHost.SizeChanged -= AnimationHostOnSizeChanged;
        Animation.PropertyChanged -= AnimationOnPropertyChanged;
        Animation.AnimationLoaded -= AnimationOnLoaded;
    }

    private static LoadingAnimation CreateDefaultAnimation() => new("Lotties/loading.json", accentColor: MessageAgentResources.GetColor(MessageAgentColorKeys.LoadingAccent));
}
