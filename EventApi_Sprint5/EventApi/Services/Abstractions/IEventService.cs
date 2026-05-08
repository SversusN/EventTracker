using EventApi.Dto;

namespace EventApi.Services.Abstractions;

internal interface IEventService
{
    Task<PaginatedResult<EventInfo>> GetAllEventsAsync(int page = 1, int pageSize = 10, DateTime? from = null, DateTime? to = null, string? title = null, CancellationToken cancellationToken = default);
    Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventInfo> CreateEventAsync(CreateEvent createEvent, CancellationToken cancellationToken = default);
    Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent updateEvent, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
}
