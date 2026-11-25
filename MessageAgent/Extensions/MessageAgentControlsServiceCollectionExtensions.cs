using CollectionLib.Handlers;
using LottieAgent.Extensions;
using MessageAgent.Controls.Provider;
using MessageAgent.Services;
using MessageAgent.Services.Factory;
using MessageAgent.Services.Handlers;
using MessageAgent.Services.Interfaces;
using MessageAgent.Services.Resolvers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MessageAgent.Extensions;

/// <summary>
/// Registers MessageAgent and all dependencies, including LottieAgent.
/// The consuming MauiProgram only needs to call AddMessageAgentControls().
/// </summary>
public static class MessageAgentControlsServiceCollectionExtensions
{
    /// <summary>
    /// Registers MessageAgent, LottieAgent and all rendering services.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The supplied collection for chained registration.</returns>
    public static IServiceCollection AddMessageAgentControls(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLottieAgent();

        HandlerAction.Progress = new MessageAgentHandlerProgress();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IVisualValueHandler, ImageVisualValueHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IVisualValueHandler, LottieVisualValueHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IVisualValueHandler, ElementVisualValueHandler>());

        services.TryAddSingleton<IVisualValueViewFactory, VisualValueViewFactory>();
        services.TryAddSingleton<IStatusBorderColorResolver, StatusBorderColorResolver>();
        services.TryAddSingleton<IMessagePresentationService, MessagePresentationService>();

        services.TryAddTransient<IMessageLabelAnimator, MessageLabelAnimator>();
        services.TryAddTransient<IMessageAutoHideService, MessageAutoHideService>();
        services.TryAddTransient<IMessageDetailsPageFactory, MessageDetailsPageFactory>();

        services.AddTransient<MessageProvider>();

        return services;
    }
}
