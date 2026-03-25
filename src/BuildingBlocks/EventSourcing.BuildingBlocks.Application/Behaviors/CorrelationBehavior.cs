using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using EventSourcing.BuildingBlocks.Application.Commands;
using EventSourcing.BuildingBlocks.Application.Queries;

namespace EventSourcing.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior that manages correlation context for request tracing
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public class CorrelationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<CorrelationBehavior<TRequest, TResponse>> _logger;

    public CorrelationBehavior(ILogger<CorrelationBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var correlationId = GetCorrelationId(request);

        // Set correlation ID in the current activity for distributed tracing
        using var activity = Activity.Current ?? new Activity("MediatR.Handler");
        activity.SetTag("CorrelationId", correlationId);
        activity.SetTag("RequestType", typeof(TRequest).Name);

        // Add to logging scope
        using var scope = _logger.BeginScope("CorrelationId: {CorrelationId}", correlationId);

        return await next();
    }

    private static string GetCorrelationId(TRequest request)
    {
        return request switch
        {
            ICommand command => command.CorrelationId,
            IQuery<TResponse> query => query.CorrelationId,
            _ => Activity.Current?.Id ?? Guid.NewGuid().ToString("N")
        };
    }
}
