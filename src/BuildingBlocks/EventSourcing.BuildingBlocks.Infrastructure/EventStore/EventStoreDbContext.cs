using EventSourcing.BuildingBlocks.Application.EventStore;
using EventSourcing.BuildingBlocks.Application.Outbox;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.BuildingBlocks.Infrastructure.EventStore;

/// <summary>
/// Entity Framework DbContext for the event store
/// </summary>
public class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Stored events table
    /// </summary>
    public DbSet<StoredEventEntity> Events { get; set; } = null!;

    /// <summary>
    /// Outbox messages table
    /// </summary>
    public DbSet<OutboxMessageEntity> OutboxMessages { get; set; } = null!;

    /// <summary>
    /// Snapshots table
    /// </summary>
    public DbSet<SnapshotEntity> Snapshots { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure stored events
        modelBuilder.Entity<StoredEventEntity>(entity =>
        {
            entity.ToTable("event_store");

            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.StreamId).HasColumnName("stream_id").HasMaxLength(200).IsRequired();
            entity.Property(e => e.StreamType).HasColumnName("stream_type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(200).IsRequired();
            entity.Property(e => e.EventData).HasColumnName("event_data").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            // Unique constraint on stream_id and version for optimistic concurrency
            entity.HasIndex(e => new { e.StreamId, e.Version }).IsUnique();

            // Index on stream_id for efficient loading
            entity.HasIndex(e => e.StreamId);

            // Index on stream_type for querying by aggregate type
            entity.HasIndex(e => e.StreamType);
        });

        // Configure outbox messages
        modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.ToTable("outbox_messages");

            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(e => e.TopicName).HasColumnName("topic_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Headers).HasColumnName("headers").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.RetryCount).HasColumnName("retry_count").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.LastError).HasColumnName("last_error");

            // Index on status for efficient querying of pending messages
            entity.HasIndex(e => e.Status);

            // Index on event_id for correlation
            entity.HasIndex(e => e.EventId);

            // Index on created_at for ordering
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure snapshots
        modelBuilder.Entity<SnapshotEntity>(entity =>
        {
            entity.ToTable("snapshots");

            entity.HasKey(e => e.StreamId);
            entity.Property(e => e.StreamId).HasColumnName("stream_id").HasMaxLength(200);
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.SnapshotData).HasColumnName("snapshot_data").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        });
    }
}

/// <summary>
/// Entity class for stored events
/// </summary>
public class StoredEventEntity
{
    public Guid EventId { get; set; }
    public string StreamId { get; set; } = string.Empty;
    public string StreamType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Converts to application StoredEvent
    /// </summary>
    public StoredEvent ToStoredEvent()
    {
        return new StoredEvent
        {
            EventId = EventId,
            StreamId = StreamId,
            StreamType = StreamType,
            Version = Version,
            EventType = EventType,
            EventData = EventData,
            Metadata = Metadata,
            CreatedAt = CreatedAt
        };
    }
}

/// <summary>
/// Entity class for outbox messages
/// </summary>
public class OutboxMessageEntity
{
    public Guid MessageId { get; set; }
    public Guid EventId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Headers { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? LastError { get; set; }

    /// <summary>
    /// Converts to application OutboxMessage
    /// </summary>
    public OutboxMessage ToOutboxMessage()
    {
        return new OutboxMessage
        {
            MessageId = MessageId,
            EventId = EventId,
            TopicName = TopicName,
            Payload = Payload,
            Headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Headers) ?? new Dictionary<string, string>(),
            Status = Enum.Parse<OutboxMessageStatus>(Status),
            RetryCount = RetryCount,
            CreatedAt = CreatedAt,
            PublishedAt = PublishedAt,
            LastError = LastError
        };
    }
}

/// <summary>
/// Entity class for snapshots
/// </summary>
public class SnapshotEntity
{
    public string StreamId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string SnapshotData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
