using ArgoBooks.Core.Services;
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
        // Register insights service for AI-style analytics
        services.AddSingleton<IInsightsService, InsightsService>();

        return services;
    }
}
