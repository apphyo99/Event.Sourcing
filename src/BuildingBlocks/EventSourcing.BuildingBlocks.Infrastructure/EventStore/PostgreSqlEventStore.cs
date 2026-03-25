using EventSourcing.BuildingBlocks.Application.EventStore;
using EventSourcing.BuildingBlocks.Domain.Events;
using EventSourcing.BuildingBlocks.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventSourcing.BuildingBlocks.Infrastructure.EventStore;

/// <summary>
/// PostgreSQL implementation of the event store
/// </summary>
public class PostgreSqlEventStore : IEventStore
{
    private readonly EventStoreDbContext _context;
    private readonly ILogger<PostgreSqlEventStore> _logger;

    public PostgreSqlEventStore(
        EventStoreDbContext context,
        ILogger<PostgreSqlEventStore> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AppendEventsAsync(
        string streamId,
        string streamType,
        IEnumerable<DomainEvent> events,
        int? expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        if (string.IsNullOrWhiteSpace(streamType))
            throw new ArgumentException("Stream type cannot be null or empty", nameof(streamType));

        var eventList = events.ToList();
        if (!eventList.Any())
            return;

        _logger.LogDebug(
            "Appending {EventCount} events to stream {StreamId} with expected version {ExpectedVersion}",
            eventList.Count, streamId, expectedVersion);

        // Check optimistic concurrency
        var currentVersion = await GetStreamVersionAsync(streamId, cancellationToken);

        if (expectedVersion.HasValue && currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(streamId, expectedVersion.Value, currentVersion ?? 0);
        }

        var nextVersion = (currentVersion ?? 0) + 1;

        var storedEvents = new List<StoredEventEntity>();

        foreach (var domainEvent in eventList)
        {
            var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            var metadata = JsonSerializer.Serialize(new
            {
                domainEvent.EventId,
                domainEvent.StreamId,
                domainEvent.StreamType,
                domainEvent.Version,
                domainEvent.OccurredAt,
                domainEvent.CorrelationId,
                domainEvent.CausationId,
                domainEvent.Actor
            });

            var storedEvent = new StoredEventEntity
            {
                EventId = domainEvent.EventId,
                StreamId = streamId,
                StreamType = streamType,
                Version = nextVersion,
                EventType = domainEvent.GetType().Name,
                EventData = eventData,
                Metadata = metadata,
                CreatedAt = domainEvent.OccurredAt
            };

            storedEvents.Add(storedEvent);
            nextVersion++;
        }

        try
        {
            _context.Events.AddRange(storedEvents);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully appended {EventCount} events to stream {StreamId}",
                eventList.Count, streamId);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
        {
            // Handle concurrent writes
            var actualVersion = await GetStreamVersionAsync(streamId, cancellationToken);
            throw new ConcurrencyException(streamId, expectedVersion ?? 0, actualVersion ?? 0);
        }
    }

    public async Task<IEnumerable<StoredEvent>> LoadEventsAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        _logger.LogDebug("Loading all events from stream {StreamId}", streamId);

        var events = await _context.Events
            .Where(e => e.StreamId == streamId)
            .OrderBy(e => e.Version)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Loaded {EventCount} events from stream {StreamId}", events.Count, streamId);

        return events.Select(e => e.ToStoredEvent()).ToList();
    }

    public async Task<IEnumerable<StoredEvent>> LoadEventsAsync(
        string streamId,
        int version,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        if (version <= 0)
            throw new ArgumentException("Version must be greater than zero", nameof(version));

        _logger.LogDebug("Loading events from stream {StreamId} up to version {Version}", streamId, version);

        var events = await _context.Events
            .Where(e => e.StreamId == streamId && e.Version <= version)
            .OrderBy(e => e.Version)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "Loaded {EventCount} events from stream {StreamId} up to version {Version}",
            events.Count, streamId, version);

        return events.Select(e => e.ToStoredEvent()).ToList();
    }

    public async Task<int?> GetStreamVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        var version = await _context.Events
            .Where(e => e.StreamId == streamId)
            .MaxAsync(e => (int?)e.Version, cancellationToken);

        return version;
    }

    public async Task<bool> StreamExistsAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        return await _context.Events
            .AnyAsync(e => e.StreamId == streamId, cancellationToken);
    }
}
