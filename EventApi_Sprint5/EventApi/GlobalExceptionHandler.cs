using EventApi.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventApi;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            NoAvailableSeatsException => (StatusCodes.Status409Conflict, "No Available Seats"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        ProblemDetails problemDetails = exception is ValidationException validationEx
            ? new ValidationProblemDetails(validationEx.Errors.ToDictionary(k => k.Key, v => v.Value.ToArray()))
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            }
            : new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
