using FluentValidation;
using EventSourcing.Command.Application.Orders.Commands;

namespace EventSourcing.Command.Application.Orders.Validators;

/// <summary>
/// Validator for CreateOrderCommand
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .WithMessage("Correlation ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("User name is required");
    }
}

/// <summary>
/// Validator for AddOrderItemCommand
/// </summary>
public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Unit price must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Unit price cannot exceed $1,000,000");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity cannot exceed 1000");

        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .WithMessage("Correlation ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("User name is required");
    }
}

/// <summary>
/// Validator for ConfirmOrderCommand
/// </summary>
public class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    public ConfirmOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .WithMessage("Correlation ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("User name is required");
    }
}

/// <summary>
/// Validator for CancelOrderCommand
/// </summary>
public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");

        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .WithMessage("Correlation ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("User name is required");
    }
}
