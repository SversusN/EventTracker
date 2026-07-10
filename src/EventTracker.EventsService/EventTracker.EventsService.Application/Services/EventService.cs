using EventTracker.EventsService.Domain.Models;
using EventTracker.EventsService.Application.DTOs;
using EventTracker.EventsService.Application.Mappers;
using EventTracker.EventsService.Application.Ports;
using Microsoft.Extensions.Logging;

namespace EventTracker.EventsService.Application.Services;

public class EventService(IEventRepository eventRepository, ILogger<EventService> logger) : IEventService
{
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly ILogger<EventService> _logger = logger;

    public async Task<PaginatedResult<Event>> GetEventsAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting events with filters. Title: {Title}, From: {From}, To: {To}, Page: {Page}, PageSize: {PageSize}", title, from, to, page, pageSize);

        var result = await _eventRepository.GetEventsAsync(title, from, to, page, pageSize);

        _logger.LogInformation("Found {TotalCount} events, returning {Count} items for page {Page}", result.TotalCount, result.Items.Count(), page);

        return result;
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting event by id: {Id}", id);
        var ev = await _eventRepository.GetByIdAsync(id);
        if (ev is null)
        {
            _logger.LogWarning("Event with id {Id} not found", id);
            return null;
        }
        return ev;
    }

    public async Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        startAt = ToUtc(startAt);
        endAt = ToUtc(endAt);
        ValidateEventData(title, startAt, endAt, totalSeats);

        var ev = EventMapper.FromCreateDto(title, description, startAt, endAt, totalSeats);

        await _eventRepository.AddAsync(ev);
        await _eventRepository.SaveChangesAsync();

        _logger.LogInformation("Created event with id: {Id}, title: {Title}, totalSeats: {TotalSeats}", ev.Id, ev.Title, ev.TotalSeats);
        return ev;
    }

    public async Task<Event?> UpdateEventAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
        _logger.LogInformation("Updating event with id: {Id}", id);
        startAt = ToUtc(startAt);
        endAt = ToUtc(endAt);
        var existingEvent = await _eventRepository.GetByIdAsync(id);
        if (existingEvent is null)
        {
            _logger.LogWarning("Event with id {Id} not found for update", id);
            return null;
        }

        ValidateEventData(title, startAt, endAt, existingEvent.TotalSeats);

        var updatedEvent = EventMapper.FromUpdateDto(id, title, description, startAt, endAt, existingEvent.TotalSeats, existingEvent.AvailableSeats);

        _eventRepository.SetValues(existingEvent, updatedEvent);
        await _eventRepository.SaveChangesAsync();

        _logger.LogInformation("Updated event with id: {Id}", id);
        return updatedEvent;
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        _logger.LogInformation("Deleting event with id: {Id}", id);
        var ev = await _eventRepository.GetByIdAsync(id);
        if (ev is null)
        {
            _logger.LogWarning("Event with id {Id} not found for deletion", id);
            return false;
        }

        _eventRepository.Remove(ev);
        await _eventRepository.SaveChangesAsync();

        _logger.LogInformation("Deleted event with id: {Id}", id);
        return true;
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static void ValidateEventData(string title, DateTime startAt, DateTime endAt, int totalSeats)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (endAt <= startAt)
        {
            throw new ArgumentException("EndAt must be later than StartAt.");
        }

        if (totalSeats <= 0)
        {
            throw new ArgumentException("TotalSeats must be greater than 0.", nameof(totalSeats));
        }
    }
}
