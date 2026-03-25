using EventSourcing.BuildingBlocks.Application.Projections;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace EventSourcing.BuildingBlocks.Infrastructure.ReadModels;

/// <summary>
/// Configuration for Azure Cosmos DB
/// </summary>
public class CosmosDbConfiguration
{
    /// <summary>
    /// Cosmos DB endpoint URL
    /// </summary>
    public string EndpointUri { get; set; } = string.Empty;

    /// <summary>
    /// Cosmos DB primary key
    /// </summary>
    public string PrimaryKey { get; set; } = string.Empty;

    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Container configurations
    /// </summary>
    public Dictionary<string, ContainerConfiguration> Containers { get; set; } = new();
}

/// <summary>
/// Container configuration for Cosmos DB
/// </summary>
public class ContainerConfiguration
{
    /// <summary>
    /// Container name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Partition key path
    /// </summary>
    public string PartitionKeyPath { get; set; } = "/id";

    /// <summary>
    /// Provisioned throughput (RU/s)
    /// </summary>
    public int? ThroughputRUs { get; set; }
}

/// <summary>
/// Cosmos DB implementation of read model repository
/// </summary>
/// <typeparam name="TReadModel">The type of read model</typeparam>
public class CosmosDbRepository<TReadModel> : IReadModelRepository<TReadModel>
    where TReadModel : class, IReadModel
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbRepository<TReadModel>> _logger;
    private readonly string _partitionKeyPath;

    public CosmosDbRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbConfiguration> configuration,
        ILogger<CosmosDbRepository<TReadModel>> logger)
    {
        var config = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var readModelType = typeof(TReadModel).Name;
        if (!config.Containers.TryGetValue(readModelType, out var containerConfig))
        {
            throw new InvalidOperationException($"Container configuration not found for read model: {readModelType}");
        }

        var database = cosmosClient.GetDatabase(config.DatabaseName);
        _container = database.GetContainer(containerConfig.Name);
        _partitionKeyPath = containerConfig.PartitionKeyPath;

        _logger.LogDebug("Initialized Cosmos DB repository for {ReadModelType}", readModelType);
    }

    public async Task<TReadModel?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

        _logger.LogDebug("Getting read model {ReadModelType} with ID {Id}", typeof(TReadModel).Name, id);

        try
        {
            var response = await _container.ReadItemAsync<TReadModel>(
                id: id,
                partitionKey: new PartitionKey(id),
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Successfully retrieved read model {ReadModelType} with ID {Id} (RU: {RequestCharge})",
                typeof(TReadModel).Name, id, response.RequestCharge);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Read model {ReadModelType} with ID {Id} not found", typeof(TReadModel).Name, id);
            return null;
        }
    }

    public async Task UpsertAsync(
        TReadModel readModel,
        CancellationToken cancellationToken = default)
    {
        if (readModel == null)
            throw new ArgumentNullException(nameof(readModel));

        _logger.LogDebug(
            "Upserting read model {ReadModelType} with ID {Id}",
            typeof(TReadModel).Name, readModel.Id);

        try
        {
            var response = await _container.UpsertItemAsync(
                item: readModel,
                partitionKey: new PartitionKey(readModel.Id),
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Successfully upserted read model {ReadModelType} with ID {Id} (RU: {RequestCharge})",
                typeof(TReadModel).Name, readModel.Id, response.RequestCharge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to upsert read model {ReadModelType} with ID {Id}",
                typeof(TReadModel).Name, readModel.Id);
            throw;
        }
    }

    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

        _logger.LogDebug("Deleting read model {ReadModelType} with ID {Id}", typeof(TReadModel).Name, id);

        try
        {
            var response = await _container.DeleteItemAsync<TReadModel>(
                id: id,
                partitionKey: new PartitionKey(id),
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Successfully deleted read model {ReadModelType} with ID {Id} (RU: {RequestCharge})",
                typeof(TReadModel).Name, id, response.RequestCharge);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Read model {ReadModelType} with ID {Id} not found for deletion", typeof(TReadModel).Name, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete read model {ReadModelType} with ID {Id}",
                typeof(TReadModel).Name, id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

        try
        {
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            using var resultSetIterator = _container.GetItemQueryIterator<int>(query);
            var response = await resultSetIterator.ReadNextAsync(cancellationToken);

            var count = response.FirstOrDefault();
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to check existence of read model {ReadModelType} with ID {Id}",
                typeof(TReadModel).Name, id);
            throw;
        }
    }
}

/// <summary>
/// Base interface for read models
/// </summary>
public interface IReadModel
{
    /// <summary>
    /// Unique identifier for the read model
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Factory for creating read model repositories
/// </summary>
public interface IReadModelRepositoryFactory
{
    /// <summary>
    /// Creates a repository for the specified read model type
    /// </summary>
    /// <typeparam name="TReadModel">The type of read model</typeparam>
    /// <returns>Repository instance</returns>
    IReadModelRepository<TReadModel> CreateRepository<TReadModel>() where TReadModel : class, IReadModel;
}

/// <summary>
/// Cosmos DB implementation of read model repository factory
/// </summary>
public class CosmosDbRepositoryFactory : IReadModelRepositoryFactory
{
    private readonly CosmosClient _cosmosClient;
    private readonly IOptions<CosmosDbConfiguration> _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public CosmosDbRepositoryFactory(
        CosmosClient cosmosClient,
        IOptions<CosmosDbConfiguration> configuration,
        ILoggerFactory loggerFactory)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public IReadModelRepository<TReadModel> CreateRepository<TReadModel>() where TReadModel : class, IReadModel
    {
        var logger = _loggerFactory.CreateLogger<CosmosDbRepository<TReadModel>>();
        return new CosmosDbRepository<TReadModel>(_cosmosClient, _configuration, logger);
    }
}
