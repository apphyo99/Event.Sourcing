using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using EventSourcing.BuildingBlocks.Application.Commands;
using EventSourcing.BuildingBlocks.Application.Queries;

namespace EventSourcing.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior that adds structured logging with correlation tracking
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var correlationId = GetCorrelationId(request);

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting {RequestType} execution with CorrelationId: {CorrelationId}",
            requestType,
            correlationId);

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Completed {RequestType} execution in {ElapsedMilliseconds}ms with CorrelationId: {CorrelationId}",
                requestType,
                stopwatch.ElapsedMilliseconds,
                correlationId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Failed {RequestType} execution after {ElapsedMilliseconds}ms with CorrelationId: {CorrelationId}. Error: {ErrorMessage}",
                requestType,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                ex.Message);

            throw;
        }
    }

    private static string GetCorrelationId(TRequest request)
    {
        return request switch
        {
            ICommand command => command.CorrelationId,
            IQuery<TResponse> query => query.CorrelationId,
            _ => Guid.NewGuid().ToString("N")
        };
    }
}
