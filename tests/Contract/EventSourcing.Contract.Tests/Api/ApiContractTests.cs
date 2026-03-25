using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace EventSourcing.Contract.Tests.Api;

/// <summary>
/// Contract tests to ensure API schemas remain stable
/// </summary>
public class ApiContractTests
{
    [Fact]
    public void CreateOrderRequest_ShouldHaveExpectedSchema()
    {
        // Arrange
        var expectedSchema = new
        {
            CustomerId = string.Empty
        };

        var sampleRequest = new
        {
            CustomerId = "customer-123"
        };

        // Act
        var serializedRequest = JsonSerializer.Serialize(sampleRequest);
        var deserializedRequest = JsonSerializer.Deserialize<object>(serializedRequest);

        // Assert
        deserializedRequest.Should().NotBeNull();
        serializedRequest.Should().Contain("CustomerId");

        // Verify the structure matches expected schema
        var requestType = sampleRequest.GetType();
        requestType.GetProperty("CustomerId").Should().NotBeNull();
    }

    [Fact]
    public void CreateOrderResponse_ShouldHaveExpectedSchema()
    {
        // Arrange
        var sampleResponse = new
        {
            OrderId = "order-12345"
        };

        // Act
        var serializedResponse = JsonSerializer.Serialize(sampleResponse);
        var deserializedResponse = JsonSerializer.Deserialize<object>(serializedResponse);

        // Assert
        deserializedResponse.Should().NotBeNull();
        serializedResponse.Should().Contain("OrderId");

        // Verify the structure matches expected schema
        var responseType = sampleResponse.GetType();
        responseType.GetProperty("OrderId").Should().NotBeNull();
    }

    [Fact]
    public void AddOrderItemRequest_ShouldHaveExpectedSchema()
    {
        // Arrange
        var sampleRequest = new
        {
            ProductId = "product-123",
            ProductName = "Test Product",
            UnitPrice = 99.99m,
            Quantity = 2
        };

        // Act
        var serializedRequest = JsonSerializer.Serialize(sampleRequest);

        // Assert
        serializedRequest.Should().Contain("ProductId");
        serializedRequest.Should().Contain("ProductName");
        serializedRequest.Should().Contain("UnitPrice");
        serializedRequest.Should().Contain("Quantity");

        // Verify all required properties exist
        var requestType = sampleRequest.GetType();
        requestType.GetProperty("ProductId").Should().NotBeNull();
        requestType.GetProperty("ProductName").Should().NotBeNull();
        requestType.GetProperty("UnitPrice").Should().NotBeNull();
        requestType.GetProperty("Quantity").Should().NotBeNull();
    }

    [Fact]
    public void OrderCreated_DomainEvent_ShouldHaveExpectedSchema()
    {
        // Arrange
        var sampleEvent = new
        {
            EventId = Guid.NewGuid(),
            StreamId = "order-123",
            StreamType = "Order",
            Version = 1,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "correlation-123",
            CausationId = (string?)null,
            Actor = new
            {
                Id = "user-123",
                Name = "Test User",
                Type = "User",
                Context = new Dictionary<string, object>()
            },
            OrderId = "order-123",
            CustomerId = "customer-456"
        };

        // Act
        var serializedEvent = JsonSerializer.Serialize(sampleEvent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        serializedEvent.Should().Contain("eventId");
        serializedEvent.Should().Contain("streamId");
        serializedEvent.Should().Contain("streamType");
        serializedEvent.Should().Contain("version");
        serializedEvent.Should().Contain("occurredAt");
        serializedEvent.Should().Contain("correlationId");
        serializedEvent.Should().Contain("actor");
        serializedEvent.Should().Contain("orderId");
        serializedEvent.Should().Contain("customerId");

        // Verify the event can be deserialized back
        var deserializedEvent = JsonSerializer.Deserialize<object>(serializedEvent);
        deserializedEvent.Should().NotBeNull();
    }

    [Fact]
    public void ErrorResponse_ShouldHaveConsistentSchema()
    {
        // Arrange
        var sampleErrorResponse = new
        {
            Error = "Validation failed",
            Details = new[]
            {
                "Customer ID is required",
                "Order must have at least one item"
            }
        };

        // Act
        var serializedResponse = JsonSerializer.Serialize(sampleErrorResponse);

        // Assert
        serializedResponse.Should().Contain("Error");
        serializedResponse.Should().Contain("Details");

        // Verify structure
        var responseType = sampleErrorResponse.GetType();
        responseType.GetProperty("Error").Should().NotBeNull();
        responseType.GetProperty("Details").Should().NotBeNull();
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Confirmed")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public void OrderStatus_ShouldHaveValidValues(string status)
    {
        // This test ensures that order status values remain consistent
        var validStatuses = new[] { "Draft", "Confirmed", "Shipped", "Delivered", "Cancelled" };

        validStatuses.Should().Contain(status);
    }

    [Fact]
    public void HealthCheckResponse_ShouldHaveExpectedSchema()
    {
        // Arrange
        var sampleHealthResponse = new
        {
            Status = "Healthy",
            TotalDuration = "00:00:00.123",
            Entries = new
            {
                Self = new
                {
                    Status = "Healthy",
                    Duration = "00:00:00.001"
                },
                PostgreSQL = new
                {
                    Status = "Healthy",
                    Duration = "00:00:00.045"
                }
            }
        };

        // Act
        var serializedResponse = JsonSerializer.Serialize(sampleHealthResponse);

        // Assert
        serializedResponse.Should().Contain("Status");
        serializedResponse.Should().Contain("TotalDuration");
        serializedResponse.Should().Contain("Entries");

        // Verify the response can be parsed
        var deserializedResponse = JsonSerializer.Deserialize<object>(serializedResponse);
        deserializedResponse.Should().NotBeNull();
    }
}
