using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventSourcing.Command.Infrastructure.EventStore.Entities;

/// <summary>
/// Represents a snapshot of aggregate state for performance optimization
/// </summary>
[Table("snapshots")]
public class Snapshot
{
    /// <summary>
    /// Identifier of the aggregate stream
    /// </summary>
    [Key]
    [Column("stream_id")]
    [StringLength(200)]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Version of the snapshot (matches the event version at snapshot time)
    /// </summary>
    [Column("version")]
    public int Version { get; set; }

    /// <summary>
    /// Serialized aggregate state in JSON format
    /// </summary>
    [Column("snapshot_data", TypeName = "jsonb")]
    public string SnapshotData { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the snapshot was created
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
