using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;

namespace EventTrackerApi.Services;

public interface IEventService
{
    /// <summary>
    /// Получить все события с фильтрацией и пагинацией
    /// </summary>
    PaginatedResult<Event> GetEvents(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10);

    Event? GetEventById(Guid id);
    Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
    Event? UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt);
    bool DeleteEvent(Guid id);
}
