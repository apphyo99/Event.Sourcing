using EventSourcing.BuildingBlocks.Application.Projections;
using EventSourcing.BuildingBlocks.Domain.Events;
using EventSourcing.BuildingBlocks.Infrastructure.ReadModels;

namespace EventSourcing.ProjectionWorkers.Services;

/// <summary>
/// Projection handler for order-related events
/// </summary>
public class OrderProjectionHandler : IProjectionHandler
{
    private readonly IReadModelRepositoryFactory _repositoryFactory;
    private readonly ILogger<OrderProjectionHandler> _logger;

    public OrderProjectionHandler(
        IReadModelRepositoryFactory repositoryFactory,
        ILogger<OrderProjectionHandler> logger)
    {
        _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IEnumerable<Type> HandledEventTypes => new[]
    {
        // Note: In a real implementation, these types would be properly referenced
        // For now, this demonstrates the pattern
        typeof(DomainEvent) // Placeholder - would be actual event types like OrderCreated, OrderConfirmed, etc.
    };

    public async Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Processing projection for event {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.EventId);

            // Route to specific handler based on event type
            switch (domainEvent.GetType().Name)
            {
                case "OrderCreated":
                    await HandleOrderCreatedAsync(domainEvent, cancellationToken);
                    break;
                case "OrderItemAdded":
                    await HandleOrderItemAddedAsync(domainEvent, cancellationToken);
                    break;
                case "OrderConfirmed":
                    await HandleOrderConfirmedAsync(domainEvent, cancellationToken);
                    break;
                case "OrderShipped":
                    await HandleOrderShippedAsync(domainEvent, cancellationToken);
                    break;
                case "OrderCancelled":
                    await HandleOrderCancelledAsync(domainEvent, cancellationToken);
                    break;
                default:
                    _logger.LogDebug("No specific handler for event type {EventType}", domainEvent.GetType().Name);
                    break;
            }

            _logger.LogDebug(
                "Successfully processed projection for event {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process projection for event {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.EventId);
            throw;
        }
    }

    private async Task HandleOrderCreatedAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Create order summary read model
        var orderSummary = new OrderSummaryReadModel
        {
            Id = domainEvent.StreamId,
            OrderId = domainEvent.StreamId,
            CustomerId = ExtractProperty(domainEvent, "CustomerId"),
            Status = "Draft",
            TotalAmount = 0m,
            CreatedAt = domainEvent.OccurredAt,
            ItemCount = 0,
            LastUpdated = DateTime.UtcNow
        };

        var repository = _repositoryFactory.CreateRepository<OrderSummaryReadModel>();
        await repository.UpsertAsync(orderSummary, cancellationToken);

        _logger.LogDebug("Created order summary read model for order {OrderId}", domainEvent.StreamId);
    }

    private async Task HandleOrderItemAddedAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var repository = _repositoryFactory.CreateRepository<OrderSummaryReadModel>();
        var orderSummary = await repository.GetByIdAsync(domainEvent.StreamId, cancellationToken);

        if (orderSummary != null)
        {
            var unitPrice = decimal.Parse(ExtractProperty(domainEvent, "UnitPrice"));
            var quantity = int.Parse(ExtractProperty(domainEvent, "Quantity"));

            orderSummary.TotalAmount += unitPrice * quantity;
            orderSummary.ItemCount++;
            orderSummary.LastUpdated = DateTime.UtcNow;

            await repository.UpsertAsync(orderSummary, cancellationToken);

            _logger.LogDebug("Updated order summary for order {OrderId} - added item", domainEvent.StreamId);
        }
    }

    private async Task HandleOrderConfirmedAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpdateOrderStatusAsync(domainEvent, "Confirmed", cancellationToken);
    }

    private async Task HandleOrderShippedAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpdateOrderStatusAsync(domainEvent, "Shipped", cancellationToken);
    }

    private async Task HandleOrderCancelledAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpdateOrderStatusAsync(domainEvent, "Cancelled", cancellationToken);
    }

    private async Task UpdateOrderStatusAsync(DomainEvent domainEvent, string status, CancellationToken cancellationToken)
    {
        var repository = _repositoryFactory.CreateRepository<OrderSummaryReadModel>();
        var orderSummary = await repository.GetByIdAsync(domainEvent.StreamId, cancellationToken);

        if (orderSummary != null)
        {
            orderSummary.Status = status;
            orderSummary.LastUpdated = DateTime.UtcNow;

            await repository.UpsertAsync(orderSummary, cancellationToken);

            _logger.LogDebug("Updated order {OrderId} status to {Status}", domainEvent.StreamId, status);
        }
    }

    private static string ExtractProperty(DomainEvent domainEvent, string propertyName)
    {
        // In a real implementation, you would use proper reflection or serialization
        // to extract properties from the domain event
        // For now, this is a placeholder that would need proper implementation
        var property = domainEvent.GetType().GetProperty(propertyName);
        return property?.GetValue(domainEvent)?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Read model for order summary projections
/// </summary>
public class OrderSummaryReadModel : IReadModel
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
