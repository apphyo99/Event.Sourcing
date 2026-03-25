using EventSourcing.BuildingBlocks.Application.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using EventSourcing.BuildingBlocks.Infrastructure.EventStore;

namespace EventSourcing.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// PostgreSQL implementation of the outbox repository
/// </summary>
public class PostgreSqlOutboxRepository : IOutboxRepository
{
    private readonly EventStoreDbContext _context;
    private readonly ILogger<PostgreSqlOutboxRepository> _logger;

    public PostgreSqlOutboxRepository(
        EventStoreDbContext context,
        ILogger<PostgreSqlOutboxRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddMessagesAsync(
        IEnumerable<OutboxMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        if (!messageList.Any())
            return;

        _logger.LogDebug("Adding {MessageCount} messages to outbox", messageList.Count);

        var entities = messageList.Select(CreateEntity).ToList();

        _context.OutboxMessages.AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully added {MessageCount} messages to outbox", messageList.Count);
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));

        _logger.LogDebug("Retrieving up to {BatchSize} pending messages", batchSize);

        var entities = await _context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending.ToString())
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {MessageCount} pending messages", entities.Count);

        return entities.Select(e => e.ToOutboxMessage()).ToList();
    }

    public async Task MarkAsPublishedAsync(
        Guid messageId,
        DateTime publishedAt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking message {MessageId} as published", messageId);

        var rowsAffected = await _context.OutboxMessages
            .Where(m => m.MessageId == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, OutboxMessageStatus.Published.ToString())
                    .SetProperty(m => m.PublishedAt, publishedAt),
                cancellationToken);

        if (rowsAffected == 0)
        {
            _logger.LogWarning("No message found with ID {MessageId} to mark as published", messageId);
        }
        else
        {
            _logger.LogDebug("Successfully marked message {MessageId} as published", messageId);
        }
    }

    public async Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking message {MessageId} as failed with error: {Error}", messageId, error);

        var rowsAffected = await _context.OutboxMessages
            .Where(m => m.MessageId == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, OutboxMessageStatus.Failed.ToString())
                    .SetProperty(m => m.LastError, error),
                cancellationToken);

        if (rowsAffected == 0)
        {
            _logger.LogWarning("No message found with ID {MessageId} to mark as failed", messageId);
        }
        else
        {
            _logger.LogInformation("Marked message {MessageId} as failed", messageId);
        }
    }

    public async Task IncrementRetryCountAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Incrementing retry count for message {MessageId}", messageId);

        var rowsAffected = await _context.OutboxMessages
            .Where(m => m.MessageId == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.RetryCount, m => m.RetryCount + 1),
                cancellationToken);

        if (rowsAffected == 0)
        {
            _logger.LogWarning("No message found with ID {MessageId} to increment retry count", messageId);
        }
        else
        {
            _logger.LogDebug("Successfully incremented retry count for message {MessageId}", messageId);
        }
    }

    private static OutboxMessageEntity CreateEntity(OutboxMessage message)
    {
        return new OutboxMessageEntity
        {
            MessageId = message.MessageId,
            EventId = message.EventId,
            TopicName = message.TopicName,
            Payload = message.Payload,
            Headers = JsonSerializer.Serialize(message.Headers),
            Status = message.Status.ToString(),
            RetryCount = message.RetryCount,
            CreatedAt = message.CreatedAt,
            PublishedAt = message.PublishedAt,
            LastError = message.LastError
        };
    }
}
