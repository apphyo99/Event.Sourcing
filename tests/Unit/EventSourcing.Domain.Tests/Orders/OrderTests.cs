using EventSourcing.BuildingBlocks.Domain.Events;
using EventSourcing.Command.Domain.Orders;
using FluentAssertions;
using Xunit;

namespace EventSourcing.Domain.Tests.Orders;

/// <summary>
/// Unit tests for Order aggregate behavior
/// </summary>
public class OrderTests
{
    private readonly EventActor _testActor;

    public OrderTests()
    {
        _testActor = EventActor.User("test-user-id", "Test User");
    }

    [Fact]
    public void Create_ShouldCreateNewOrder_WithDraftStatus()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = new CustomerId("customer-123");
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var order = Order.Create(orderId, customerId, correlationId, _testActor);

        // Assert
        order.Should().NotBeNull();
        order.Id.Should().Be(orderId);
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Draft);
        order.Items.Should().BeEmpty();
        order.TotalAmount.Should().Be(0m);
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var uncommittedEvents = order.GetUncommittedEvents().ToList();
        uncommittedEvents.Should().HaveCount(1);
        uncommittedEvents.First().Should().BeOfType<OrderCreated>();

        var orderCreatedEvent = uncommittedEvents.First() as OrderCreated;
        orderCreatedEvent!.OrderId.Should().Be(orderId.Value);
        orderCreatedEvent.CustomerId.Should().Be(customerId.Value);
    }

    [Fact]
    public void AddItem_ShouldAddNewItem_WhenOrderIsDraft()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = new ProductId("product-123");
        const string productName = "Test Product";
        const decimal unitPrice = 99.99m;
        const int quantity = 2;
        var correlationId = Guid.NewGuid().ToString();

        // Act
        order.AddItem(productId, productName, unitPrice, quantity, correlationId, _testActor);

        // Assert
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(unitPrice * quantity);

        var item = order.Items.First();
        item.ProductId.Should().Be(productId);
        item.ProductName.Should().Be(productName);
        item.UnitPrice.Should().Be(unitPrice);
        item.Quantity.Should().Be(quantity);
        item.Subtotal.Should().Be(unitPrice * quantity);

        var uncommittedEvents = order.GetUncommittedEvents().ToList();
        uncommittedEvents.Should().HaveCount(2); // OrderCreated + OrderItemAdded
        uncommittedEvents.Last().Should().BeOfType<OrderItemAdded>();
    }

    [Fact]
    public void AddItem_ShouldUpdateExistingItem_WhenProductAlreadyExists()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = new ProductId("product-123");
        const string productName = "Test Product";
        const decimal unitPrice = 99.99m;
        const int initialQuantity = 2;
        const int additionalQuantity = 3;
        var correlationId = Guid.NewGuid().ToString();

        // Add initial item
        order.AddItem(productId, productName, unitPrice, initialQuantity, correlationId, _testActor);

        // Act - Add more of the same product
        order.AddItem(productId, productName, unitPrice, additionalQuantity, correlationId, _testActor);

        // Assert
        order.Items.Should().HaveCount(1); // Still only one item

        var item = order.Items.First();
        item.Quantity.Should().Be(initialQuantity + additionalQuantity);
        order.TotalAmount.Should().Be(unitPrice * (initialQuantity + additionalQuantity));

        var uncommittedEvents = order.GetUncommittedEvents().ToList();
        uncommittedEvents.Should().HaveCount(3); // OrderCreated + OrderItemAdded + OrderItemUpdated
        uncommittedEvents.Last().Should().BeOfType<OrderItemUpdated>();
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public void AddItem_ShouldThrowDomainException_WhenOrderIsNotDraft(OrderStatus status)
    {
        // Arrange
        var order = CreateTestOrderWithStatus(status);
        var productId = new ProductId("product-123");
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.AddItem(productId, "Product", 10m, 1, correlationId, _testActor);
        act.Should().Throw<DomainException>()
            .WithMessage($"Cannot add items to order in status {status}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_ShouldThrowDomainException_WhenQuantityIsInvalid(int quantity)
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = new ProductId("product-123");
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.AddItem(productId, "Product", 10m, quantity, correlationId, _testActor);
        act.Should().Throw<DomainException>()
            .WithMessage("Quantity must be greater than zero");
    }

    [Fact]
    public void AddItem_ShouldThrowDomainException_WhenUnitPriceIsNegative()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = new ProductId("product-123");
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.AddItem(productId, "Product", -10m, 1, correlationId, _testActor);
        act.Should().Throw<DomainException>()
            .WithMessage("Unit price cannot be negative");
    }

    [Fact]
    public void Confirm_ShouldConfirmOrder_WhenOrderHasItems()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        var correlationId = Guid.NewGuid().ToString();

        // Act
        order.Confirm(correlationId, _testActor);

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var uncommittedEvents = order.GetUncommittedEvents().ToList();
        uncommittedEvents.Last().Should().BeOfType<OrderConfirmed>();

        var confirmEvent = uncommittedEvents.Last() as OrderConfirmed;
        confirmEvent!.TotalAmount.Should().Be(order.TotalAmount);
    }

    [Fact]
    public void Confirm_ShouldThrowDomainException_WhenOrderHasNoItems()
    {
        // Arrange
        var order = CreateTestOrder();
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.Confirm(correlationId, _testActor);
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot confirm order with no items");
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public void Confirm_ShouldThrowDomainException_WhenOrderIsNotDraft(OrderStatus status)
    {
        // Arrange
        var order = CreateTestOrderWithStatus(status);
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.Confirm(correlationId, _testActor);
        act.Should().Throw<DomainException>()
            .WithMessage($"Cannot confirm order in status {status}");
    }

    [Fact]
    public void Ship_ShouldShipOrder_WhenOrderIsConfirmed()
    {
        // Arrange
        var order = CreateConfirmedOrder();
        const string trackingNumber = "TRACK123";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        order.Ship(trackingNumber, correlationId, _testActor);

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var uncommittedEvents = order.GetUncommittedEvents().ToList();
        uncommittedEvents.Last().Should().BeOfType<OrderShipped>();

        var shipEvent = uncommittedEvents.Last() as OrderShipped;
        shipEvent!.TrackingNumber.Should().Be(trackingNumber);
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public void Ship_ShouldThrowDomainException_WhenOrderIsNotConfirmed(OrderStatus status)
    {
        // Arrange
        var order = CreateTestOrderWithStatus(status);
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.Ship("TRACK123", correlationId, _testActor);
        act.Should().Throw<DomainException>()
            .WithMessage($"Cannot ship order in status {status}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Ship_ShouldThrowArgumentException_WhenTrackingNumberIsInvalid(string? trackingNumber)
    {
        // Arrange
        var order = CreateConfirmedOrder();
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.Ship(trackingNumber!, correlationId, _testActor);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Tracking number is required*");
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Confirmed)]
    public void Cancel_ShouldCancelOrder_WhenOrderIsNotShipped(OrderStatus status)
    {
        // Arrange
        var order = CreateTestOrderWithStatus(status);
        const string reason = "Customer requested cancellation";
        var correlationId = Guid.NewGuid().ToString();

        // Act
        order.Cancel(reason, correlationId, _testActor);

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);

        var uncommittedEvents = order.GetUncommittedEvents().ToList();
        uncommittedEvents.Last().Should().BeOfType<OrderCancelled>();

        var cancelEvent = uncommittedEvents.Last() as OrderCancelled;
        cancelEvent!.Reason.Should().Be(reason);
    }

    [Fact]
    public void Cancel_ShouldThrowDomainException_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateTestOrderWithStatus(OrderStatus.Shipped);
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.Cancel("Reason", correlationId, _testActor);
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot cancel shipped order");
    }

    [Fact]
    public void Cancel_ShouldThrowDomainException_WhenOrderIsAlreadyCancelled()
    {
        // Arrange
        var order = CreateTestOrderWithStatus(OrderStatus.Cancelled);
        var correlationId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => order.Cancel("Reason", correlationId, _testActor);
        act.Should().Throw<DomainException>()
            .WithMessage("Order is already cancelled");
    }

    #region Test Helpers

    private Order CreateTestOrder()
    {
        var orderId = OrderId.New();
        var customerId = new CustomerId("customer-123");
        var correlationId = Guid.NewGuid().ToString();

        return Order.Create(orderId, customerId, correlationId, _testActor);
    }

    private Order CreateTestOrderWithItems()
    {
        var order = CreateTestOrder();
        var correlationId = Guid.NewGuid().ToString();

        order.AddItem(
            new ProductId("product-1"),
            "Product 1",
            99.99m,
            2,
            correlationId,
            _testActor);

        return order;
    }

    private Order CreateConfirmedOrder()
    {
        var order = CreateTestOrderWithItems();
        var correlationId = Guid.NewGuid().ToString();

        order.Confirm(correlationId, _testActor);
        return order;
    }

    private Order CreateTestOrderWithStatus(OrderStatus targetStatus)
    {
        var order = CreateTestOrderWithItems();
        var correlationId = Guid.NewGuid().ToString();

        switch (targetStatus)
        {
            case OrderStatus.Draft:
                return order;

            case OrderStatus.Confirmed:
                order.Confirm(correlationId, _testActor);
                return order;

            case OrderStatus.Shipped:
                order.Confirm(correlationId, _testActor);
                order.Ship("TRACK123", correlationId, _testActor);
                return order;

            case OrderStatus.Cancelled:
                order.Cancel("Test cancellation", correlationId, _testActor);
                return order;

            default:
                throw new ArgumentOutOfRangeException(nameof(targetStatus), targetStatus, null);
        }
    }

    #endregion
}
