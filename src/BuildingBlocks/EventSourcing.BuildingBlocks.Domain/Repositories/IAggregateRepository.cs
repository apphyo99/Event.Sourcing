using EventSourcing.BuildingBlocks.Domain.Aggregates;

namespace EventSourcing.BuildingBlocks.Domain.Repositories;

/// <summary>
/// Generic repository interface for aggregate roots in event sourcing
/// </summary>
/// <typeparam name="T">Aggregate root type</typeparam>
public interface IAggregateRepository<T> where T : AggregateRoot
{
    /// <summary>
    /// Loads an aggregate by its stream identifier
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate if found, null otherwise</returns>
    Task<T?> LoadAsync(string streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an aggregate up to a specific version
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="version">Maximum version to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate if found, null otherwise</returns>
    Task<T?> LoadAsync(string streamId, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an aggregate and publishes its uncommitted events
    /// </summary>
    /// <param name="aggregate">The aggregate to save</param>
    /// <param name="expectedVersion">Expected current version for optimistic concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SaveAsync(T aggregate, int? expectedVersion = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an aggregate exists
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(string streamId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when aggregate concurrency conflicts occur
/// </summary>
public class ConcurrencyException : Exception
{
    public string StreamId { get; }
    public int ExpectedVersion { get; }
    public int ActualVersion { get; }

    public ConcurrencyException(string streamId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict in stream '{streamId}'. Expected version {expectedVersion}, but current version is {actualVersion}")
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

/// <summary>
/// Exception thrown when aggregate is not found
/// </summary>
public class AggregateNotFoundException : Exception
{
    public string StreamId { get; }
    public string StreamType { get; }

    public AggregateNotFoundException(string streamId, string streamType)
        : base($"Aggregate of type '{streamType}' with ID '{streamId}' was not found")
    {
        StreamId = streamId;
        StreamType = streamType;
    }
}
