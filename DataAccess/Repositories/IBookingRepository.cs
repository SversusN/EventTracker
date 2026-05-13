using EventTrackerApi.Models;

namespace EventTrackerApi.DataAccess.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<IEnumerable<Booking>> GetPendingAsync();
    Task AddAsync(Booking booking);
    void Update(Booking booking);
    Task SaveChangesAsync();
}
