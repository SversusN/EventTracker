using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApi.DataAccess.Repositories;

public class EventRepository(AppDbContext context) : IEventRepository
{
    private readonly AppDbContext _context = context;

    public async Task<PaginatedResult<Event>> GetEventsAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EndAt <= to.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        return await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task AddAsync(Event ev)
    {
        await _context.Events.AddAsync(ev);
    }

    public void SetValues(Event target, Event source)
    {
        _context.Entry(target).CurrentValues.SetValues(source);
    }

    public void Remove(Event ev)
    {
        _context.Events.Remove(ev);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
