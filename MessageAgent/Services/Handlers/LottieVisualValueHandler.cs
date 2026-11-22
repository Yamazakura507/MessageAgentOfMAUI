using LottieAgent.Interfaces;
using MessageAgent.Controls.Provider.StatusMessage;
using MessageAgent.Core.Models.VisualValues;
using MessageAgent.Core.Models.VisualValues.Abstract;
using MessageAgent.Services.Interfaces;

namespace MessageAgent.Services.Handlers;

/// <summary>
/// Creates interactive Lottie views for <see cref="LottieVisualValue"/> models.
/// </summary>
public sealed class LottieVisualValueHandler(
    ILottieJsonLoader jsonLoader,
    ILottieModifierProcessor modifierProcessor) : IVisualValueHandler
{
    /// <inheritdoc />
    public bool CanHandle(VisualValue visualValue) => visualValue is LottieVisualValue;

    /// <inheritdoc />
    public View CreateView(VisualValue visualValue) => new LottieVisualValueView((LottieVisualValue)visualValue, jsonLoader, modifierProcessor);
}
