using MediatR;

namespace EventSourcing.BuildingBlocks.Application.Queries;

/// <summary>
/// Handler for queries that return a result
/// </summary>
/// <typeparam name="TQuery">The type of query to handle</typeparam>
/// <typeparam name="TResult">The type of result returned by the query</typeparam>
public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
}
