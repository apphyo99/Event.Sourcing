using EventSourcing.BuildingBlocks.Domain.Aggregates;

namespace EventSourcing.Command.Domain.Orders;

/// <summary>
/// Order aggregate root - manages order lifecycle and business rules
/// </summary>
public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();

    /// <summary>
    /// Order identifier
    /// </summary>
    public OrderId Id { get; private set; } = null!;

    /// <summary>
    /// Customer who placed the order
    /// </summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>
    /// Current status of the order
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Items in the order
    /// </summary>
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Total amount of the order
    /// </summary>
    public decimal TotalAmount => _items.Sum(item => item.Subtotal);

    /// <summary>
    /// When the order was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the order was confirmed
    /// </summary>
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>
    /// When the order was shipped
    /// </summary>
    public DateTime? ShippedAt { get; private set; }

    /// <summary>
    /// Stream identifier for event sourcing
    /// </summary>
    public override string StreamId => Id.Value;

    /// <summary>
    /// Stream type for event sourcing
    /// </summary>
    public override string StreamType => "Order";

    /// <summary>
    /// Returns order ID for entity comparison
    /// </summary>
    public override object GetId() => Id;

    /// <summary>
    /// Private constructor for event sourcing reconstruction
    /// </summary>
    private Order() { }

    /// <summary>
    /// Creates a new order
    /// </summary>
    public static Order Create(OrderId orderId, CustomerId customerId, string correlationId, EventActor actor)
    {
        var order = new Order();
        var orderCreated = new OrderCreated(orderId.Value, customerId.Value, correlationId, null, actor);
        order.ApplyEvent(orderCreated);
        return order;
    }

    /// <summary>
    /// Adds an item to the order
    /// </summary>
    public void AddItem(ProductId productId, string productName, decimal unitPrice, int quantity, string correlationId, EventActor actor)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException($"Cannot add items to order in status {Status}");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        if (unitPrice < 0)
            throw new DomainException("Unit price cannot be negative");

        // Check if item already exists
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            // Update existing item
            var itemUpdated = new OrderItemUpdated(
                Id.Value, productId.Value, existingItem.Quantity + quantity,
                correlationId, null, actor);
            ApplyEvent(itemUpdated);
        }
        else
        {
            // Add new item
            var itemAdded = new OrderItemAdded(
                Id.Value, productId.Value, productName, unitPrice, quantity,
                correlationId, null, actor);
            ApplyEvent(itemAdded);
        }
    }

    /// <summary>
    /// Confirms the order
    /// </summary>
    public void Confirm(string correlationId, EventActor actor)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException($"Cannot confirm order in status {Status}");

        if (!_items.Any())
            throw new DomainException("Cannot confirm order with no items");

        var orderConfirmed = new OrderConfirmed(Id.Value, TotalAmount, correlationId, null, actor);
        ApplyEvent(orderConfirmed);
    }

    /// <summary>
    /// Ships the order
    /// </summary>
    public void Ship(string trackingNumber, string correlationId, EventActor actor)
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException($"Cannot ship order in status {Status}");

        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("Tracking number is required", nameof(trackingNumber));

        var orderShipped = new OrderShipped(Id.Value, trackingNumber, correlationId, null, actor);
        ApplyEvent(orderShipped);
    }

    /// <summary>
    /// Cancels the order
    /// </summary>
    public void Cancel(string reason, string correlationId, EventActor actor)
    {
        if (Status == OrderStatus.Shipped)
            throw new DomainException("Cannot cancel shipped order");

        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled");

        var orderCancelled = new OrderCancelled(Id.Value, reason, correlationId, null, actor);
        ApplyEvent(orderCancelled);
    }

    /// <summary>
    /// Applies events to rebuild aggregate state
    /// </summary>
    protected override void ApplyEventInternal(DomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case OrderCreated orderCreated:
                Apply(orderCreated);
                break;
            case OrderItemAdded itemAdded:
                Apply(itemAdded);
                break;
            case OrderItemUpdated itemUpdated:
                Apply(itemUpdated);
                break;
            case OrderConfirmed orderConfirmed:
                Apply(orderConfirmed);
                break;
            case OrderShipped orderShipped:
                Apply(orderShipped);
                break;
            case OrderCancelled orderCancelled:
                Apply(orderCancelled);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {domainEvent.GetType().Name}");
        }
    }

    private void Apply(OrderCreated orderCreated)
    {
        Id = new OrderId(orderCreated.OrderId);
        CustomerId = new CustomerId(orderCreated.CustomerId);
        Status = OrderStatus.Draft;
        CreatedAt = orderCreated.OccurredAt;
    }

    private void Apply(OrderItemAdded itemAdded)
    {
        var item = new OrderItem(
            new ProductId(itemAdded.ProductId),
            itemAdded.ProductName,
            itemAdded.UnitPrice,
            itemAdded.Quantity);
        _items.Add(item);
    }

    private void Apply(OrderItemUpdated itemUpdated)
    {
        var existingItem = _items.First(i => i.ProductId.Value == itemUpdated.ProductId);
        var updatedItem = existingItem.WithQuantity(itemUpdated.Quantity);
        _items.Remove(existingItem);
        _items.Add(updatedItem);
    }

    private void Apply(OrderConfirmed orderConfirmed)
    {
        Status = OrderStatus.Confirmed;
        ConfirmedAt = orderConfirmed.OccurredAt;
    }

    private void Apply(OrderShipped orderShipped)
    {
        Status = OrderStatus.Shipped;
        ShippedAt = orderShipped.OccurredAt;
    }

    private void Apply(OrderCancelled orderCancelled)
    {
        Status = OrderStatus.Cancelled;
    }

    /// <summary>
    /// Validates business invariants
    /// </summary>
    protected override void ValidateInvariants()
    {
        if (Status == OrderStatus.Confirmed && !_items.Any())
            throw new DomainException("Confirmed order must have items");

        if (_items.Any(item => item.Quantity <= 0))
            throw new DomainException("All order items must have positive quantity");
    }
}
