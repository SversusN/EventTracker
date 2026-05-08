using EventTrackerApi.DataAccess;
using EventTrackerApi.Infrastructure.Mappers;
using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApi.Services;

public class EventService(AppDbContext context, ILogger<EventService> logger) : IEventService
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<EventService> _logger = logger;

    public async Task<PaginatedResult<Event>> GetEventsAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting events with filters. Title: {Title}, From: {From}, To: {To}, Page: {Page}, PageSize: {PageSize}", title, from, to, page, pageSize);

        var query = _context.Events.AsQueryable();

        // Фильтрация по названию (регистронезависимая, частичное совпадение)
        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
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

        var totalCount = await query.CountAsync();

        // Применяем пагинацию
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Found {TotalCount} events, returning {Count} items for page {Page}", totalCount, items.Count, page);

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting event by id: {Id}", id);
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null)
        {
            _logger.LogWarning("Event with id {Id} not found", id);
            return null;
        }
        return ev;
    }

    public async Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        ValidateEventData(title, startAt, endAt, totalSeats);

        var ev = EventMapper.FromCreateDto(title, description, startAt, endAt, totalSeats);

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created event with id: {Id}, title: {Title}, totalSeats: {TotalSeats}", ev.Id, ev.Title, ev.TotalSeats);
        return ev;
    }

    public async Task<Event?> UpdateEventAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
        _logger.LogInformation("Updating event with id: {Id}", id);
        var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (existingEvent is null)
        {
            _logger.LogWarning("Event with id {Id} not found for update", id);
            return null;
        }

        ValidateEventData(title, startAt, endAt, existingEvent.TotalSeats);

        var updatedEvent = EventMapper.FromUpdateDto(id, title, description, startAt, endAt, existingEvent.TotalSeats, existingEvent.AvailableSeats);

        _context.Entry(existingEvent).CurrentValues.SetValues(updatedEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated event with id: {Id}", id);
        return updatedEvent;
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

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        _logger.LogInformation("Deleting event with id: {Id}", id);
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null)
        {
            _logger.LogWarning("Event with id {Id} not found for deletion", id);
            return false;
        }

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted event with id: {Id}", id);
        return true;
    }
}
