using EventSourcing.BuildingBlocks.Application.EventStore;
using EventSourcing.BuildingBlocks.Application.Outbox;
using EventSourcing.BuildingBlocks.Domain.Aggregates;
using EventSourcing.BuildingBlocks.Domain.Events;
using EventSourcing.BuildingBlocks.Domain.Repositories;
using EventSourcing.BuildingBlocks.Infrastructure.EventStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventSourcing.BuildingBlocks.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation for aggregate roots using event sourcing
/// </summary>
/// <typeparam name="T">Aggregate root type</typeparam>
public class EventSourcedRepository<T> : IAggregateRepository<T> where T : AggregateRoot
{
    private readonly IEventStore _eventStore;
    private readonly IOutboxRepository _outboxRepository;
    private readonly EventStoreDbContext _dbContext;
    private readonly ILogger<EventSourcedRepository<T>> _logger;

    public EventSourcedRepository(
        IEventStore eventStore,
        IOutboxRepository outboxRepository,
        EventStoreDbContext dbContext,
        ILogger<EventSourcedRepository<T>> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> LoadAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        _logger.LogDebug("Loading aggregate {AggregateType} with stream ID {StreamId}", typeof(T).Name, streamId);

        var events = await _eventStore.LoadEventsAsync(streamId, cancellationToken);
        if (!events.Any())
        {
            _logger.LogDebug("No events found for stream {StreamId}", streamId);
            return null;
        }

        var aggregate = CreateEmptyAggregate();
        var domainEvents = events.Select(DeserializeEvent).ToList();

        aggregate.LoadFromHistory(domainEvents);

        _logger.LogDebug(
            "Successfully loaded aggregate {AggregateType} with {EventCount} events from stream {StreamId}",
            typeof(T).Name, domainEvents.Count, streamId);

        return aggregate;
    }

    public async Task<T?> LoadAsync(
        string streamId,
        int version,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        if (version <= 0)
            throw new ArgumentException("Version must be greater than zero", nameof(version));

        _logger.LogDebug(
            "Loading aggregate {AggregateType} with stream ID {StreamId} up to version {Version}",
            typeof(T).Name, streamId, version);

        var events = await _eventStore.LoadEventsAsync(streamId, version, cancellationToken);
        if (!events.Any())
        {
            _logger.LogDebug("No events found for stream {StreamId} up to version {Version}", streamId, version);
            return null;
        }

        var aggregate = CreateEmptyAggregate();
        var domainEvents = events.Select(DeserializeEvent).ToList();

        aggregate.LoadFromHistory(domainEvents);

        _logger.LogDebug(
            "Successfully loaded aggregate {AggregateType} with {EventCount} events from stream {StreamId} up to version {Version}",
            typeof(T).Name, domainEvents.Count, streamId, version);

        return aggregate;
    }

    public async Task SaveAsync(
        T aggregate,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (aggregate == null)
            throw new ArgumentNullException(nameof(aggregate));

        var uncommittedEvents = aggregate.GetUncommittedEvents().ToList();
        if (!uncommittedEvents.Any())
        {
            _logger.LogDebug("No uncommitted events to save for aggregate {StreamId}", aggregate.StreamId);
            return;
        }

        _logger.LogDebug(
            "Saving aggregate {AggregateType} with {EventCount} uncommitted events to stream {StreamId}",
            typeof(T).Name, uncommittedEvents.Count, aggregate.StreamId);

        try
        {
            // Single transaction: event_store append + outbox insert (see architecture.md — write-side ACID).
            // Wrapped in execution strategy because Npgsql EnableRetryOnFailure does not support ad-hoc transactions otherwise.
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                await _eventStore.AppendEventsAsync(
                    aggregate.StreamId,
                    aggregate.StreamType,
                    uncommittedEvents,
                    expectedVersion,
                    cancellationToken);

                var outboxMessages = uncommittedEvents.Select(CreateOutboxMessage).ToList();
                await _outboxRepository.AddMessagesAsync(outboxMessages, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                aggregate.MarkEventsAsCommitted();
            });

            _logger.LogInformation(
                "Successfully saved aggregate {AggregateType} with {EventCount} events to stream {StreamId}",
                typeof(T).Name, uncommittedEvents.Count, aggregate.StreamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to save aggregate {AggregateType} to stream {StreamId}",
                typeof(T).Name, aggregate.StreamId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        return await _eventStore.StreamExistsAsync(streamId, cancellationToken);
    }

    private static T CreateEmptyAggregate()
    {
        // Use reflection to create an instance with a private constructor
        return (T)Activator.CreateInstance(typeof(T), true)!;
    }

    private static DomainEvent DeserializeEvent(Application.EventStore.StoredEvent storedEvent)
    {
        // Type.GetType(string) without an assembly-qualified name does not resolve application types.
        var eventTypeName = $"EventSourcing.Command.Domain.Orders.{storedEvent.EventType}";
        Type? eventType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            eventType = assembly.GetType(eventTypeName, throwOnError: false, ignoreCase: false);
            if (eventType != null)
                break;
        }

        if (eventType == null)
        {
            throw new InvalidOperationException($"Could not resolve event type: {storedEvent.EventType}");
        }

        var domainEvent = JsonSerializer.Deserialize(storedEvent.EventData, eventType) as DomainEvent;
        if (domainEvent == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event: {storedEvent.EventType}");
        }

        return domainEvent;
    }

    private static OutboxMessage CreateOutboxMessage(DomainEvent domainEvent)
    {
        var topicName = $"{domainEvent.StreamType}.{domainEvent.GetType().Name}";
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        var headers = new Dictionary<string, string>
        {
            ["EventType"] = domainEvent.GetType().Name,
            ["StreamId"] = domainEvent.StreamId,
            ["StreamType"] = domainEvent.StreamType,
            ["CorrelationId"] = domainEvent.CorrelationId,
            ["CausationId"] = domainEvent.CausationId ?? string.Empty,
            ["OccurredAt"] = domainEvent.OccurredAt.ToString("O")
        };

        return new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            EventId = domainEvent.EventId,
            TopicName = topicName,
            Payload = payload,
            Headers = headers,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }
}
