using EventTracker.BookingsService.Domain.Models;

namespace EventTracker.BookingsService.Application.Ports;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<IEnumerable<Booking>> GetPendingAsync();
    Task<IEnumerable<Booking>> GetActiveByUserIdAsync(Guid userId);
    Task AddAsync(Booking booking);
    void Update(Booking booking);
    Task SaveChangesAsync();
}
