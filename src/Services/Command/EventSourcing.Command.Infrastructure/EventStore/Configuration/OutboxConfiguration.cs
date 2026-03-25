namespace EventSourcing.Command.Infrastructure.EventStore.Configuration;

/// <summary>
/// Configuration settings for the outbox pattern
/// </summary>
public class OutboxConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Outbox";

    /// <summary>
    /// Number of messages to process in a single batch
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Polling interval for checking new outbox messages
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum number of retry attempts before marking as failed
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts (exponential backoff base)
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay for retry attempts
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Service Bus topic name for publishing events
    /// </summary>
    public string ServiceBusTopicName { get; set; } = "domain-events";

    /// <summary>
    /// Whether to enable poison message handling
    /// </summary>
    public bool EnablePoisonMessageHandling { get; set; } = true;
}
