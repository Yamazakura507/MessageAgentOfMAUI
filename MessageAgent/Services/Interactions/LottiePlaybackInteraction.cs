using LottieAgent.Controls;

#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
#endif
#if ANDROID
using Android.Views;
#endif
#if IOS || MACCATALYST
using UIKit;
#endif

namespace MessageAgent.Services.Interactions;

/// <summary>
/// Shares the native hover/press interaction used by all interactive Lottie icons.
/// Turning interaction off disables looping but lets the current iteration complete.
/// </summary>
internal sealed class LottiePlaybackInteraction : IDisposable
{
    private readonly LottieView animation;
    private readonly VisualElement interactionTarget;
    private bool isActive;
    private bool isDisposed;

    #if WINDOWS
    private UIElement? nativeView;
    #endif
    #if ANDROID
    private Android.Views.View? nativeView;
    #endif
    #if IOS || MACCATALYST
    private UIView? nativeView;
    private UILongPressGestureRecognizer? pressRecognizer;
    private UIHoverGestureRecognizer? hoverRecognizer;
    #endif

    /// <summary>
    /// Gets a value indicating whether the interaction is currently active.
    /// </summary>
    public bool IsActive => isActive;

    /// <summary>
    /// Initializes the interaction controller and attaches native input handlers.
    /// </summary>
    /// <param name="animation">The Lottie view whose playback responds to interaction.</param>
    /// <param name="interactionTarget">The visual element that receives native input, or <see langword="null"/> to use <paramref name="animation"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="animation"/> is <see langword="null"/>.</exception>
    public LottiePlaybackInteraction(LottieView animation, VisualElement? interactionTarget = null)
    {
        this.animation = animation ?? throw new ArgumentNullException(nameof(animation));
        this.interactionTarget = interactionTarget ?? animation;
        this.interactionTarget.HandlerChanged += OnHandlerChanged;

        AttachNativeInput();
    }

    /// <summary>
    /// Activates or deactivates interactive playback.
    /// </summary>
    /// <param name="active"><see langword="true"/> to enable looping and start playback; otherwise, disables looping and allows the current iteration to finish.</param>
    public void SetActive(bool active)
    {
        if (isDisposed || isActive == active) return;

        isActive = active;
        animation.IsLooping = active;

        if (!active) return;

        bool reachedPlaybackEnd = animation.IsReversed ? animation.Progress <= 0 : animation.Progress >= 1;

        if (reachedPlaybackEnd) animation.Progress = animation.IsReversed ? 1d : 0d;

        animation.Play();
    }

    private void OnHandlerChanged(object? sender, EventArgs e) => AttachNativeInput();

    private void AttachNativeInput()
    {
        DetachNativeInput();

        #if WINDOWS
        nativeView = interactionTarget.Handler?.PlatformView as UIElement;
        if (nativeView is not null)
        {
            nativeView.PointerEntered += NativePointerEntered;
            nativeView.PointerExited += NativePointerExited;
        }
        #elif ANDROID
        nativeView = interactionTarget.Handler?.PlatformView as Android.Views.View;
        if (nativeView is not null)
        {
            nativeView.Clickable = true;
            nativeView.Touch += NativeTouch;
        }
        #elif IOS || MACCATALYST
        nativeView = interactionTarget.Handler?.PlatformView as UIView;
        if (nativeView is not null)
        {
            pressRecognizer = new UILongPressGestureRecognizer(NativePress)
            {
                MinimumPressDuration = 0.05,
                CancelsTouchesInView = false
            };

            nativeView.AddGestureRecognizer(pressRecognizer);

            hoverRecognizer = new UIHoverGestureRecognizer(NativeHover);

            nativeView.AddGestureRecognizer(hoverRecognizer);
        }
        #endif
    }

    #if WINDOWS
    private void NativePointerEntered(object sender, PointerRoutedEventArgs e) => SetActive(true);

    private void NativePointerExited(object sender, PointerRoutedEventArgs e) => SetActive(false);
    #endif

    #if ANDROID
    private void NativeTouch(object? sender, Android.Views.View.TouchEventArgs e)
    {
        switch (e.Event?.ActionMasked)
        {
            case MotionEventActions.Down:
                SetActive(true);
                break;
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                SetActive(false);
                break;
        }

        e.Handled = false;
    }
    #endif

    #if IOS || MACCATALYST
    private void NativePress(UILongPressGestureRecognizer recognizer)
    {
        switch (recognizer.State)
        {
            case UIGestureRecognizerState.Began:
                SetActive(true);
                break;
            case UIGestureRecognizerState.Ended:
            case UIGestureRecognizerState.Cancelled:
            case UIGestureRecognizerState.Failed:
                SetActive(false);
                break;
        }
    }

    private void NativeHover(UIHoverGestureRecognizer recognizer)
    {
        switch (recognizer.State)
        {
            case UIGestureRecognizerState.Began:
            case UIGestureRecognizerState.Changed:
                SetActive(true);
                break;
            case UIGestureRecognizerState.Ended:
            case UIGestureRecognizerState.Cancelled:
                SetActive(false);
                break;
        }
    }
    #endif

    private void DetachNativeInput()
    {
        if (nativeView is not null)
        {
            #if WINDOWS
            nativeView.PointerEntered -= NativePointerEntered;
            nativeView.PointerExited -= NativePointerExited;
            #elif ANDROID
            nativeView.Touch -= NativeTouch;
            #elif IOS || MACCATALYST
            if (pressRecognizer is not null) nativeView.RemoveGestureRecognizer(pressRecognizer);
            if (hoverRecognizer is not null) nativeView.RemoveGestureRecognizer(hoverRecognizer);
            #endif
        }

        #if IOS || MACCATALYST
        pressRecognizer?.Dispose();
        hoverRecognizer?.Dispose();
        pressRecognizer = null;
        hoverRecognizer = null;
        #endif
        nativeView = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (isDisposed) return;

        interactionTarget.HandlerChanged -= OnHandlerChanged;
        isActive = false;
        animation.IsLooping = false;
        animation.IsPlaying = false;

        DetachNativeInput();

        isDisposed = true;
    }
}
