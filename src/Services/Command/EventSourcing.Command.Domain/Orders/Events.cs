using EventSourcing.BuildingBlocks.Domain.Events;

namespace EventSourcing.Command.Domain.Orders;

/// <summary>
/// Event raised when an order is created
/// </summary>
public class OrderCreated : DomainEvent
{
    public string OrderId { get; }
    public string CustomerId { get; }

    public OrderCreated(string orderId, string customerId, string correlationId, string? causationId, EventActor actor)
        : base(orderId, "Order", correlationId, causationId, actor)
    {
        OrderId = orderId;
        CustomerId = customerId;
    }
}

/// <summary>
/// Event raised when an item is added to an order
/// </summary>
public class OrderItemAdded : DomainEvent
{
    public string OrderId { get; }
    public string ProductId { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }

    public OrderItemAdded(string orderId, string productId, string productName, decimal unitPrice, int quantity,
        string correlationId, string? causationId, EventActor actor)
        : base(orderId, "Order", correlationId, causationId, actor)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}

/// <summary>
/// Event raised when an order item is updated
/// </summary>
public class OrderItemUpdated : DomainEvent
{
    public string OrderId { get; }
    public string ProductId { get; }
    public int Quantity { get; }

    public OrderItemUpdated(string orderId, string productId, int quantity,
        string correlationId, string? causationId, EventActor actor)
        : base(orderId, "Order", correlationId, causationId, actor)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
    }
}

/// <summary>
/// Event raised when an order is confirmed
/// </summary>
public class OrderConfirmed : DomainEvent
{
    public string OrderId { get; }
    public decimal TotalAmount { get; }

    public OrderConfirmed(string orderId, decimal totalAmount,
        string correlationId, string? causationId, EventActor actor)
        : base(orderId, "Order", correlationId, causationId, actor)
    {
        OrderId = orderId;
        TotalAmount = totalAmount;
    }
}

/// <summary>
/// Event raised when an order is shipped
/// </summary>
public class OrderShipped : DomainEvent
{
    public string OrderId { get; }
    public string TrackingNumber { get; }

    public OrderShipped(string orderId, string trackingNumber,
        string correlationId, string? causationId, EventActor actor)
        : base(orderId, "Order", correlationId, causationId, actor)
    {
        OrderId = orderId;
        TrackingNumber = trackingNumber;
    }
}

/// <summary>
/// Event raised when an order is cancelled
/// </summary>
public class OrderCancelled : DomainEvent
{
    public string OrderId { get; }
    public string Reason { get; }

    public OrderCancelled(string orderId, string reason,
        string correlationId, string? causationId, EventActor actor)
        : base(orderId, "Order", correlationId, causationId, actor)
    {
        OrderId = orderId;
        Reason = reason;
    }
}
