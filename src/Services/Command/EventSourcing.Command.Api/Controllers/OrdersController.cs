using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventSourcing.Command.Api.Controllers;
using EventSourcing.Command.Application.Orders.Commands;

namespace EventSourcing.Command.Api.Controllers;

/// <summary>
/// Controller for order management commands
/// </summary>
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class OrdersController : BaseController
{
    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Creates a new order
    /// </summary>
    /// <param name="request">Create order request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created order ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateOrderCommand
            {
                CustomerId = request.CustomerId,
                CorrelationId = CorrelationId,
                UserId = UserId ?? "anonymous",
                UserName = UserName ?? "Anonymous User"
            };

            var result = await Mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                var response = new CreateOrderResponse { OrderId = result.Value! };
                return Ok(response);
            }

            return HandleResult(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// Adds an item to an existing order
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="request">Add item request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{orderId}/items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> AddOrderItem(
        [FromRoute] string orderId,
        [FromBody] AddOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new AddOrderItemCommand
            {
                OrderId = orderId,
                ProductId = request.ProductId,
                ProductName = request.ProductName,
                UnitPrice = request.UnitPrice,
                Quantity = request.Quantity,
                CorrelationId = CorrelationId,
                UserId = UserId ?? "anonymous",
                UserName = UserName ?? "Anonymous User"
            };

            await Mediator.Send(command, cancellationToken);

            return CreateSuccessResponse("Item added to order successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// Confirms an order
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{orderId}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ConfirmOrder(
        [FromRoute] string orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ConfirmOrderCommand
            {
                OrderId = orderId,
                CorrelationId = CorrelationId,
                UserId = UserId ?? "anonymous",
                UserName = UserName ?? "Anonymous User"
            };

            await Mediator.Send(command, cancellationToken);

            return CreateSuccessResponse("Order confirmed successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// Cancels an order
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="request">Cancel order request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{orderId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CancelOrder(
        [FromRoute] string orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CancelOrderCommand
            {
                OrderId = orderId,
                Reason = request.Reason,
                CorrelationId = CorrelationId,
                UserId = UserId ?? "anonymous",
                UserName = UserName ?? "Anonymous User"
            };

            await Mediator.Send(command, cancellationToken);

            return CreateSuccessResponse("Order cancelled successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}

/// <summary>
/// Request model for creating an order
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// Customer identifier
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;
}

/// <summary>
/// Response model for creating an order
/// </summary>
public class CreateOrderResponse
{
    /// <summary>
    /// Created order identifier
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for adding an item to an order
/// </summary>
public class AddOrderItemRequest
{
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
}

/// <summary>
/// Request model for cancelling an order
/// </summary>
public class CancelOrderRequest
{
    /// <summary>
    /// Cancellation reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
