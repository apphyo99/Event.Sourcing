using EventSourcing.BuildingBlocks.Application.EventStore;
using EventSourcing.BuildingBlocks.Application.Outbox;
using EventSourcing.BuildingBlocks.Application.Projections;
using EventSourcing.BuildingBlocks.Domain.Repositories;
using EventSourcing.BuildingBlocks.Infrastructure.EventStore;
using EventSourcing.BuildingBlocks.Infrastructure.Messaging;
using EventSourcing.BuildingBlocks.Infrastructure.Outbox;
using EventSourcing.BuildingBlocks.Infrastructure.ReadModels;
using EventSourcing.BuildingBlocks.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventSourcing.BuildingBlocks.Infrastructure.Extensions;

/// <summary>
/// Dependency injection extensions for infrastructure building blocks
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PostgreSQL event store to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddPostgreSqlEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // Add Entity Framework DbContext
        services.AddDbContext<EventStoreDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        // Register event store and outbox repository
        services.AddScoped<IEventStore, PostgreSqlEventStore>();
        services.AddScoped<IOutboxRepository, PostgreSqlOutboxRepository>();

        // Register generic repository
        services.AddScoped(typeof(IAggregateRepository<>), typeof(EventSourcedRepository<>));

        return services;
    }

    /// <summary>
    /// Adds Azure Cosmos DB for read models
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration section containing Cosmos DB settings</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddCosmosDbReadModels(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Cosmos DB settings
        services.Configure<CosmosDbConfiguration>(configuration.GetSection("CosmosDb"));

        // Register Cosmos DB client as singleton
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            var cosmosConfig = serviceProvider.GetRequiredService<IOptions<CosmosDbConfiguration>>().Value;

            if (string.IsNullOrWhiteSpace(cosmosConfig.EndpointUri))
                throw new InvalidOperationException("Cosmos DB endpoint URI is required");

            if (string.IsNullOrWhiteSpace(cosmosConfig.PrimaryKey))
                throw new InvalidOperationException("Cosmos DB primary key is required");

            var cosmosClientOptions = new CosmosClientOptions
            {
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                ConsistencyLevel = ConsistencyLevel.Session
            };

            return new CosmosClient(cosmosConfig.EndpointUri, cosmosConfig.PrimaryKey, cosmosClientOptions);
        });

        // Register repository factory
        services.AddScoped<IReadModelRepositoryFactory, CosmosDbRepositoryFactory>();

        return services;
    }

    /// <summary>
    /// Adds Azure Service Bus messaging
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration section containing Service Bus settings</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddServiceBusMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Service Bus settings
        services.Configure<ServiceBusConfiguration>(configuration.GetSection("ServiceBus"));

        // Register message publisher
        services.AddScoped<IMessagePublisher, ServiceBusMessagePublisher>();

        return services;
    }

    /// <summary>
    /// Adds all infrastructure building blocks
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddInfrastructureBuildingBlocks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection("ConnectionStrings");

        // Add PostgreSQL event store
        var postgresqlConnectionString = connectionStrings["PostgreSQL"];
        if (!string.IsNullOrWhiteSpace(postgresqlConnectionString))
        {
            services.AddPostgreSqlEventStore(postgresqlConnectionString);
        }

        // Add Cosmos DB read models
        services.AddCosmosDbReadModels(configuration);

        // Add Service Bus messaging
        services.AddServiceBusMessaging(configuration);

        return services;
    }

    /// <summary>
    /// Ensures that the event store database is created and migrated
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task EnsureEventStoreDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures that Cosmos DB containers are created
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task EnsureCosmosDbContainersAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        var configuration = serviceProvider.GetRequiredService<IOptions<CosmosDbConfiguration>>().Value;

        // Create database if it doesn't exist
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            configuration.DatabaseName,
            cancellationToken: cancellationToken);

        // Create containers if they don't exist
        foreach (var containerConfig in configuration.Containers.Values)
        {
            await database.Database.CreateContainerIfNotExistsAsync(
                containerConfig.Name,
                containerConfig.PartitionKeyPath,
                containerConfig.ThroughputRUs,
                cancellationToken: cancellationToken);
        }
    }
}
