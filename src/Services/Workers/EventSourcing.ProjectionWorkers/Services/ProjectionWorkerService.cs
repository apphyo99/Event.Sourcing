using Azure.Messaging.ServiceBus;
using EventSourcing.BuildingBlocks.Domain.Events;
using Microsoft.Extensions.Options;
using Polly;
using System.Text.Json;

namespace EventSourcing.ProjectionWorkers.Services;

/// <summary>
/// Configuration options for projection workers
/// </summary>
public class ProjectionWorkerOptions
{
    /// <summary>
    /// Service Bus topic name to subscribe to
    /// </summary>
    public string TopicName { get; set; } = "Order.Events";

    /// <summary>
    /// Service Bus subscription name
    /// </summary>
    public string SubscriptionName { get; set; } = "ProjectionWorkers";

    /// <summary>
    /// Maximum number of concurrent message processors
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 5;

    /// <summary>
    /// Maximum number of retry attempts for failed projections
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Auto-complete messages after successful processing
    /// </summary>
    public bool AutoCompleteMessages { get; set; } = true;

    /// <summary>
    /// Maximum wait time for receiving messages
    /// </summary>
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Background service that processes Service Bus messages and updates projections
/// </summary>
public class ProjectionWorkerService : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProjectionWorkerOptions _options;
    private readonly ILogger<ProjectionWorkerService> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private ServiceBusProcessor? _processor;

    public ProjectionWorkerService(
        ServiceBusClient serviceBusClient,
        IServiceProvider serviceProvider,
        IOptions<ProjectionWorkerOptions> options,
        ILogger<ProjectionWorkerService> logger)
    {
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure retry policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount}/{MaxRetries} for projection processing after {Delay}ms. Exception: {Exception}",
                        retryCount, _options.MaxRetryAttempts, timespan.TotalMilliseconds, outcome.Exception?.Message);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Projection Worker Service started. Topic: {TopicName}, Subscription: {SubscriptionName}",
            _options.TopicName, _options.SubscriptionName);

        await StartMessageProcessorAsync(stoppingToken);

        // Keep the service running until cancellation is requested
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        await StopMessageProcessorAsync();
        _logger.LogInformation("Projection Worker Service stopped");
    }

    private async Task StartMessageProcessorAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create processor for the topic and subscription
            _processor = _serviceBusClient.CreateProcessor(_options.TopicName, _options.SubscriptionName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = _options.MaxConcurrentCalls,
                AutoCompleteMessages = _options.AutoCompleteMessages,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10),
                PrefetchCount = 10
            });

            // Configure event handlers
            _processor.ProcessMessageAsync += HandleMessageAsync;
            _processor.ProcessErrorAsync += HandleErrorAsync;

            // Start processing
            await _processor.StartProcessingAsync(cancellationToken);

            _logger.LogInformation("Service Bus message processor started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Service Bus message processor");
            throw;
        }
    }

    private async Task StopMessageProcessorAsync()
    {
        if (_processor != null)
        {
            try
            {
                _logger.LogInformation("Stopping Service Bus message processor...");

                await _processor.StopProcessingAsync();
                await _processor.DisposeAsync();

                _logger.LogInformation("Service Bus message processor stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while stopping Service Bus message processor");
            }
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        var correlationId = args.Message.ApplicationProperties.TryGetValue("CorrelationId", out var corrId)
            ? corrId?.ToString() : Guid.NewGuid().ToString();

        using var scope = _logger.BeginScope("MessageId: {MessageId}, CorrelationId: {CorrelationId}", messageId, correlationId);

        try
        {
            _logger.LogDebug("Processing message {MessageId}", messageId);

            var messageBody = args.Message.Body.ToString();
            var eventType = args.Message.ApplicationProperties.TryGetValue("EventType", out var eventTypeValue)
                ? eventTypeValue?.ToString() : "Unknown";

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await ProcessEventAsync(eventType!, messageBody, args.CancellationToken);
            });

            _logger.LogDebug("Successfully processed message {MessageId}", messageId);

            // Complete the message if not using auto-complete
            if (!_options.AutoCompleteMessages)
            {
                await args.CompleteMessageAsync(args.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId} after {MaxRetries} attempts", messageId, _options.MaxRetryAttempts);

            // Dead letter the message
            await args.DeadLetterMessageAsync(args.Message, "ProcessingFailed", ex.Message);
        }
    }

    private async Task ProcessEventAsync(string eventType, string messageBody, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            // Deserialize the domain event
            var domainEvent = DeserializeEvent(eventType, messageBody);
            if (domainEvent == null)
            {
                _logger.LogWarning("Could not deserialize event of type {EventType}", eventType);
                return;
            }

            // Get the appropriate projection handler
            var projectionHandler = GetProjectionHandler(scope.ServiceProvider, eventType);
            if (projectionHandler == null)
            {
                _logger.LogDebug("No projection handler found for event type {EventType}", eventType);
                return;
            }

            // Process the projection
            await projectionHandler.HandleAsync(domainEvent, cancellationToken);

            _logger.LogDebug("Successfully processed projection for event type {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing projection for event type {EventType}", eventType);
            throw;
        }
    }

    private DomainEvent? DeserializeEvent(string eventType, string messageBody)
    {
        try
        {
            // In a real implementation, you would need a type registry to map event type names to actual types
            // For now, this is a simplified version
            var eventTypeName = $"EventSourcing.Command.Domain.Orders.{eventType}";
            var type = Type.GetType(eventTypeName);

            if (type == null)
            {
                _logger.LogWarning("Could not resolve type for event {EventType}", eventType);
                return null;
            }

            var domainEvent = JsonSerializer.Deserialize(messageBody, type) as DomainEvent;
            return domainEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize event of type {EventType}", eventType);
            return null;
        }
    }

    private object? GetProjectionHandler(IServiceProvider serviceProvider, string eventType)
    {
        try
        {
            // In a real implementation, you would have a registry of projection handlers
            // For now, we'll use the OrderProjectionHandler for order-related events
            if (eventType.StartsWith("Order"))
            {
                return serviceProvider.GetService<OrderProjectionHandler>();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projection handler for event type {EventType}", eventType);
            return null;
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "Service Bus error occurred. Source: {ErrorSource}, EntityPath: {EntityPath}",
            args.ErrorSource, args.EntityPath);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Projection Worker Service is stopping...");
        await StopMessageProcessorAsync();
        await base.StopAsync(cancellationToken);
    }
}
