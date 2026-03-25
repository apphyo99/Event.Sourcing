using MediatR;

namespace EventSourcing.BuildingBlocks.Application.Commands;

/// <summary>
/// Marker interface for commands
/// </summary>
public interface ICommand : IRequest
{
    /// <summary>
    /// Correlation ID for tracking requests across services
    /// </summary>
    string CorrelationId { get; }
}

/// <summary>
/// Marker interface for commands that return a result
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command</typeparam>
public interface ICommand<out TResult> : IRequest<TResult>
{
    /// <summary>
    /// Correlation ID for tracking requests across services
    /// </summary>
    string CorrelationId { get; }
}
