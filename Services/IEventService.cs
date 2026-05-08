using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;

namespace EventTrackerApi.Services;

public interface IEventService
{
    /// <summary>
    /// Получить все события с фильтрацией и пагинацией
    /// </summary>
    Task<PaginatedResult<Event>> GetEventsAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10);

    Task<Event?> GetEventByIdAsync(Guid id);
    Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
    Task<Event?> UpdateEventAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt);
    Task<bool> DeleteEventAsync(Guid id);
}
