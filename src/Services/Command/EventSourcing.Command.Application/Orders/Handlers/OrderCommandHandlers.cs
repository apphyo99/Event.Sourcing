using EventSourcing.BuildingBlocks.Application.Commands;
using EventSourcing.BuildingBlocks.Application.Common;
using EventSourcing.BuildingBlocks.Domain.Events;
using EventSourcing.Command.Application.Orders.Commands;
using EventSourcing.Command.Domain.Orders;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Command.Application.Orders.Handlers;

/// <summary>
/// Handler for CreateOrderCommand
/// </summary>
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Result<string>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<string>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating order for customer {CustomerId} with correlation ID {CorrelationId}",
                request.CustomerId, request.CorrelationId);

            var orderId = OrderId.New();
            var customerId = new CustomerId(request.CustomerId);
            var actor = EventActor.User(request.UserId, request.UserName);

            var order = Order.Create(orderId, customerId, request.CorrelationId, actor);

            await _orderRepository.SaveAsync(order, null, cancellationToken);

            _logger.LogInformation(
                "Successfully created order {OrderId} for customer {CustomerId}",
                orderId.Value, request.CustomerId);

            return Result<string>.Success(orderId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create order for customer {CustomerId} with correlation ID {CorrelationId}",
                request.CustomerId, request.CorrelationId);

            return Result<string>.Failure($"Failed to create order: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for AddOrderItemCommand
/// </summary>
public class AddOrderItemCommandHandler : ICommandHandler<AddOrderItemCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<AddOrderItemCommandHandler> _logger;

    public AddOrderItemCommandHandler(
        IOrderRepository orderRepository,
        ILogger<AddOrderItemCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adding item {ProductId} to order {OrderId} with correlation ID {CorrelationId}",
            request.ProductId, request.OrderId, request.CorrelationId);

        var order = await _orderRepository.LoadAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order with ID {request.OrderId} not found");
        }

        var productId = new ProductId(request.ProductId);
        var actor = EventActor.User(request.UserId, request.UserName);

        order.AddItem(
            productId,
            request.ProductName,
            request.UnitPrice,
            request.Quantity,
            request.CorrelationId,
            actor);

        await _orderRepository.SaveAsync(order, null, cancellationToken);

        _logger.LogInformation(
            "Successfully added item {ProductId} to order {OrderId}",
            request.ProductId, request.OrderId);
    }
}

/// <summary>
/// Handler for ConfirmOrderCommand
/// </summary>
public class ConfirmOrderCommandHandler : ICommandHandler<ConfirmOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<ConfirmOrderCommandHandler> _logger;

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        ILogger<ConfirmOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Confirming order {OrderId} with correlation ID {CorrelationId}",
            request.OrderId, request.CorrelationId);

        var order = await _orderRepository.LoadAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order with ID {request.OrderId} not found");
        }

        var actor = EventActor.User(request.UserId, request.UserName);

        order.Confirm(request.CorrelationId, actor);

        await _orderRepository.SaveAsync(order, null, cancellationToken);

        _logger.LogInformation("Successfully confirmed order {OrderId}", request.OrderId);
    }
}

/// <summary>
/// Handler for CancelOrderCommand
/// </summary>
public class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Cancelling order {OrderId} with reason '{Reason}' and correlation ID {CorrelationId}",
            request.OrderId, request.Reason, request.CorrelationId);

        var order = await _orderRepository.LoadAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order with ID {request.OrderId} not found");
        }

        var actor = EventActor.User(request.UserId, request.UserName);

        order.Cancel(request.Reason, request.CorrelationId, actor);

        await _orderRepository.SaveAsync(order, null, cancellationToken);

        _logger.LogInformation("Successfully cancelled order {OrderId}", request.OrderId);
    }
}
