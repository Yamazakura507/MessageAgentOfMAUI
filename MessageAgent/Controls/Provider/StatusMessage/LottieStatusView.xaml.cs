using LottieAgent.Interfaces;
using MessageAgent.Core.Enums;
using MessageAgent.Core.Models;
using MessageAgent.Services.Interactions;

namespace MessageAgent.Controls.Provider.StatusMessage;

/// <summary>
/// Lottie status icon. It loops while hovered/pressed and completes the current
/// iteration after the pointer leaves or the press is released.
/// </summary>
public partial class LottieStatusView : ContentView
{
    private readonly LottieStartPosition startPosition;
    private readonly LottiePlaybackInteraction playbackInteraction;

    public LottieStatusView(LottieValueStatus value, ILottieJsonLoader jsonLoader, ILottieModifierProcessor modifierProcessor)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(jsonLoader);
        ArgumentNullException.ThrowIfNull(modifierProcessor);

        InitializeComponent();

        startPosition = value.StartPosition;
        playbackInteraction = new LottiePlaybackInteraction(Animation);
        BindingContext = value;

        Animation.JsonLoader = jsonLoader;
        Animation.ModifierProcessor = modifierProcessor;
        Animation.AnimationLoaded += AnimationOnLoaded;
    }

    private void AnimationOnLoaded(object? sender, EventArgs e)
    {
        Animation.IsPlaying = false;
        Animation.Progress = GetIdleProgress();
        Animation.IsLooping = playbackInteraction.IsActive;

        if (playbackInteraction.IsActive) Animation.Play();
    }

    private double GetIdleProgress() => startPosition == LottieStartPosition.End ? 1d : 0d;

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent is null)
        {
            playbackInteraction.SetActive(false);

            Animation.IsPlaying = false;
        }
    }
}
