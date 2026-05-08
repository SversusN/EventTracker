using EventTrackerApi.DataAccess;
using EventTrackerApi.Exceptions;
using EventTrackerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApi.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
public class BookingService(AppDbContext context, ILogger<BookingService> logger) : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly AppDbContext _context = context;

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        logger.LogInformation("Creating booking for event {EventId}", eventId);

        await BookingLock.WaitAsync();
        try
        {
            // Проверяем существование события
            var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventItem is null)
            {
                logger.LogWarning("Cannot create booking: event {EventId} not found", eventId);
                throw new KeyNotFoundException($"Event with id '{eventId}' not found.");
            }

            // Проверяем доступные места
            if (!eventItem.TryReserveSeats())
            {
                logger.LogWarning("Cannot create booking: no available seats for event {EventId}", eventId);
                throw new NoAvailableSeatsException("No available seats for this event");
            }

            // Создаём бронь в статусе Pending
            var booking = new Booking(eventId);
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            logger.LogInformation("Created booking {BookingId} for event {EventId} with status {Status}. Available seats left: {AvailableSeats}",
                booking.Id, eventId, booking.Status, eventItem.AvailableSeats);

            return booking;
        }
        finally
        {
            BookingLock.Release();
        }
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        logger.LogInformation("Getting booking by id: {BookingId}", bookingId);

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
        {
            logger.LogWarning("Booking with id {BookingId} not found", bookingId);
            return null;
        }

        return booking;
    }
}
