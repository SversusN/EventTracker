using EventTracker.EventsService.Application.DTOs;
using EventTracker.EventsService.Application.Mappers;
using EventTracker.EventsService.Application.Options;
using EventTracker.EventsService.Application.Ports;
using EventTracker.EventsService.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventTracker.EventsService.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository eventRepository,
        ICacheService cacheService,
        IOptions<CacheOptions> cacheOptions,
        ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<PaginatedResult<Event>> GetEventsAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting events with filters. Title: {Title}, From: {From}, To: {To}, Page: {Page}, PageSize: {PageSize}", title, from, to, page, pageSize);

        var result = await _eventRepository.GetEventsAsync(title, from, to, page, pageSize);

        _logger.LogInformation("Found {TotalCount} events, returning {Count} items for page {Page}", result.TotalCount, result.Items.Count(), page);

        return result;
    }

    public async Task<EventResponseDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Event(id);

        var cached = await _cacheService.GetAsync<EventResponseDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for event {EventId}", id);
            return cached;
        }

        _logger.LogInformation("Cache miss for event {EventId}, fetching from database", id);
        var ev = await _eventRepository.GetByIdAsync(id);
        if (ev is null)
        {
            _logger.LogWarning("Event with id {Id} not found", id);
            return null;
        }

        var dto = EventMapper.ToResponseDto(ev);
        await _cacheService.SetAsync(
            cacheKey,
            dto,
            TimeSpan.FromSeconds(_cacheOptions.EventTtlSeconds),
            cancellationToken);

        return dto;
    }

    public async Task<IReadOnlyList<EventResponseDto>> GetTopEventsAsync(int count, CancellationToken cancellationToken = default)
    {
        const string cacheKey = CacheKeys.TopEvents;

        var cached = await _cacheService.GetAsync<List<EventResponseDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for top {Count} events", count);
            return cached;
        }

        _logger.LogInformation("Cache miss for top {Count} events, fetching from database", count);
        var events = await _eventRepository.GetTopEventsAsync(count, cancellationToken);
        var dtos = events.Select(EventMapper.ToResponseDto).ToList();

        await _cacheService.SetAsync(
            cacheKey,
            dtos,
            TimeSpan.FromSeconds(_cacheOptions.TopEventsTtlSeconds),
            cancellationToken);

        return dtos;
    }

    public async Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        startAt = ToUtc(startAt);
        endAt = ToUtc(endAt);
        ValidateEventData(title, startAt, endAt, totalSeats);

        var ev = EventMapper.FromCreateDto(title, description, startAt, endAt, totalSeats);

        await _eventRepository.AddAsync(ev);
        await _eventRepository.SaveChangesAsync();

        await _cacheService.SetAsync(
            CacheKeys.Event(ev.Id),
            EventMapper.ToResponseDto(ev),
            TimeSpan.FromSeconds(_cacheOptions.EventTtlSeconds));

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

        await _cacheService.RemoveAsync(CacheKeys.Event(id));

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

        await _cacheService.RemoveAsync(CacheKeys.Event(id));

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
