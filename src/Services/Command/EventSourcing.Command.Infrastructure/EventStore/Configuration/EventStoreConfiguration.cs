namespace EventSourcing.Command.Infrastructure.EventStore.Configuration;

/// <summary>
/// Configuration settings for the event store
/// </summary>
public class EventStoreConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "EventStore";

    /// <summary>
    /// PostgreSQL connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Number of events to load per batch when rebuilding aggregates
    /// </summary>
    public int StreamBatchSize { get; set; } = 100;

    /// <summary>
    /// How often to create snapshots (every N events)
    /// </summary>
    public int SnapshotFrequency { get; set; } = 50;

    /// <summary>
    /// Maximum number of concurrent connections
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;
}
