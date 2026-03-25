using EventSourcing.Command.Infrastructure.EventStore.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Command.Infrastructure.EventStore.Extensions;

/// <summary>
/// Extension methods for registering event store services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the event store DbContext and related services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<EventStoreConfiguration>(
            configuration.GetSection(EventStoreConfiguration.SectionName));

        services.Configure<OutboxConfiguration>(
            configuration.GetSection(OutboxConfiguration.SectionName));

        // Get connection string
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("PostgreSQL connection string is required");

        // Register DbContext
        services.AddDbContext<EventStoreDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Configure for performance and reliability
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();
            options.EnableDetailedErrors(false);
        });

        // Register health checks
        services.AddHealthChecks()
            .AddDbContextCheck<EventStoreDbContext>("event-store-db",
                customTestQuery: async (context, cancellationToken) =>
                {
                    // Test basic connectivity and check if tables exist
                    var canConnect = await context.Database.CanConnectAsync(cancellationToken);
                    if (!canConnect)
                        return false;

                    // Check if the event_store table exists
                    var tableExists = await context.Database
                        .SqlQueryRaw<int>("""
                            SELECT COUNT(*)
                            FROM information_schema.tables
                            WHERE table_name = 'event_store'
                            """)
                        .FirstOrDefaultAsync(cancellationToken) > 0;

                    return tableExists;
                });

        return services;
    }

    /// <summary>
    /// Ensures the event store database is created and migrated
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task EnsureEventStoreDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        await context.Database.MigrateAsync();
    }
}
