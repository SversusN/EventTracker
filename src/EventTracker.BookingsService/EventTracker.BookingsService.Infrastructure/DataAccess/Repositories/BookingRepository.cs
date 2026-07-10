using EventTracker.BookingsService.Domain.Models;
using EventTracker.BookingsService.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace EventTracker.BookingsService.Infrastructure.DataAccess.Repositories;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetPendingAsync()
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ToListAsync();
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    public void Update(Booking booking)
    {
        _context.Bookings.Update(booking);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
