using System.Collections.Concurrent;
using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;

namespace EventTrackerApi.Services;

public class EventService : IEventService
{
    private readonly ConcurrentDictionary<Guid, Event> _events = new();
    private readonly ILogger<EventService> _logger;

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<Event> GetAllEvents()
    {
        _logger.LogInformation("Getting all events. Count: {Count}", _events.Count);
        return _events.Values;
    }

    public Event? GetEventById(Guid id)
    {
        _logger.LogInformation("Getting event by id: {Id}", id);
        if (!_events.TryGetValue(id, out var ev))
        {
            _logger.LogWarning("Event with id {Id} not found", id);
            return null;
        }
        return ev;
    }

    public Event CreateEvent(CreateEventDto dto)
    {
        var ev = new Event(
            dto.Title,
            dto.Description,
            dto.StartAt,
            dto.EndAt
        );

        _events.TryAdd(ev.Id, ev);

        _logger.LogInformation("Created event with id: {Id}, title: {Title}", ev.Id, ev.Title);
        return ev;
    }

    public Event? UpdateEvent(Guid id, UpdateEventDto dto)
    {
        _logger.LogInformation("Updating event with id: {Id}", id);
        if (!_events.TryGetValue(id, out var ev))
        {
            _logger.LogWarning("Event with id {Id} not found for update", id);
            return null;
        }

        ev.Update(
            dto.Title,
            dto.Description,
            dto.StartAt,
            dto.EndAt
        );

        _logger.LogInformation("Updated event with id: {Id}", id);
        return ev;
    }

    public bool DeleteEvent(Guid id)
    {
        _logger.LogInformation("Deleting event with id: {Id}", id);
        if (!_events.TryRemove(id, out _))
        {
            _logger.LogWarning("Event with id {Id} not found for deletion", id);
            return false;
        }

        _logger.LogInformation("Deleted event with id: {Id}", id);
        return true;
    }
}
