using System.Collections.Concurrent;
using EventTrackerApi.Infrastructure.Mappers;
using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;

namespace EventTrackerApi.Services;

public class EventService(ILogger<EventService> logger) : IEventService
{
    private readonly ConcurrentDictionary<Guid, Event> _events = new();
    private readonly ILogger<EventService> _logger = logger;

    public PaginatedResult<Event> GetEvents(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting events with filters. Title: {Title}, From: {From}, To: {To}, Page: {Page}, PageSize: {PageSize}", title, from, to, page, pageSize);

        var query = _events.Values.AsEnumerable();

        // Фильтрация по названию (регистронезависимая, частичное совпадение)
        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }

        // Фильтрация по дате начала (события, которые начинаются не раньше указанной даты)
        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value);
        }

        // Фильтрация по дате окончания (события, которые заканчиваются не позже указанной даты)
        if (to.HasValue)
        {
            query = query.Where(e => e.EndAt <= to.Value);
        }

        var totalCount = query.Count();

        // Применяем пагинацию
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation("Found {TotalCount} events, returning {Count} items for page {Page}", totalCount, items.Count, page);

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
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
