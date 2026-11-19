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
    public static IServiceCollection AddMessageAgentControls(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLottieAgent();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValueStatusHandler, ImageValueStatusHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValueStatusHandler, LottieValueStatusHandler>());

        services.TryAddSingleton<IValueStatusViewFactory, ValueStatusViewFactory>();
        services.TryAddSingleton<IStatusBorderColorResolver, StatusBorderColorResolver>();
        services.TryAddSingleton<IMessagePresentationService, MessagePresentationService>();

        services.TryAddTransient<IMessageLabelAnimator, MessageLabelAnimator>();
        services.TryAddTransient<IMessageAutoHideService, MessageAutoHideService>();
        services.TryAddTransient<IMessageDetailsPageFactory, MessageDetailsPageFactory>();

        services.AddTransient<MessageProvider>();

        return services;
    }
}
