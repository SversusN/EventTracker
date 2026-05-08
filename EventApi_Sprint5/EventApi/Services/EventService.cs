using EventApi.DataAccess;
using EventApi.Dto;
using EventApi.Entities;
using EventApi.Exceptions;
using EventApi.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Services;

internal sealed class EventService : IEventService
{
    private readonly AppDbContext _context;

    public EventService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var @event = Event.Create(request.Title, request.StartAt, request.EndAt, request.TotalSeats, request.Description);
        await _context.Events.AddAsync(@event, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return ToInfo(@event);
    }

    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Event not found");

        return ToInfo(@event);
    }

    public async Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        int page = 1,
        int pageSize = 10,
        DateTime? from = null,
        DateTime? to = null,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events.AsQueryable();

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<EventInfo>
        {
            Items = items.Select(ToInfo).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent request, CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Event not found");

        @event.Update(request.Title, request.StartAt, request.EndAt, request.Description);
        await _context.SaveChangesAsync(cancellationToken);

        return ToInfo(@event);
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (@event == null)
            return false;

        _context.Events.Remove(@event);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    internal static EventInfo ToInfo(Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        StartAt = @event.StartAt,
        EndAt = @event.EndAt,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats,
        Description = @event.Description
    };
}
