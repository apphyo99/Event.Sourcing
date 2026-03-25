using MediatR;
using Microsoft.AspNetCore.Mvc;
using EventSourcing.BuildingBlocks.Application.Common;

namespace EventSourcing.Command.Api.Controllers;

/// <summary>
/// Base controller for all API controllers
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected readonly IMediator Mediator;
    protected readonly ILogger Logger;

    protected BaseController(IMediator mediator, ILogger logger)
    {
        Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the correlation ID from the current request context
    /// </summary>
    protected string CorrelationId =>
        HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets the current user ID from the authentication claims
    /// </summary>
    protected string? UserId =>
        User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;

    /// <summary>
    /// Gets the current user name from the authentication claims
    /// </summary>
    protected string? UserName =>
        User.FindFirst("name")?.Value ?? User.Identity?.Name;

    /// <summary>
    /// Converts a Result to an appropriate HTTP response
    /// </summary>
    /// <param name="result">The result to convert</param>
    /// <returns>ActionResult representing the HTTP response</returns>
    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true });
        }

        Logger.LogWarning("Request failed with error: {Error}", result.Error);

        return result.Errors.Any() switch
        {
            true when result.Errors.Count == 1 => BadRequest(new { error = result.Error, details = result.Errors }),
            true => BadRequest(new { error = "Validation failed", details = result.Errors }),
            false => BadRequest(new { error = result.Error })
        };
    }

    /// <summary>
    /// Converts a Result with value to an appropriate HTTP response
    /// </summary>
    /// <typeparam name="T">The type of the result value</typeparam>
    /// <param name="result">The result to convert</param>
    /// <returns>ActionResult representing the HTTP response</returns>
    protected ActionResult<T> HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        Logger.LogWarning("Request failed with error: {Error}", result.Error);

        return result.Errors.Any() switch
        {
            true when result.Errors.Count == 1 => BadRequest(new { error = result.Error, details = result.Errors }),
            true => BadRequest(new { error = "Validation failed", details = result.Errors }),
            false => BadRequest(new { error = result.Error })
        };
    }

    /// <summary>
    /// Handles exceptions and converts them to appropriate HTTP responses
    /// </summary>
    /// <param name="ex">The exception to handle</param>
    /// <returns>ActionResult representing the HTTP response</returns>
    protected ActionResult HandleException(Exception ex)
    {
        Logger.LogError(ex, "Unhandled exception occurred");

        return ex switch
        {
            ArgumentException argEx => BadRequest(new { error = argEx.Message }),
            InvalidOperationException opEx => Conflict(new { error = opEx.Message }),
            UnauthorizedAccessException => Unauthorized(new { error = "Access denied" }),
            _ => StatusCode(500, new { error = "An internal server error occurred" })
        };
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="details">Additional error details</param>
    /// <returns>BadRequest ActionResult</returns>
    protected ActionResult CreateErrorResponse(string message, object? details = null)
    {
        Logger.LogWarning("Creating error response: {Message}", message);

        var response = new { error = message };

        if (details != null)
        {
            return BadRequest(new { error = message, details });
        }

        return BadRequest(response);
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    /// <param name="message">Success message</param>
    /// <param name="data">Additional data</param>
    /// <returns>Ok ActionResult</returns>
    protected ActionResult CreateSuccessResponse(string message, object? data = null)
    {
        Logger.LogInformation("Creating success response: {Message}", message);

        var response = new { success = true, message };

        if (data != null)
        {
            return Ok(new { success = true, message, data });
        }

        return Ok(response);
    }
}
