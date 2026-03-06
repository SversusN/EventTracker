using System.Collections.Concurrent;
using EventTrackerApi.Infrastructure.Mappers;
using EventTrackerApi.Models;

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

    public Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt)
    {
        var ev = EventMapper.FromCreateDto(title, description, startAt, endAt);

        _events.TryAdd(ev.Id, ev);

        _logger.LogInformation("Created event with id: {Id}, title: {Title}", ev.Id, ev.Title);
        return ev;
    }

    public Event? UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
        _logger.LogInformation("Updating event with id: {Id}", id);
        if (!_events.TryGetValue(id, out var existingEvent))
        {
            _logger.LogWarning("Event with id {Id} not found for update", id);
            return null;
        }

        var updatedEvent = EventMapper.FromUpdateDto(id, title, description, startAt, endAt);

        if (!_events.TryUpdate(id, updatedEvent, existingEvent))
        {
            _logger.LogWarning("Event with id {Id} was modified by another request", id);
            return null;
        }

        _logger.LogInformation("Updated event with id: {Id}", id);
        return updatedEvent;
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
