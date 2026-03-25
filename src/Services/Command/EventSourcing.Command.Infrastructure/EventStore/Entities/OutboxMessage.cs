using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventSourcing.Command.Infrastructure.EventStore.Entities;

/// <summary>
/// Represents an outbox message for reliable event publishing
/// </summary>
[Table("outbox_messages")]
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for the outbox message
    /// </summary>
    [Key]
    [Column("message_id")]
    public Guid MessageId { get; set; }

    /// <summary>
    /// Reference to the original event
    /// </summary>
    [Column("event_id")]
    public Guid EventId { get; set; }

    /// <summary>
    /// Service Bus topic name where the message should be published
    /// </summary>
    [Column("topic_name")]
    [StringLength(200)]
    public string TopicName { get; set; } = string.Empty;

    /// <summary>
    /// Serialized message payload
    /// </summary>
    [Column("payload", TypeName = "jsonb")]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Message headers (routing, correlation, etc.)
    /// </summary>
    [Column("headers", TypeName = "jsonb")]
    public string Headers { get; set; } = string.Empty;

    /// <summary>
    /// Processing status of the message
    /// </summary>
    [Column("status")]
    [StringLength(50)]
    public OutboxMessageStatus Status { get; set; }

    /// <summary>
    /// Number of publishing retry attempts
    /// </summary>
    [Column("retry_count")]
    public int RetryCount { get; set; }

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the message was successfully published
    /// </summary>
    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Reference to the associated stored event
    /// </summary>
    [ForeignKey(nameof(EventId))]
    public StoredEvent StoredEvent { get; set; } = null!;
}

/// <summary>
/// Status of an outbox message
/// </summary>
public enum OutboxMessageStatus
{
    Pending = 0,
    Published = 1,
    Failed = 2,
    PoisonMessage = 3
}
