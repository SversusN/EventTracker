using Microsoft.AspNetCore.Mvc;

namespace EventTrackerApi.Infrastructure;

/// <summary>
/// Хелпер для создания ProblemDetails
/// </summary>
public static class ProblemDetailsHelper
{
    /// <summary>
    /// Создает PD
    /// </summary>
    public static ProblemDetails Create(int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
    }

    /// <summary>
    /// PD 404 Not Found
    /// </summary>
    public static ProblemDetails NotFound(string resourceName, object id) =>
        Create(404, $"{resourceName} не найдено", $"{resourceName} с идентификатором '{id}' не найдено.");

    /// <summary>
    /// PD 400 Bad Request
    /// </summary>
    public static ProblemDetails BadRequest(string title, string detail) =>
        Create(400, title, detail);

    /// <summary>
    /// PD некорректного номера страницы
    /// </summary>
    public static ProblemDetails InvalidPageNumber() =>
        BadRequest("Некорректный номер страницы", "Параметр станицы должен быть >= 1.");

    /// <summary>
    /// PD некорректного размера страницы
    /// </summary>
    public static ProblemDetails InvalidPageSize() =>
        BadRequest("Некорректный размер страницы", "Параметр станицы должен быть >= 1.");
}
