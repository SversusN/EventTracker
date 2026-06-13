using EventTrackerApi.Domain.Models;
using EventTrackerApi.Domain.Exceptions;
using EventTrackerApi.Application.Ports;
using Microsoft.Extensions.Logging;

namespace EventTrackerApi.Application.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
public class BookingService(
    IEventRepository eventRepository,
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    ILogger<BookingService> logger) : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        logger.LogInformation("Creating booking for event {EventId} by user {UserId}", eventId, userId);

        await BookingLock.WaitAsync();
        try
        {
            var user = await userRepository.GetByIdAsync(userId);
            if (user is null)
            {
                logger.LogWarning("Cannot create booking: user {UserId} not found", userId);
                throw new KeyNotFoundException($"User with id '{userId}' not found.");
            }

            var eventItem = await eventRepository.GetByIdAsync(eventId);
            if (eventItem is null)
            {
                logger.LogWarning("Cannot create booking: event {EventId} not found", eventId);
                throw new KeyNotFoundException($"Event with id '{eventId}' not found.");
            }

            if (eventItem.StartAt <= DateTime.UtcNow)
            {
                logger.LogWarning("Cannot create booking: event {EventId} has already started", eventId);
                throw new EventAlreadyStartedException("Cannot book an event that has already started.");
            }

            var activeBookings = await bookingRepository.GetActiveByUserIdAsync(userId);
            if (activeBookings.Count() >= MaxActiveBookingsPerUser)
            {
                logger.LogWarning("Cannot create booking: user {UserId} has reached the limit of {Limit} active bookings", userId, MaxActiveBookingsPerUser);
                throw new BookingLimitExceededException($"User has reached the limit of {MaxActiveBookingsPerUser} active bookings.");
            }

            if (!eventItem.TryReserveSeats())
            {
                logger.LogWarning("Cannot create booking: no available seats for event {EventId}", eventId);
                throw new NoAvailableSeatsException("No available seats for this event");
            }

            var booking = new Booking(eventId, userId);
            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();

            logger.LogInformation("Created booking {BookingId} for event {EventId} by user {UserId}. Available seats left: {AvailableSeats}",
                booking.Id, eventId, userId, eventItem.AvailableSeats);

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

        var booking = await bookingRepository.GetByIdAsync(bookingId);
        if (booking is null)
        {
            logger.LogWarning("Booking with id {BookingId} not found", bookingId);
            return null;
        }

        return booking;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId)
    {
        logger.LogInformation("Cancelling booking {BookingId} by user {UserId}", bookingId, userId);

        await BookingLock.WaitAsync();
        try
        {
            var booking = await bookingRepository.GetByIdAsync(bookingId);
            if (booking is null)
            {
                logger.LogWarning("Cannot cancel booking: booking {BookingId} not found", bookingId);
                throw new KeyNotFoundException($"Booking with id '{bookingId}' not found.");
            }

            var currentUser = await userRepository.GetByIdAsync(userId);
            if (currentUser is null)
            {
                logger.LogWarning("Cannot cancel booking: user {UserId} not found", userId);
                throw new KeyNotFoundException($"User with id '{userId}' not found.");
            }

            if (currentUser.Role != UserRole.Admin && booking.UserId != userId)
            {
                logger.LogWarning("Cannot cancel booking {BookingId}: user {UserId} does not have permission", bookingId, userId);
                throw new ForbiddenOperationException("You can only cancel your own bookings.");
            }

            var eventItem = await eventRepository.GetByIdAsync(booking.EventId);

            // Бронь активна, если она в статусе Pending или Confirmed
            var wasActive = booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed;

            booking.Cancel();
            bookingRepository.Update(booking);

            if (wasActive && eventItem is not null)
            {
                eventItem.ReleaseSeats();
            }

            await bookingRepository.SaveChangesAsync();

            logger.LogInformation("Booking {BookingId} cancelled by user {UserId}", bookingId, userId);
        }
        finally
        {
            BookingLock.Release();
        }
    }
}
