using LottieAgent.Interfaces;
using LottieAgent.Models;
using MessageAgent.Services;
using MessageAgent.Services.Actions;
using MessageAgent.Services.Extensions;
using MessageAgent.Resources.ExtensionResources;
using MessageAgent.Services.Interactions;
using MessageAgent.Services.Models;
using System.Diagnostics;

namespace MessageAgent.Controls.Provider.StatusMessage;

/// <summary>
/// Compact action badge used for the status overflow and status-grid paging actions.
/// </summary>
public partial class StatusActionButton : ContentView
{
    private static readonly TimeSpan LongPressDuration = TimeSpan.FromMilliseconds(650);
    private readonly Color actionColor = Color.FromHsla(Random.Shared.NextDouble(), 0.72, 0.45);
    private StatusActionPalette actionPalette;
    private MessageAgentLottieSettings animationSettings = new();
    private readonly LottiePlaybackInteraction animationInteraction;
    private readonly NativeLongPressGesture longPressGesture;

    private bool ignoreNextTap;
    private CancellationTokenSource? pendingTap;
    private DateTime lastTapUtc = DateTime.MinValue;
    private DateTime lastLongPressUtc = DateTime.MinValue;

    /// <summary>
    /// Raised after a single tap on the action badge.
    /// </summary>
    public event EventHandler? Clicked;
    /// <summary>
    /// Raised after a double tap or a native long press.
    /// </summary>
    public event EventHandler? DoubleOrLongPressed;

    /// <summary>
    /// Bindable property for <see cref="Text"/>.
    /// </summary>
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(StatusActionButton), "...", propertyChanged: OnTextChanged);

    /// <summary>
    /// Bindable property for <see cref="ToolTipText"/>.
    /// </summary>
    public static readonly BindableProperty ToolTipTextProperty =
        BindableProperty.Create(nameof(ToolTipText), typeof(string), typeof(StatusActionButton), default(string), propertyChanged: OnToolTipTextChanged);

    /// <summary>
    /// Bindable property for <see cref="AnimationSource"/>.
    /// </summary>
    public static readonly BindableProperty AnimationSourceProperty =
        BindableProperty.Create(nameof(AnimationSource), typeof(string), typeof(StatusActionButton), default(string), propertyChanged: OnAnimationSourceChanged);

    /// <summary>
    /// Gets or sets the text displayed when the action does not use a Lottie asset.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Tooltip shown by desktop platforms when the pointer rests over the action.
    /// </summary>
    public string? ToolTipText
    {
        get => (string?)GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    /// <summary>
    /// Optional Lottie asset used instead of the action text.
    /// </summary>
    public string? AnimationSource
    {
        get => (string?)GetValue(AnimationSourceProperty);
        set => SetValue(AnimationSourceProperty, value);
    }

    /// <summary>
    /// Gets whether the action is currently represented by a Lottie animation.
    /// </summary>
    public bool IsAnimationVisible => string.Equals(Text, "...", StringComparison.Ordinal) || !string.IsNullOrWhiteSpace(AnimationSource);

    /// <summary>
    /// Gets whether the action is currently represented by its text label.
    /// </summary>
    public bool IsTextVisible => !IsAnimationVisible;

    /// <summary>
    /// Gets the effective Lottie source used when the action is animated.
    /// </summary>
    public string EffectiveAnimationSource => animationSettings.Source ?? AnimationSource ?? "Lotties/more_point.json";

    /// <summary>
    /// Gets the modifiers associated with the current action animation.
    /// </summary>
    public LottieModifierCollection AnimationModifiers { get; private set; } = new();

    /// <summary>
    /// Initializes the action badge and its native interaction handlers.
    /// </summary>
    public StatusActionButton()
    {
        InitializeComponent();

        ActionAnimation.JsonLoader = MauiServiceProvider.GetRequiredService<ILottieJsonLoader>();
        ActionAnimation.ModifierProcessor = MauiServiceProvider.GetRequiredService<ILottieModifierProcessor>();
        ActionAnimation.AnimationLoaded += AnimationOnLoaded;
        ActionAnimation.AnimationFailed += exception => Debug.WriteLine(exception);
        ActionAnimation.ModifierNotApplied += target => Debug.WriteLine($"Lottie modifier target not found: {target}");

        animationInteraction = new LottiePlaybackInteraction(ActionAnimation, resetToRestingOnDeactivate: true);
        longPressGesture = new NativeLongPressGesture(ActionBorder, OnNativeLongPress, LongPressDuration, cancelsTouchesInView: false);

        ActionBorder.HandlerChanged += (_, _) =>
        {
            ApplyToolTip();
            ApplyAnimationSurface();
        };
        ActionAnimation.HandlerChanged += (_, _) => ApplyAnimationSurface();

        ApplyStyle();
    }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue) => ((StatusActionButton)bindable).ApplyText();

    private static void OnToolTipTextChanged(BindableObject bindable, object oldValue, object newValue) => ((StatusActionButton)bindable).ApplyToolTip();

    private static void OnAnimationSourceChanged(BindableObject bindable, object oldValue, object newValue) => ((StatusActionButton)bindable).ApplyText();

    private void ApplyText()
    {
        bool isMoreAction = string.Equals(Text, "...", StringComparison.Ordinal);
        bool useAnimation = isMoreAction || !string.IsNullOrWhiteSpace(AnimationSource);

        bool isMinimizeAction = AnimationSource?.EndsWith("minimize.json", StringComparison.OrdinalIgnoreCase) == true;
        bool isMoreAnimation = string.IsNullOrWhiteSpace(AnimationSource)
            ? isMoreAction
            : AnimationSource.EndsWith("more_point.json", StringComparison.OrdinalIgnoreCase);

        if (isMoreAnimation)
        {
            animationSettings = MessageAgentLotties.GetSettings(
                MessageAgentLottieKeys.More,
                AnimationSource,
                defaultRestingProgress: 0d,
                defaultModifiers: CreateMoreAnimationModifiers());
        }
        else if (isMinimizeAction)
        {
            animationSettings = MessageAgentLotties.GetSettings(
                MessageAgentLottieKeys.Minimize,
                AnimationSource,
                defaultRestingProgress: 0d,
                defaultModifiers: CreateMinimizeAnimationModifiers());
        }
        else
        {
            animationSettings = new MessageAgentLottieSettings
            {
                Source = AnimationSource,
                RestingProgress = 0d,
                Speed = 1d,
                IsReversed = false,
                Modifiers = new LottieModifierCollection()
            };
        }

        AnimationModifiers = animationSettings.Modifiers ?? new LottieModifierCollection();
        ActionAnimation.Speed = animationSettings.Speed ?? 1d;
        ActionAnimation.IsReversed = animationSettings.IsReversed ?? false;
        animationInteraction.RestingProgress = animationSettings.RestingProgress ?? 0d;

        OnPropertyChanged(nameof(IsAnimationVisible));
        OnPropertyChanged(nameof(IsTextVisible));
        OnPropertyChanged(nameof(EffectiveAnimationSource));
        OnPropertyChanged(nameof(AnimationModifiers));

        if (useAnimation)
        {
            ActionBorder.Padding = new Thickness(4, 2);
            ActionBorder.WidthRequest = 34;
        }
        else
        {
            ActionBorder.Padding = new Thickness(6, 2);
            ActionBorder.WidthRequest = Math.Max(66, 20 + Text.Length * 5.7);
        }
    }

    private LottieModifierCollection CreateMoreAnimationModifiers() => new LottieModifierCollection()
        .SetColor("Layer 1", actionPalette.FirstCircleColor)
        .SetColor("Layer 2", actionPalette.SecondCircleColor)
        .SetColor("Layer 3", actionPalette.ThirdCircleColor);

    private LottieModifierCollection CreateMinimizeAnimationModifiers()
    {
        Color borderColor = actionPalette.Surface.BorderColor;

        return new LottieModifierCollection()
            .SetColor("Layer 2 Outlines", borderColor)
            .SetColor("Layer 3 Outlines", borderColor)
            .SetColor("Layer 4 Outlines", borderColor)
            .SetColor("Layer 5 Outlines", borderColor);
    }

    private void ApplyStyle()
    {
        actionPalette = StatusActionPaletteFactory.Create(actionColor);
        StatusVisualPalette surface = actionPalette.Surface;

        ActionBorder.Stroke = new SolidColorBrush(surface.BorderColor);
        ActionBorder.BackgroundColor = surface.BackgroundColor;
        ActionLabel.TextColor = surface.TextColor;

        ApplyAnimationSurface();
        ApplyText();
        ApplyToolTip();
    }

    private void ApplyAnimationSurface()
    {
        Color surface = ActionBorder.BackgroundColor ?? Colors.Transparent;

        AnimationHost.BackgroundColor = surface;
        ActionAnimation.BackgroundColor = surface;

        ActionAnimation.InvalidateSurface();
    }

    private void ApplyToolTip()
    {
        ToolTipHelper.SetText(this, ToolTipText);
        ToolTipHelper.SetText(ActionBorder, ToolTipText);
    }

    private void AnimationOnLoaded(object? sender, EventArgs e)
    {
        ApplyAnimationSurface();

        ActionAnimation.IsPlaying = false;
        ActionAnimation.Progress = animationInteraction.RestingProgress;
        ActionAnimation.IsLooping = animationInteraction.IsActive;

        if (animationInteraction.IsActive) ActionAnimation.Play();
    }

    private void OnNativeLongPress()
    {
        ignoreNextTap = true;
        lastLongPressUtc = DateTime.UtcNow;
        DoubleOrLongPressed?.Invoke(this, EventArgs.Empty);
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (ignoreNextTap)
        {
            bool suppressReleaseTap = DateTime.UtcNow - lastLongPressUtc <= TimeSpan.FromSeconds(1);
            ignoreNextTap = false;

            if (suppressReleaseTap) return;
        }

        DateTime now = DateTime.UtcNow;

        if (pendingTap is not null && now - lastTapUtc <= TimeSpan.FromMilliseconds(350))
        {
            CancellationTokenSource previousTap = pendingTap;
            pendingTap = null;
            lastTapUtc = DateTime.MinValue;

            previousTap.Cancel();
            previousTap.Dispose();
            DoubleOrLongPressed?.Invoke(this, EventArgs.Empty);

            return;
        }

        pendingTap?.Cancel();
        pendingTap?.Dispose();

        CancellationTokenSource tap = new ();
        pendingTap = tap;
        lastTapUtc = now;

        try
        {
            await Task.Delay(350, tap.Token);

            if (!tap.IsCancellationRequested && ReferenceEquals(pendingTap, tap))
            {
                pendingTap = null;
                lastTapUtc = DateTime.MinValue;
                Clicked?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            // A second tap or a long press superseded the pending single tap.
        }
        finally
        {
            tap.Dispose();
        }
    }

    /// <inheritdoc />
    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent is null)
        {
            animationInteraction.SetActive(false);
            ActionAnimation.IsPlaying = false;
            ActionAnimation.Progress = animationInteraction.RestingProgress;

            pendingTap?.Cancel();
            pendingTap?.Dispose();
            pendingTap = null;
            lastTapUtc = DateTime.MinValue;
        }
    }
}
