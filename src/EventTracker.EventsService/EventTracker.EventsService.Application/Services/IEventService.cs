using EventTracker.EventsService.Application.DTOs;
using EventTracker.EventsService.Domain.Models;

namespace EventTracker.EventsService.Application.Services;

public interface IEventService
{
    Task<PaginatedResult<Event>> GetEventsAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10);

    Task<EventResponseDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventResponseDto>> GetTopEventsAsync(int count, CancellationToken cancellationToken = default);

    Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);

    Task<Event?> UpdateEventAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt);

    Task<bool> DeleteEventAsync(Guid id);
}
