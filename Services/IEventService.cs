using EventTrackerApi.Models;

namespace EventTrackerApi.Services;

public interface IEventService
{
    IEnumerable<Event> GetAllEvents();
    Event? GetEventById(Guid id);
    Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt);
    Event? UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt);
    bool DeleteEvent(Guid id);
}
