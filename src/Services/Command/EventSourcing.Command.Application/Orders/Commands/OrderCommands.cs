using EventSourcing.BuildingBlocks.Application.Commands;
using EventSourcing.BuildingBlocks.Application.Common;

namespace EventSourcing.Command.Application.Orders.Commands;

/// <summary>
/// Command to create a new order
/// </summary>
public class CreateOrderCommand : ICommand<Result<string>>
{
    /// <summary>
    /// Customer identifier
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// User context information
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User name
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Command to add an item to an order
/// </summary>
public class AddOrderItemCommand : ICommand
{
    /// <summary>
    /// Order identifier
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Product identifier
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Product name
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Unit price of the product
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Quantity to add
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// User context information
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User name
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Command to confirm an order
/// </summary>
public class ConfirmOrderCommand : ICommand
{
    /// <summary>
    /// Order identifier
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// User context information
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User name
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Command to cancel an order
/// </summary>
public class CancelOrderCommand : ICommand
{
    /// <summary>
    /// Order identifier
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Cancellation reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// User context information
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User name
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}
