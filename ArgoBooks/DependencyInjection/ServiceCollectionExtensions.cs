using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ArgoBooks.DependencyInjection;

/// <summary>
/// Extension methods for registering ArgoBooks UI services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ArgoBooks UI services to the service collection.
    /// </summary>
    public static IServiceCollection AddArgoBooksUI(this IServiceCollection services)
    {
        // Theme service
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());

        return services;
    }
}
