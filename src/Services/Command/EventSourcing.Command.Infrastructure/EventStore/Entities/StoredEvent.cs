using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventSourcing.Command.Infrastructure.EventStore.Entities;

/// <summary>
/// Represents a stored event in the event store
/// </summary>
[Table("event_store")]
public class StoredEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    [Key]
    [Column("event_id")]
    public Guid EventId { get; set; }

    /// <summary>
    /// Identifier of the aggregate stream
    /// </summary>
    [Column("stream_id")]
    [StringLength(200)]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the aggregate stream
    /// </summary>
    [Column("stream_type")]
    [StringLength(100)]
    public string StreamType { get; set; } = string.Empty;

    /// <summary>
    /// Version number within the stream (for optimistic concurrency)
    /// </summary>
    [Column("version")]
    public int Version { get; set; }

    /// <summary>
    /// Type of the domain event
    /// </summary>
    [Column("event_type")]
    [StringLength(200)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event data in JSON format
    /// </summary>
    [Column("event_data", TypeName = "jsonb")]
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Event metadata (correlation ID, causation ID, actor, etc.)
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string Metadata { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
