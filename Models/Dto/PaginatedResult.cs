namespace EventTrackerApi.Models.Dto;

/// <summary>
/// Результат пагинации
/// </summary>
/// <typeparam name="T">Тип элементов</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Общее количество элементов
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Элементы текущей страницы
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Номер текущей страницы
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Общее количество страниц
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
