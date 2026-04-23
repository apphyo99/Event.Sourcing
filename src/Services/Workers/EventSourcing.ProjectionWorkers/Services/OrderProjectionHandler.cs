using EventSourcing.BuildingBlocks.Application.Projections;
using EventSourcing.BuildingBlocks.Domain.Events;
using EventSourcing.BuildingBlocks.Infrastructure.ReadModels;
using EventSourcing.Command.Domain.Orders;

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

    /// <inheritdoc />
    public IEnumerable<Type> HandledEventTypes =>
    [
        typeof(OrderCreated),
        typeof(OrderItemAdded),
        typeof(OrderConfirmed),
        typeof(OrderShipped),
        typeof(OrderCancelled)
    ];

    public async Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Processing projection for event {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.EventId);

            switch (domainEvent)
            {
                case OrderCreated created:
                    await HandleOrderCreatedAsync(created, cancellationToken);
                    break;
                case OrderItemAdded added:
                    await HandleOrderItemAddedAsync(added, cancellationToken);
                    break;
                case OrderConfirmed confirmed:
                    await HandleOrderConfirmedAsync(confirmed, cancellationToken);
                    break;
                case OrderShipped shipped:
                    await HandleOrderShippedAsync(shipped, cancellationToken);
                    break;
                case OrderCancelled cancelled:
                    await HandleOrderCancelledAsync(cancelled, cancellationToken);
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

    private async Task HandleOrderCreatedAsync(OrderCreated e, CancellationToken cancellationToken)
    {
        var orderSummary = new OrderSummaryReadModel
        {
            Id = e.StreamId,
            OrderId = e.StreamId,
            CustomerId = e.CustomerId.Trim(),
            Status = "Draft",
            TotalAmount = 0m,
            CreatedAt = e.OccurredAt,
            ItemCount = 0,
            LastUpdated = DateTime.UtcNow
        };

        var repository = _repositoryFactory.CreateRepository<OrderSummaryReadModel>();
        await repository.UpsertAsync(orderSummary, cancellationToken);

        _logger.LogDebug("Created order summary read model for order {OrderId}", e.StreamId);
    }

    private async Task HandleOrderItemAddedAsync(OrderItemAdded e, CancellationToken cancellationToken)
    {
        var repository = _repositoryFactory.CreateRepository<OrderSummaryReadModel>();
        var orderSummary = await repository.GetByIdAsync(e.StreamId, cancellationToken);

        if (orderSummary != null)
        {
            orderSummary.TotalAmount += e.UnitPrice * e.Quantity;
            orderSummary.ItemCount++;
            orderSummary.LastUpdated = DateTime.UtcNow;

            await repository.UpsertAsync(orderSummary, cancellationToken);

            _logger.LogDebug("Updated order summary for order {OrderId} - added item", e.StreamId);
        }
    }

    private async Task HandleOrderConfirmedAsync(OrderConfirmed e, CancellationToken cancellationToken)
    {
        await UpdateOrderStatusAsync(e.StreamId, "Confirmed", cancellationToken);
    }

    private async Task HandleOrderShippedAsync(OrderShipped e, CancellationToken cancellationToken)
    {
        await UpdateOrderStatusAsync(e.StreamId, "Shipped", cancellationToken);
    }

    private async Task HandleOrderCancelledAsync(OrderCancelled e, CancellationToken cancellationToken)
    {
        await UpdateOrderStatusAsync(e.StreamId, "Cancelled", cancellationToken);
    }

    private async Task UpdateOrderStatusAsync(string streamId, string status, CancellationToken cancellationToken)
    {
        var repository = _repositoryFactory.CreateRepository<OrderSummaryReadModel>();
        var orderSummary = await repository.GetByIdAsync(streamId, cancellationToken);

        if (orderSummary != null)
        {
            orderSummary.Status = status;
            orderSummary.LastUpdated = DateTime.UtcNow;

            await repository.UpsertAsync(orderSummary, cancellationToken);

            _logger.LogDebug("Updated order {OrderId} status to {Status}", streamId, status);
        }
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
