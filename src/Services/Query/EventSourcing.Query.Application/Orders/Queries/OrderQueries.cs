using EventSourcing.BuildingBlocks.Application.Queries;
using EventSourcing.BuildingBlocks.Application.Common;

namespace EventSourcing.Query.Application.Orders.Queries;

/// <summary>
/// Query to get order details by ID
/// </summary>
public class GetOrderByIdQuery : IQuery<Result<OrderDetailDto?>>
{
    /// <summary>
    /// Order identifier
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Query to get orders for a customer
/// </summary>
public class GetOrdersByCustomerQuery : IQuery<Result<PagedResult<OrderSummaryDto>>>
{
    /// <summary>
    /// Customer identifier
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Order detail data transfer object
/// </summary>
public class OrderDetailDto
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public IEnumerable<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();
}

/// <summary>
/// Order summary data transfer object
/// </summary>
public class OrderSummaryDto
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Order item data transfer object
/// </summary>
public class OrderItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}
