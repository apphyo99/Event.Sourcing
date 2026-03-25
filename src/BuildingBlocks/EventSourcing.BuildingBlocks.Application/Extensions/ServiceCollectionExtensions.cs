using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using EventSourcing.BuildingBlocks.Application.Behaviors;

namespace EventSourcing.BuildingBlocks.Application.Extensions;

/// <summary>
/// Dependency injection extensions for application building blocks
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application building blocks to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for handlers and validators</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddApplicationBuildingBlocks(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // Add MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(assemblies);

            // Register pipeline behaviors in order of execution
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CorrelationBehavior<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        // Add FluentValidation
        services.AddValidatorsFromAssemblies(assemblies);

        return services;
    }

    /// <summary>
    /// Adds application building blocks with custom MediatR configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="mediatrConfiguration">Custom MediatR configuration</param>
    /// <param name="assemblies">Assemblies to scan for handlers and validators</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddApplicationBuildingBlocks(
        this IServiceCollection services,
        Action<MediatRServiceConfiguration> mediatrConfiguration,
        params Assembly[] assemblies)
    {
        // Add MediatR with custom configuration
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(assemblies);

            // Apply custom configuration
            mediatrConfiguration(config);

            // Register default pipeline behaviors if not overridden
            if (!HasBehaviors(config))
            {
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CorrelationBehavior<,>));
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            }
        });

        // Add FluentValidation
        services.AddValidatorsFromAssemblies(assemblies);

        return services;
    }

    private static bool HasBehaviors(MediatRServiceConfiguration config)
    {
        // This is a simplified check - in a real implementation you might want
        // to inspect the actual registered services to see if behaviors are already registered
        return false;
    }
}
