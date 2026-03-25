using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventSourcing.Integration.Tests.Api;

/// <summary>
/// Integration tests for the Command API using TestContainers
/// </summary>
public class OrdersControllerIntegrationTests : IClassFixture<OrdersControllerIntegrationTests.TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly PostgreSqlContainer _postgresContainer;

    public OrdersControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("eventsourcing_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Start the PostgreSQL container
        await _postgresContainer.StartAsync();

        // Update the factory with the container connection string
        _factory.SetConnectionString(_postgresContainer.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnCreatedOrder_WhenValidRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "customer-123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateOrderResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.OrderId.Should().NotBeNullOrEmpty();
        result.OrderId.Should().StartWith("order-");
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenCustomerIdIsEmpty()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddOrderItem_ShouldSucceed_WhenValidRequest()
    {
        // Arrange - First create an order
        var createRequest = new CreateOrderRequest
        {
            CustomerId = "customer-123"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createRequest);
        createResponse.Should().BeSuccessful();

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<CreateOrderResponse>(createContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var orderId = createResult!.OrderId;

        var addItemRequest = new AddOrderItemRequest
        {
            ProductId = "product-123",
            ProductName = "Test Product",
            UnitPrice = 99.99m,
            Quantity = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/items", addItemRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddOrderItem_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = "order-nonexistent";
        var request = new AddOrderItemRequest
        {
            ProductId = "product-123",
            ProductName = "Test Product",
            UnitPrice = 99.99m,
            Quantity = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("", "Product", 10.99, 1)] // Empty ProductId
    [InlineData("product-1", "", 10.99, 1)] // Empty ProductName
    [InlineData("product-1", "Product", -10.99, 1)] // Negative price
    [InlineData("product-1", "Product", 10.99, 0)] // Zero quantity
    public async Task AddOrderItem_ShouldReturnBadRequest_WhenRequestIsInvalid(
        string productId, string productName, decimal unitPrice, int quantity)
    {
        // Arrange - First create an order
        var createRequest = new CreateOrderRequest
        {
            CustomerId = "customer-123"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<CreateOrderResponse>(createContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var orderId = createResult!.OrderId;

        var request = new AddOrderItemRequest
        {
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmOrder_ShouldSucceed_WhenOrderHasItems()
    {
        // Arrange - Create order and add item
        var orderId = await CreateOrderWithItemAsync();

        // Act
        var response = await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfirmOrder_ShouldReturnBadRequest_WhenOrderHasNoItems()
    {
        // Arrange - Create empty order
        var createRequest = new CreateOrderRequest
        {
            CustomerId = "customer-123"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<CreateOrderResponse>(createContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var orderId = createResult!.OrderId;

        // Act
        var response = await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelOrder_ShouldSucceed_WhenValidRequest()
    {
        // Arrange - Create order
        var orderId = await CreateOrderWithItemAsync();

        var request = new CancelOrderRequest
        {
            Reason = "Customer requested cancellation"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CancelOrder_ShouldReturnBadRequest_WhenReasonIsEmpty(string reason)
    {
        // Arrange
        var orderId = await CreateOrderWithItemAsync();

        var request = new CancelOrderRequest
        {
            Reason = reason
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #region Health Checks

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    #endregion

    #region Helper Methods

    private async Task<string> CreateOrderWithItemAsync()
    {
        // Create order
        var createRequest = new CreateOrderRequest
        {
            CustomerId = "customer-123"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<CreateOrderResponse>(createContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var orderId = createResult!.OrderId;

        // Add item
        var addItemRequest = new AddOrderItemRequest
        {
            ProductId = "product-123",
            ProductName = "Test Product",
            UnitPrice = 99.99m,
            Quantity = 1
        };

        await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/items", addItemRequest);

        return orderId;
    }

    #endregion

    #region Test Factory

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private string? _connectionString;

        public void SetConnectionString(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Override configuration with test connection string
                if (!string.IsNullOrEmpty(_connectionString))
                {
                    // In a real implementation, you would properly override the connection string
                    // For demo purposes, this shows the pattern
                }

                // Add test-specific services
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
            });

            builder.UseEnvironment("Testing");
        }
    }

    #endregion
}

#region Request/Response Models

public class CreateOrderRequest
{
    public string CustomerId { get; set; } = string.Empty;
}

public class CreateOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
}

public class AddOrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

#endregion
