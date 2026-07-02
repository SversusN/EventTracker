using EventTracker.EventsService.Domain.Models;
using EventTracker.EventsService.Application.DTOs;

namespace EventTracker.EventsService.Application.Ports;

public interface IEventRepository
{
    Task<PaginatedResult<Event>> GetEventsAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10);
    Task<Event?> GetByIdAsync(Guid id);
    Task AddAsync(Event ev);
    void SetValues(Event target, Event source);
    void Remove(Event ev);
    Task SaveChangesAsync();
}
