using EventSourcing.BuildingBlocks.Domain.Abstractions;

namespace EventSourcing.Command.Domain.Orders;

/// <summary>
/// Order identifier value object
/// </summary>
public class OrderId : ValueObject
{
    public string Value { get; }

    public OrderId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(value));

        Value = value;
    }

    public static OrderId New() => new($"order-{Guid.NewGuid():N}");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(OrderId orderId) => orderId.Value;
}

/// <summary>
/// Customer identifier value object
/// </summary>
public class CustomerId : ValueObject
{
    public string Value { get; }

    public CustomerId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Customer ID cannot be null or empty", nameof(value));

        Value = value;
    }

    public static CustomerId New() => new($"customer-{Guid.NewGuid():N}");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(CustomerId customerId) => customerId.Value;
}

/// <summary>
/// Product identifier value object
/// </summary>
public class ProductId : ValueObject
{
    public string Value { get; }

    public ProductId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(value));

        Value = value;
    }

    public static ProductId New() => new($"product-{Guid.NewGuid():N}");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(ProductId productId) => productId.Value;
}

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

/// <summary>
/// Order item entity
/// </summary>
public class OrderItem : ValueObject
{
    public ProductId ProductId { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }
    public decimal Subtotal => UnitPrice * Quantity;

    public OrderItem(ProductId productId, string productName, decimal unitPrice, int quantity)
    {
        ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public OrderItem WithQuantity(int newQuantity)
    {
        return new OrderItem(ProductId, ProductName, UnitPrice, newQuantity);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ProductId;
        yield return ProductName;
        yield return UnitPrice;
        yield return Quantity;
    }
}
