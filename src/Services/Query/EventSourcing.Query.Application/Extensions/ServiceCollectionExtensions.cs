using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Query.Application.Extensions;

/// <summary>
/// Dependency injection extensions for query application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds query application services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddQueryApplication(this IServiceCollection services)
    {
        // Register query-specific application services here
        // Query handlers and validators are registered via the building blocks

        return services;
    }
}
