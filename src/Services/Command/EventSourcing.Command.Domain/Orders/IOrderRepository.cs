using EventSourcing.BuildingBlocks.Domain.Repositories;

namespace EventSourcing.Command.Domain.Orders;

/// <summary>
/// Specific repository interface for Order aggregates
/// </summary>
public interface IOrderRepository : IAggregateRepository<Order>
{
    /// <summary>
    /// Finds orders by customer ID
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of order stream identifiers</returns>
    Task<IEnumerable<string>> FindByCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets order statistics for a customer
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order statistics</returns>
    Task<CustomerOrderStats> GetCustomerStatsAsync(string customerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Customer order statistics
/// </summary>
public class CustomerOrderStats
{
    public int TotalOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalOrderValue { get; set; }
    public DateTime? LastOrderDate { get; set; }
}
