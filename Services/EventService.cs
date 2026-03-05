using EventTracker.Models;
using EventTracker.Models.Dto;

namespace EventTracker.Services;

public class EventService : IEventService
{
    private readonly List<Event> _events = [];
    private readonly Lock _lock = new();
    private readonly ILogger<EventService> _logger;

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<EventResponseDto> GetAllEvents()
    {
        _logger.LogInformation("Getting all events. Count: {Count}", _events.Count);
        lock (_lock)
        {
            return _events.Select(MapToResponseDto).ToList();
        }
    }

    public EventResponseDto? GetEventById(Guid id)
    {
        _logger.LogInformation("Getting event by id: {Id}", id);
        lock (_lock)
        {
            var ev = _events.FirstOrDefault(e => e.Id == id);
            if (ev is null)
            {
                _logger.LogWarning("Event with id {Id} not found", id);
                return null;
            }
            return MapToResponseDto(ev);
        }
    }

    public EventResponseDto CreateEvent(CreateEventDto dto)
    {
        var ev = new Event(
            dto.Title,
            dto.Description,
            dto.StartAt,
            dto.EndAt
        );

        lock (_lock)
        {
            _events.Add(ev);
        }

        _logger.LogInformation("Created event with id: {Id}, title: {Title}", ev.Id, ev.Title);
        return MapToResponseDto(ev);
    }

    public EventResponseDto? UpdateEvent(Guid id, UpdateEventDto dto)
    {
        _logger.LogInformation("Updating event with id: {Id}", id);
        lock (_lock)
        {
            var ev = _events.FirstOrDefault(e => e.Id == id);
            if (ev is null)
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
            return MapToResponseDto(ev);
        }
    }

    public bool DeleteEvent(Guid id)
    {
        _logger.LogInformation("Deleting event with id: {Id}", id);
        lock (_lock)
        {
            var ev = _events.FirstOrDefault(e => e.Id == id);
            if (ev is null)
            {
                _logger.LogWarning("Event with id {Id} not found for deletion", id);
                return false;
            }

            _events.Remove(ev);
            _logger.LogInformation("Deleted event with id: {Id}", id);
            return true;
        }
    }

    private static EventResponseDto MapToResponseDto(Event ev)
    {
        return new EventResponseDto(
            ev.Id,
            ev.Title,
            ev.Description,
            ev.StartAt,
            ev.EndAt
        );
    }
}
