using System.ComponentModel.DataAnnotations;
using System.Net;
using EventTracker.BookingsService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventTracker.BookingsService.Presentation.Middleware;

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
            BookingLimitExceededException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Conflict,
                Title = "Превышен лимит броней",
                Detail = exception.Message
            },
            ForbiddenOperationException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Доступ запрещён",
                Detail = exception.Message
            },
            InvalidOperationException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Ошибка операции",
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
