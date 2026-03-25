using EventSourcing.BuildingBlocks.Domain.Events;

namespace EventSourcing.BuildingBlocks.Application.Projections;

/// <summary>
/// Base interface for all projection handlers
/// </summary>
public interface IProjectionHandler
{
    /// <summary>
    /// Gets the events that this projection handles
    /// </summary>
    IEnumerable<Type> HandledEventTypes { get; }

    /// <summary>
    /// Handles a domain event and updates the projection
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed projection handler for specific event types
/// </summary>
/// <typeparam name="TEvent">The type of event to handle</typeparam>
public interface IProjectionHandler<in TEvent> : IProjectionHandler
    where TEvent : DomainEvent
{
    /// <summary>
    /// Handles a specific type of domain event
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Implementation of the base interface
    /// </summary>
    async Task IProjectionHandler.HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is TEvent typedEvent)
        {
            await HandleAsync(typedEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Gets the handled event types
    /// </summary>
    IEnumerable<Type> IProjectionHandler.HandledEventTypes => new[] { typeof(TEvent) };
}

/// <summary>
/// Repository interface for read model operations
/// </summary>
/// <typeparam name="TReadModel">The type of read model</typeparam>
public interface IReadModelRepository<TReadModel> where TReadModel : class
{
    /// <summary>
    /// Gets a read model by its identifier
    /// </summary>
    /// <param name="id">The identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The read model if found, null otherwise</returns>
    Task<TReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a read model
    /// </summary>
    /// <param name="readModel">The read model to upsert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UpsertAsync(TReadModel readModel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a read model by its identifier
    /// </summary>
    /// <param name="id">The identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a read model exists
    /// </summary>
    /// <param name="id">The identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
