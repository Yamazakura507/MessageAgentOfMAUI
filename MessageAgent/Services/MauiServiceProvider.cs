namespace MessageAgent.Services;

/// <summary>
/// XAML-created views cannot use constructor injection. This bridge resolves their
/// dependencies from the active MAUI application container; DI-created views use
/// their normal constructor instead.
/// </summary>
internal static class MauiServiceProvider
{
    public static T GetRequiredService<T>() where T : notnull
    {
        IServiceProvider services = IPlatformApplication.Current?.Services
            ?? Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("The MAUI service provider is not available. Register AddMessageAgentControls() before creating MessageProvider.");

        return services.GetRequiredService<T>();
    }
}
