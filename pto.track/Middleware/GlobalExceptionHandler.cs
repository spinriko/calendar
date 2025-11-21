using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using pto.track.services.Exceptions;
using System.Net;

namespace pto.track.Middleware;

/// <summary>
/// Global exception handler that converts exceptions to appropriate HTTP responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            AbsenceNotFoundException ex => (
                HttpStatusCode.NotFound,
                "Absence Not Found",
                $"Absence request with ID '{ex.AbsenceId}' was not found."
            ),
            EventNotFoundException ex => (
                HttpStatusCode.NotFound,
                "Event Not Found",
                $"Event with ID '{ex.EventId}' was not found."
            ),
            ResourceNotFoundException ex => (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                $"Resource with ID '{ex.ResourceId}' was not found."
            ),
            InvalidAbsenceOperationException ex => (
                HttpStatusCode.BadRequest,
                "Invalid Operation",
                ex.Message
            ),
            UnauthorizedAbsenceAccessException ex => (
                HttpStatusCode.Forbidden,
                "Unauthorized Access",
                ex.Message
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later."
            )
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = (int)statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
