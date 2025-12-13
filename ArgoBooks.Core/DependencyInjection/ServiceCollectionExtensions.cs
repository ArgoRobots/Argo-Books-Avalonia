using Microsoft.Extensions.DependencyInjection;

namespace ArgoBooks.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering ArgoBooks.Core services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ArgoBooks core services to the service collection.
    /// </summary>
    public static IServiceCollection AddArgoBooksCore(this IServiceCollection services)
    {
        // Register services as singletons since they manage application-wide state
        // Implementation classes will be added in Phase 2

        return services;
    }
}
