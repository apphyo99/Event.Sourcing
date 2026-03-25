using EventSourcing.Command.Domain.Orders;
using EventSourcing.Command.Infrastructure.Orders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Command.Infrastructure.Extensions;

/// <summary>
/// Dependency injection extensions for command infrastructure services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds command infrastructure services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddCommandInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register domain-specific repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Additional command-specific infrastructure services can be added here

        return services;
    }
}
