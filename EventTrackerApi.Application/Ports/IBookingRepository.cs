using EventTrackerApi.Domain.Models;

namespace EventTrackerApi.Application.Ports;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<IEnumerable<Booking>> GetPendingAsync();
    Task AddAsync(Booking booking);
    void Update(Booking booking);
    Task SaveChangesAsync();
}
