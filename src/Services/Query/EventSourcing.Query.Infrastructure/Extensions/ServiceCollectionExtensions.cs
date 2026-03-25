using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Query.Infrastructure.Extensions;

/// <summary>
/// Dependency injection extensions for query infrastructure services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds query infrastructure services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddQueryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register query-specific infrastructure services here
        // Cosmos DB read model repositories, Redis caching, etc.

        return services;
    }
}
