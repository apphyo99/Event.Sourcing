using EventSourcing.BuildingBlocks.Domain.Events;

namespace EventSourcing.BuildingBlocks.Application.EventStore;

/// <summary>
/// Represents a stored event in the event store
/// </summary>
public class StoredEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Stream identifier that groups related events
    /// </summary>
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the stream (e.g., "Order", "Customer")
    /// </summary>
    public string StreamType { get; set; } = string.Empty;

    /// <summary>
    /// Version of the event within the stream
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Type of the event
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event data
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Serialized metadata
    /// </summary>
    public string Metadata { get; set; } = string.Empty;

    /// <summary>
    /// When the event was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Event store abstraction for persisting and retrieving domain events
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events to a stream
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="streamType">Stream type</param>
    /// <param name="events">Events to append</param>
    /// <param name="expectedVersion">Expected current version for optimistic concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task AppendEventsAsync(
        string streamId,
        string streamType,
        IEnumerable<DomainEvent> events,
        int? expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all events from a stream
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stored events</returns>
    Task<IEnumerable<StoredEvent>> LoadEventsAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads events from a stream up to a specific version
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="version">Maximum version to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of stored events</returns>
    Task<IEnumerable<StoredEvent>> LoadEventsAsync(
        string streamId,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a stream
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current version or null if stream doesn't exist</returns>
    Task<int?> GetStreamVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a stream exists
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stream exists, false otherwise</returns>
    Task<bool> StreamExistsAsync(
        string streamId,
        CancellationToken cancellationToken = default);
}
