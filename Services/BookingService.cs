using System.Collections.Concurrent;
using EventTrackerApi.Exceptions;
using EventTrackerApi.Models;

namespace EventTrackerApi.Services;

/// <summary>
/// Сервис для работы с бронированиями (in-memory хранилище)
/// </summary>
public class BookingService(IEventService eventService, ILogger<BookingService> logger) : IBookingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
    private readonly Lock _bookingLock = new();

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        logger.LogInformation("Creating booking for event {EventId}", eventId);

        lock (_bookingLock)
        {
            // Проверяем существование события
            var eventItem = eventService.GetEventById(eventId);
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
            _bookings.TryAdd(booking.Id, booking);

            logger.LogInformation("Created booking {BookingId} for event {EventId} with status {Status}. Available seats left: {AvailableSeats}",
                booking.Id, eventId, booking.Status, eventItem.AvailableSeats);

            return Task.FromResult(booking);
        }
    }

    public Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        logger.LogInformation("Getting booking by id: {BookingId}", bookingId);

        if (!_bookings.TryGetValue(bookingId, out var booking))
        {
            logger.LogWarning("Booking with id {BookingId} not found", bookingId);
            return Task.FromResult<Booking?>(null);
        }

        return Task.FromResult<Booking?>(booking);
    }

    public Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status)
    {
        logger.LogInformation("Getting bookings by status: {Status}", status);

        var bookings = _bookings.Values
            .Where(b => b.Status == status)
            .ToList();

        return Task.FromResult<IEnumerable<Booking>>(bookings);
    }

    public Task<bool> UpdateBookingAsync(Booking booking)
    {
        logger.LogInformation("Updating booking {BookingId} with status {Status}",
            booking.Id, booking.Status);

        if (!_bookings.ContainsKey(booking.Id))
        {
            logger.LogWarning("Cannot update booking {BookingId}: not found", booking.Id);
            return Task.FromResult(false);
        }

        _bookings[booking.Id] = booking;
        return Task.FromResult(true);
    }
}
