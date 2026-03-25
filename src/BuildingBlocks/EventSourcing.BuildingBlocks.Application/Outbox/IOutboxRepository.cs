namespace EventSourcing.BuildingBlocks.Application.Outbox;

/// <summary>
/// Represents a message in the outbox pattern
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for the outbox message
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Event ID that this message represents
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Topic or queue name for publishing
    /// </summary>
    public string TopicName { get; set; } = string.Empty;

    /// <summary>
    /// Serialized message payload
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Message headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Current status of the message
    /// </summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the message was published (if applicable)
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Last error message (if applicable)
    /// </summary>
    public string? LastError { get; set; }
}

/// <summary>
/// Status of an outbox message
/// </summary>
public enum OutboxMessageStatus
{
    Pending = 0,
    Published = 1,
    Failed = 2
}

/// <summary>
/// Repository for managing outbox messages
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds messages to the outbox
    /// </summary>
    /// <param name="messages">Messages to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task AddMessagesAsync(
        IEnumerable<OutboxMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages for publishing
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of pending messages</returns>
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks message as published
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="publishedAt">When the message was published</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task MarkAsPublishedAsync(
        Guid messageId,
        DateTime publishedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks message as failed
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="error">Error message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments retry count for a message
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task IncrementRetryCountAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);
}
