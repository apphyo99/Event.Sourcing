using EventSourcing.BuildingBlocks.Infrastructure.Repositories;
using EventSourcing.Command.Domain.Orders;
using EventSourcing.BuildingBlocks.Application.EventStore;
using EventSourcing.BuildingBlocks.Application.Outbox;
using Microsoft.Extensions.Logging;
using Dapper;
using Microsoft.EntityFrameworkCore;
using EventSourcing.BuildingBlocks.Infrastructure.EventStore;

namespace EventSourcing.Command.Infrastructure.Orders;

/// <summary>
/// PostgreSQL implementation of IOrderRepository
/// </summary>
public class OrderRepository : EventSourcedRepository<Order>, IOrderRepository
{
    private readonly EventStoreDbContext _context;

    public OrderRepository(
        IEventStore eventStore,
        IOutboxRepository outboxRepository,
        EventStoreDbContext context,
        ILogger<OrderRepository> logger)
        : base(eventStore, outboxRepository, logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<string>> FindByCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID cannot be null or empty", nameof(customerId));

        const string sql = @"
            SELECT DISTINCT stream_id
            FROM event_store
            WHERE stream_type = 'Order'
            AND event_data->>'CustomerId' = @CustomerId
            ORDER BY stream_id";

        var connection = _context.Database.GetDbConnection();
        var streamIds = await connection.QueryAsync<string>(sql, new { CustomerId = customerId });

        return streamIds;
    }

    public async Task<CustomerOrderStats> GetCustomerStatsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID cannot be null or empty", nameof(customerId));

        const string sql = @"
            WITH order_streams AS (
                SELECT DISTINCT stream_id
                FROM event_store
                WHERE stream_type = 'Order'
                AND event_data->>'CustomerId' = @CustomerId
            ),
            order_events AS (
                SELECT
                    es.stream_id,
                    es.event_type,
                    es.event_data,
                    es.created_at,
                    ROW_NUMBER() OVER (PARTITION BY es.stream_id ORDER BY es.version DESC) as rn
                FROM event_store es
                INNER JOIN order_streams os ON es.stream_id = os.stream_id
            ),
            latest_order_states AS (
                SELECT
                    stream_id,
                    event_type as last_event_type,
                    event_data,
                    created_at
                FROM order_events
                WHERE rn = 1
            ),
            order_confirmed AS (
                SELECT DISTINCT stream_id
                FROM event_store
                WHERE stream_type = 'Order'
                AND event_type = 'OrderConfirmed'
                AND event_data->>'CustomerId' = @CustomerId
            ),
            order_shipped AS (
                SELECT DISTINCT stream_id
                FROM event_store
                WHERE stream_type = 'Order'
                AND event_type = 'OrderShipped'
                AND event_data->>'CustomerId' = @CustomerId
            ),
            order_cancelled AS (
                SELECT DISTINCT stream_id
                FROM event_store
                WHERE stream_type = 'Order'
                AND event_type = 'OrderCancelled'
                AND event_data->>'CustomerId' = @CustomerId
            ),
            order_totals AS (
                SELECT
                    stream_id,
                    COALESCE((event_data->>'TotalAmount')::decimal, 0) as total_amount
                FROM event_store
                WHERE stream_type = 'Order'
                AND event_type = 'OrderConfirmed'
                AND event_data->>'CustomerId' = @CustomerId
            )
            SELECT
                COUNT(DISTINCT los.stream_id) as TotalOrders,
                COUNT(DISTINCT oc.stream_id) as ConfirmedOrders,
                COUNT(DISTINCT os.stream_id) as ShippedOrders,
                COUNT(DISTINCT ocan.stream_id) as CancelledOrders,
                COALESCE(SUM(ot.total_amount), 0) as TotalOrderValue,
                MAX(los.created_at) as LastOrderDate
            FROM latest_order_states los
            LEFT JOIN order_confirmed oc ON los.stream_id = oc.stream_id
            LEFT JOIN order_shipped os ON los.stream_id = os.stream_id
            LEFT JOIN order_cancelled ocan ON los.stream_id = ocan.stream_id
            LEFT JOIN order_totals ot ON los.stream_id = ot.stream_id";

        var connection = _context.Database.GetDbConnection();
        var result = await connection.QuerySingleOrDefaultAsync(sql, new { CustomerId = customerId });

        return new CustomerOrderStats
        {
            TotalOrders = result?.TotalOrders ?? 0,
            ConfirmedOrders = result?.ConfirmedOrders ?? 0,
            ShippedOrders = result?.ShippedOrders ?? 0,
            CancelledOrders = result?.CancelledOrders ?? 0,
            TotalOrderValue = result?.TotalOrderValue ?? 0m,
            LastOrderDate = result?.LastOrderDate
        };
    }
}
