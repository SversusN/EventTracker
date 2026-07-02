using Microsoft.AspNetCore.Mvc;

namespace EventTracker.BookingsService.Presentation.Infrastructure;

public static class ProblemDetailsHelper
{
    public static ProblemDetails Create(int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
    }

    public static ProblemDetails NotFound(string resourceName, object id) =>
        Create(404, $"{resourceName} не найдено", $"{resourceName} с идентификатором '{id}' не найдено.");

    public static ProblemDetails BadRequest(string title, string detail) =>
        Create(400, title, detail);
}
