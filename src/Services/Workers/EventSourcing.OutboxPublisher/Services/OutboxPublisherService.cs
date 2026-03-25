using EventSourcing.BuildingBlocks.Application.Outbox;
using EventSourcing.BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace EventSourcing.OutboxPublisher.Services;

/// <summary>
/// Configuration options for the outbox publisher
/// </summary>
public class OutboxPublisherOptions
{
    /// <summary>
    /// Polling interval for checking pending messages
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Batch size for processing messages
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retries
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether to delete successfully published messages
    /// </summary>
    public bool DeletePublishedMessages { get; set; } = false;

    /// <summary>
    /// Maximum age of messages to retain (for cleanup)
    /// </summary>
    public TimeSpan MaxMessageAge { get; set; } = TimeSpan.FromDays(7);
}

/// <summary>
/// Background service that publishes outbox messages to the message bus
/// </summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxPublisherOptions _options;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public OutboxPublisherService(
        IServiceProvider serviceProvider,
        IOptions<OutboxPublisherOptions> options,
        ILogger<OutboxPublisherService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure retry policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + _options.RetryDelay,
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount}/{MaxRetries} for outbox message publishing after {Delay}ms. Exception: {Exception}",
                        retryCount, _options.MaxRetryAttempts, timespan.TotalMilliseconds, outcome.Exception?.Message);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox Publisher Service started. Polling interval: {PollingInterval}, Batch size: {BatchSize}",
            _options.PollingInterval, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages");
            }

            await Task.Delay(_options.PollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Publisher Service stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var pendingMessages = await outboxRepository.GetPendingMessagesAsync(_options.BatchSize, cancellationToken);
        var messageList = pendingMessages.ToList();

        if (!messageList.Any())
        {
            _logger.LogDebug("No pending outbox messages found");
            return;
        }

        _logger.LogInformation("Processing {MessageCount} pending outbox messages", messageList.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var message in messageList)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await messagePublisher.PublishRawAsync(
                        message.TopicName,
                        message.Payload,
                        message.Headers,
                        cancellationToken);
                });

                await outboxRepository.MarkAsPublishedAsync(
                    message.MessageId,
                    DateTime.UtcNow,
                    cancellationToken);

                successCount++;

                _logger.LogDebug(
                    "Successfully published outbox message {MessageId} to topic {TopicName}",
                    message.MessageId, message.TopicName);
            }
            catch (Exception ex)
            {
                await outboxRepository.IncrementRetryCountAsync(message.MessageId, cancellationToken);

                if (message.RetryCount >= _options.MaxRetryAttempts)
                {
                    await outboxRepository.MarkAsFailedAsync(
                        message.MessageId,
                        ex.Message,
                        cancellationToken);

                    _logger.LogError(ex,
                        "Failed to publish outbox message {MessageId} after {RetryCount} attempts. Marked as failed.",
                        message.MessageId, message.RetryCount + 1);
                }
                else
                {
                    _logger.LogWarning(ex,
                        "Failed to publish outbox message {MessageId}, will retry. Attempt {RetryCount}/{MaxRetries}",
                        message.MessageId, message.RetryCount + 1, _options.MaxRetryAttempts);
                }

                failureCount++;
            }
        }

        _logger.LogInformation(
            "Completed processing outbox messages. Success: {SuccessCount}, Failures: {FailureCount}",
            successCount, failureCount);

        // Optionally clean up old messages
        if (_options.DeletePublishedMessages)
        {
            await CleanupOldMessagesAsync(outboxRepository, cancellationToken);
        }
    }

    private async Task CleanupOldMessagesAsync(IOutboxRepository outboxRepository, CancellationToken cancellationToken)
    {
        try
        {
            // Implementation would depend on having a cleanup method in the repository
            // For now, this is a placeholder for the cleanup logic
            _logger.LogDebug("Cleanup of old outbox messages would be performed here");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during outbox message cleanup");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox Publisher Service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}
