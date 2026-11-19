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

namespace MessageAgent.Services.Actions;

/// <summary>
/// Reusable native long-press recognizer for controls that need a hold action.
/// Owns and detaches platform event handlers when the target handler changes.
/// </summary>
internal sealed class NativeLongPressGesture : IDisposable
{
    private readonly VisualElement target;
    private readonly Action callback;
    private readonly TimeSpan duration;
    private readonly bool cancelsTouchesInView;
    private bool isDisposed;

    #if WINDOWS
    private UIElement? nativeView;
    private DispatcherTimer? timer;
    #endif
    #if ANDROID
    private Android.Views.View? nativeView;
    #endif
    #if IOS || MACCATALYST
    private UIView? nativeView;
    private UILongPressGestureRecognizer? recognizer;
    #endif

    public NativeLongPressGesture(VisualElement target, Action callback, TimeSpan duration, bool cancelsTouchesInView = true)
    {
        this.target = target ?? throw new ArgumentNullException(nameof(target));
        this.callback = callback ?? throw new ArgumentNullException(nameof(callback));

        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));

        this.duration = duration;
        this.cancelsTouchesInView = cancelsTouchesInView;
        target.HandlerChanged += OnHandlerChanged;

        AttachNativeInput();
    }

    private void OnHandlerChanged(object? sender, EventArgs e) => AttachNativeInput();

    private void AttachNativeInput()
    {
        DetachNativeInput();

        #if WINDOWS
        nativeView = target.Handler?.PlatformView as UIElement;

        if (nativeView is not null)
        {
            nativeView.PointerPressed += NativePointerPressed;
            nativeView.PointerReleased += NativePointerReleased;
            nativeView.PointerExited += NativePointerReleased;
            nativeView.PointerCanceled += NativePointerReleased;
        }
        #elif ANDROID
        nativeView = target.Handler?.PlatformView as Android.Views.View;

        if (nativeView is not null)
        {
            nativeView.LongClickable = true;
            nativeView.LongClick += NativeLongClick;
        }
        #elif IOS || MACCATALYST
        nativeView = target.Handler?.PlatformView as UIView;

        if (nativeView is not null)
        {
            recognizer = new UILongPressGestureRecognizer(NativeLongPress)
            {
                MinimumPressDuration = duration.TotalSeconds,
                CancelsTouchesInView = cancelsTouchesInView
            };

            nativeView.AddGestureRecognizer(recognizer);
        }
        #endif
    }

    #if WINDOWS
    private void NativePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        timer?.Stop();

        timer ??= new DispatcherTimer { Interval = duration };
        timer.Tick -= TimerOnTick;
        timer.Tick += TimerOnTick;

        timer.Start();
    }

    private void TimerOnTick(object? sender, object e)
    {
        timer?.Stop();
        callback();
    }

    private void NativePointerReleased(object sender, PointerRoutedEventArgs e) => timer?.Stop();
    #endif

    #if ANDROID
    private void NativeLongClick(object? sender, Android.Views.View.LongClickEventArgs e)
    {
        e.Handled = true;

        callback();
    }
    #endif

    #if IOS || MACCATALYST
    private void NativeLongPress(UILongPressGestureRecognizer recognizer)
    {
        if (recognizer.State == UIGestureRecognizerState.Began) callback();
    }
    #endif

    private void DetachNativeInput()
    {
        #if WINDOWS
        if (nativeView is not null)
        {
            nativeView.PointerPressed -= NativePointerPressed;
            nativeView.PointerReleased -= NativePointerReleased;
            nativeView.PointerExited -= NativePointerReleased;
            nativeView.PointerCanceled -= NativePointerReleased;
        }

        timer?.Stop();

        timer = null;
        nativeView = null;
        #elif ANDROID
        if (nativeView is not null) nativeView.LongClick -= NativeLongClick;

        nativeView = null;
        #elif IOS || MACCATALYST
        if (nativeView is not null && recognizer is not null) nativeView.RemoveGestureRecognizer(recognizer);

        recognizer?.Dispose();

        recognizer = null;
        nativeView = null;
        #endif
    }

    public void Dispose()
    {
        if (isDisposed) return;

        isDisposed = true;
        target.HandlerChanged -= OnHandlerChanged;

        DetachNativeInput();
    }
}
