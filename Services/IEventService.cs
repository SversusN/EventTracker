using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;

namespace EventTrackerApi.Services;

public interface IEventService
{
    IEnumerable<Event> GetAllEvents();
    Event? GetEventById(Guid id);
    Event CreateEvent(CreateEventDto dto);
    Event? UpdateEvent(Guid id, UpdateEventDto dto);
    bool DeleteEvent(Guid id);
}
