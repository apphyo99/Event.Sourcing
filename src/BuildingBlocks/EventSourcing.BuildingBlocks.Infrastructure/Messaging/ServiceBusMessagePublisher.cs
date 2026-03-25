using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Text;
using System.Text.Json;

namespace EventSourcing.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Configuration for Azure Service Bus
/// </summary>
public class ServiceBusConfiguration
{
    /// <summary>
    /// Connection string to Azure Service Bus
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum retry attempts for failed messages
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum message size in bytes
    /// </summary>
    public long MaxMessageSizeInBytes { get; set; } = 1024 * 1024; // 1MB
}

/// <summary>
/// Interface for message publishing
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to a topic
    /// </summary>
    /// <param name="topicName">Topic name</param>
    /// <param name="message">Message content</param>
    /// <param name="headers">Message headers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync(
        string topicName,
        object message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a raw message to a topic
    /// </summary>
    /// <param name="topicName">Topic name</param>
    /// <param name="payload">Raw message payload</param>
    /// <param name="headers">Message headers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishRawAsync(
        string topicName,
        string payload,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Azure Service Bus implementation of message publisher
/// </summary>
public class ServiceBusMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusConfiguration _configuration;
    private readonly ILogger<ServiceBusMessagePublisher> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly Dictionary<string, ServiceBusSender> _senderCache = new();
    private readonly SemaphoreSlim _senderCacheSemaphore = new(1, 1);

    public ServiceBusMessagePublisher(
        IOptions<ServiceBusConfiguration> configuration,
        ILogger<ServiceBusMessagePublisher> logger)
    {
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_configuration.ConnectionString))
            throw new InvalidOperationException("Service Bus connection string is required");

        _client = new ServiceBusClient(_configuration.ConnectionString);

        // Configure retry policy using Polly
        _retryPolicy = Policy
            .Handle<ServiceBusException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: _configuration.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + _configuration.RetryDelay,
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount}/{MaxRetries} for message publishing after {Delay}ms. Exception: {Exception}",
                        retryCount, _configuration.MaxRetryAttempts, timespan.TotalMilliseconds, outcome.Exception?.Message);
                });
    }

    public async Task PublishAsync(
        string topicName,
        object message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Topic name cannot be null or empty", nameof(topicName));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var payload = JsonSerializer.Serialize(message, message.GetType());
        await PublishRawAsync(topicName, payload, headers, cancellationToken);
    }

    public async Task PublishRawAsync(
        string topicName,
        string payload,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Topic name cannot be null or empty", nameof(topicName));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload cannot be null or empty", nameof(payload));

        _logger.LogDebug("Publishing message to topic {TopicName}", topicName);

        var sender = await GetSenderAsync(topicName);
        var messageBytes = Encoding.UTF8.GetBytes(payload);

        if (messageBytes.Length > _configuration.MaxMessageSizeInBytes)
        {
            throw new InvalidOperationException(
                $"Message size ({messageBytes.Length} bytes) exceeds maximum allowed size ({_configuration.MaxMessageSizeInBytes} bytes)");
        }

        var serviceBusMessage = new ServiceBusMessage(messageBytes)
        {
            MessageId = Guid.NewGuid().ToString(),
            ContentType = "application/json"
        };

        // Add headers as application properties
        if (headers != null)
        {
            foreach (var header in headers)
            {
                serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
            }
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
        });

        _logger.LogInformation(
            "Successfully published message {MessageId} to topic {TopicName}",
            serviceBusMessage.MessageId, topicName);
    }

    private async Task<ServiceBusSender> GetSenderAsync(string topicName)
    {
        if (_senderCache.TryGetValue(topicName, out var existingSender))
        {
            return existingSender;
        }

        await _senderCacheSemaphore.WaitAsync();

        try
        {
            // Double-check locking pattern
            if (_senderCache.TryGetValue(topicName, out existingSender))
            {
                return existingSender;
            }

            var sender = _client.CreateSender(topicName);
            _senderCache[topicName] = sender;

            _logger.LogDebug("Created new sender for topic {TopicName}", topicName);

            return sender;
        }
        finally
        {
            _senderCacheSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing Service Bus message publisher");

        await _senderCacheSemaphore.WaitAsync();

        try
        {
            foreach (var sender in _senderCache.Values)
            {
                await sender.DisposeAsync();
            }

            _senderCache.Clear();
        }
        finally
        {
            _senderCacheSemaphore.Release();
        }

        await _client.DisposeAsync();
        _senderCacheSemaphore.Dispose();

        _logger.LogInformation("Service Bus message publisher disposed");
    }
}
