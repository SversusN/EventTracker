using System.ComponentModel.DataAnnotations;
using System.Net;
using EventTrackerApi.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventTrackerApi.Middleware;

/// <summary>
/// MW для глобальной обработки исключений
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Произошла непредвиденная ошибка: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var problemDetails = exception switch
        {
            ArgumentException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Ошибка валидации",
                Detail = exception.Message
            },
            ValidationException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Ошибка валидации",
                Detail = exception.Message
            },
            KeyNotFoundException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Ресурс не найден",
                Detail = exception.Message
            },
            NoAvailableSeatsException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Conflict,
                Title = "Нет свободных мест",
                Detail = exception.Message
            },
            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Внутренняя ошибка сервера",
                Detail = "Произошла внутренняя ошибка сервера"
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
