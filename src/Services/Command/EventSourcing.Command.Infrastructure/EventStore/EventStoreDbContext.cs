using EventSourcing.Command.Infrastructure.EventStore.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Command.Infrastructure.EventStore;

/// <summary>
/// Entity Framework DbContext for the event store database
/// </summary>
public class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Collection of stored events
    /// </summary>
    public DbSet<StoredEvent> Events { get; set; } = null!;

    /// <summary>
    /// Collection of outbox messages
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    /// <summary>
    /// Collection of snapshots
    /// </summary>
    public DbSet<Snapshot> Snapshots { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureStoredEvent(modelBuilder);
        ConfigureOutboxMessage(modelBuilder);
        ConfigureSnapshot(modelBuilder);
    }

    private static void ConfigureStoredEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<StoredEvent>();

        // Primary key
        entity.HasKey(e => e.EventId);

        // Indexes
        entity.HasIndex(e => new { e.StreamId, e.Version })
            .IsUnique()
            .HasDatabaseName("ix_event_store_stream_id_version");

        entity.HasIndex(e => e.StreamId)
            .HasDatabaseName("ix_event_store_stream_id");

        entity.HasIndex(e => e.EventType)
            .HasDatabaseName("ix_event_store_event_type");

        entity.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_event_store_created_at");

        // Properties
        entity.Property(e => e.EventId)
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(e => e.StreamId)
            .IsRequired();

        entity.Property(e => e.StreamType)
            .IsRequired();

        entity.Property(e => e.EventType)
            .IsRequired();

        entity.Property(e => e.EventData)
            .IsRequired();

        entity.Property(e => e.Metadata)
            .IsRequired();
    }

    private static void ConfigureOutboxMessage(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxMessage>();

        // Primary key
        entity.HasKey(e => e.MessageId);

        // Indexes
        entity.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("ix_outbox_status_created_at");

        entity.HasIndex(e => e.EventId)
            .HasDatabaseName("ix_outbox_event_id");

        // Properties
        entity.Property(e => e.MessageId)
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(e => e.TopicName)
            .IsRequired();

        entity.Property(e => e.Payload)
            .IsRequired();

        entity.Property(e => e.Headers)
            .IsRequired();

        entity.Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired();

        // Relationships
        entity.HasOne(e => e.StoredEvent)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureSnapshot(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Snapshot>();

        // Primary key
        entity.HasKey(e => e.StreamId);

        // Properties
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(e => e.StreamId)
            .IsRequired();

        entity.Property(e => e.SnapshotData)
            .IsRequired();
    }
}
