using MediatR;

namespace EventSourcing.BuildingBlocks.Application.Queries;

/// <summary>
/// Marker interface for queries
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query</typeparam>
public interface IQuery<out TResult> : IRequest<TResult>
{
    /// <summary>
    /// Correlation ID for tracking requests across services
    /// </summary>
    string CorrelationId { get; }
}
