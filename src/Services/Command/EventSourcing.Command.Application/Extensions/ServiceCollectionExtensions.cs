using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EventSourcing.Command.Application.Extensions;

/// <summary>
/// Dependency injection extensions for command application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds command application services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddCommandApplication(this IServiceCollection services)
    {
        // Register command handlers and validators from this assembly
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR and FluentValidation are registered via the building blocks
        // This method can be used to register any command-specific services

        return services;
    }
}
