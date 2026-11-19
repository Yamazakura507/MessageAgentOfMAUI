using MessageAgent.Core.Enums;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services;

/// <summary>
/// Owns the visual transition mechanics of a message label.
/// </summary>
public sealed class MessageLabelAnimator : IMessageLabelAnimator
{
    private int animationVersion;

    public async Task SetVisibilityAsync(Border target, bool visible, MessageLabelEffect effect, bool animated)
    {
        ArgumentNullException.ThrowIfNull(target);

        int version = Interlocked.Increment(ref animationVersion);

        target.CancelAnimations();

        if (!animated || (!visible && !target.IsVisible))
        {
            ApplyFinalState(target, visible);

            return;
        }

        if (visible)
        {
            target.IsVisible = true;
            target.Opacity = 0;

            ApplyEffectOffset(target, effect, isShowing: true);
            await RunAnimationAsync(target, effect, 1, true, 320, Easing.CubicOut);
        }
        else
        {
            await RunAnimationAsync(target, effect, 0, false, 290, Easing.CubicIn);
        }

        if (version == animationVersion) ApplyFinalState(target, visible);
    }

    private static void ApplyFinalState(Border target, bool visible)
    {
        target.IsVisible = visible;
        target.Opacity = 1;

        ResetTransform(target);
    }

    private static void ApplyEffectOffset(Border target, MessageLabelEffect effect, bool isShowing)
    {
        ResetTransform(target);

        (double scale, double tx, double ty, double rot) = GetEffectValues(effect, isShowing);

        target.Scale = scale;
        target.TranslationX = tx;
        target.TranslationY = ty;
        target.Rotation = rot;
    }

    private static Task RunAnimationAsync(Border target, MessageLabelEffect effect, double toOpacity, bool isShowing, uint duration, Easing easing)
    {
        (double scale, double tx, double ty, double rot) = isShowing ? (1d, 0d, 0d, 0d) : GetEffectValues(effect, isShowing: false);

        return effect switch
        {
            MessageLabelEffect.FadeScale => Task.WhenAll(
                target.FadeToAsync(toOpacity, duration, easing),
                target.ScaleToAsync(scale, duration, easing)),
            MessageLabelEffect.Slide => Task.WhenAll(
                target.FadeToAsync(toOpacity, duration, easing),
                target.TranslateToAsync(tx, ty, duration, easing)),
            MessageLabelEffect.Scatter => Task.WhenAll(
                target.FadeToAsync(toOpacity, duration, easing),
                target.ScaleToAsync(scale, duration, easing),
                target.TranslateToAsync(tx, ty, duration, easing),
                target.RotateToAsync(rot, duration, easing)),
            _ => target.FadeToAsync(toOpacity, duration, easing)
        };
    }

    private static (double Scale, double Tx, double Ty, double Rot) GetEffectValues(MessageLabelEffect effect, bool isShowing) => (effect, isShowing) switch
    {
        (MessageLabelEffect.FadeScale, _) => (0.88, 0, 0, 0),
        (MessageLabelEffect.Slide, true) => (1, 18, 0, 0),
        (MessageLabelEffect.Slide, false) => (1, -18, 0, 0),
        (MessageLabelEffect.Scatter, true) => (0.72, -14, 10, 5),
        (MessageLabelEffect.Scatter, false) => (0.72, 18, -12, -5),
        _ => (1, 0, 0, 0)
    };

    private static void ResetTransform(VisualElement target)
    {
        target.Scale = 1;
        target.TranslationX = 0;
        target.TranslationY = 0;
        target.Rotation = 0;
    }
}
