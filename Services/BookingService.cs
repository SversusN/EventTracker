using System.Collections.Concurrent;
using EventTrackerApi.Models;

namespace EventTrackerApi.Services;

/// <summary>
/// Сервис для работы с бронированиями (in-memory хранилище)
/// </summary>
public class BookingService(IEventService eventService, ILogger<BookingService> logger) : IBookingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

    public async Task<Booking?> CreateBookingAsync(Guid eventId)
    {
        logger.LogInformation("Creating booking for event {EventId}", eventId);

        // Проверяем существование события
        var eventItem = eventService.GetEventById(eventId);
        if (eventItem is null)
        {
            logger.LogWarning("Cannot create booking: event {EventId} not found", eventId);
            return null;
        }

        // Создаём бронь в статусе Pending
        var booking = new Booking(eventId);
        _bookings.TryAdd(booking.Id, booking);

        logger.LogInformation("Created booking {BookingId} for event {EventId} with status {Status}",
            booking.Id, eventId, booking.Status);

        return booking;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        logger.LogInformation("Getting booking by id: {BookingId}", bookingId);

        if (!_bookings.TryGetValue(bookingId, out var booking))
        {
            logger.LogWarning("Booking with id {BookingId} not found", bookingId);
            return null;
        }

        return booking;
    }

    public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status)
    {
        logger.LogInformation("Getting bookings by status: {Status}", status);

        var bookings = _bookings.Values
            .Where(b => b.Status == status)
            .ToList();

        return bookings;
    }

    public async Task<bool> UpdateBookingAsync(Booking booking)
    {
        logger.LogInformation("Updating booking {BookingId} with status {Status}",
            booking.Id, booking.Status);

        if (!_bookings.ContainsKey(booking.Id))
        {
            logger.LogWarning("Cannot update booking {BookingId}: not found", booking.Id);
            return false;
        }

        _bookings[booking.Id] = booking;
        return true;
    }
}
