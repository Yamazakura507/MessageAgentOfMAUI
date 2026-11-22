using LottieAgent.Interfaces;
using MessageAgent.Core.Models.VisualValues;
using MessageAgent.Services.Interactions;

namespace MessageAgent.Controls.Provider.StatusMessage;

/// <summary>
/// Displays a Lottie value and plays it while the user hovers or presses it.
/// </summary>
public partial class LottieVisualValueView : ContentView
{
    private readonly LottiePlaybackInteraction playbackInteraction;

    /// <summary>
    /// Initializes the Lottie view and connects the required rendering services.
    /// </summary>
    public LottieVisualValueView(LottieVisualValue value, ILottieJsonLoader jsonLoader, ILottieModifierProcessor modifierProcessor)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(jsonLoader);
        ArgumentNullException.ThrowIfNull(modifierProcessor);

        InitializeComponent();

        playbackInteraction = new LottiePlaybackInteraction(Animation, restingProgress: value.Progress);
        BindingContext = value;

        Animation.JsonLoader = jsonLoader;
        Animation.ModifierProcessor = modifierProcessor;
        Animation.AnimationLoaded += AnimationOnLoaded;
    }

    private void AnimationOnLoaded(object? sender, EventArgs e)
    {
        Animation.IsPlaying = false;
        Animation.Progress = ((LottieVisualValue)BindingContext).Progress;
        Animation.IsLooping = playbackInteraction.IsActive;

        if (playbackInteraction.IsActive) Animation.Play();
    }

    /// <inheritdoc />
    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent is not null) return;

        playbackInteraction.SetActive(false);
        Animation.IsPlaying = false;
    }
}
