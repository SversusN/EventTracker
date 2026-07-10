using EventTracker.BookingsService.Domain.Exceptions;
using EventTracker.BookingsService.Domain.Models;
using EventTracker.BookingsService.Application.Options;
using EventTracker.BookingsService.Application.Ports;
using EventTracker.Contracts.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventTracker.BookingsService.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IMessagePublisher<BookingConfirmedEvent> publisher,
    IOptions<BookingOptions> bookingOptions,
    ILogger<BookingService> logger) : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);
    private readonly int _maxActiveBookingsPerUser = bookingOptions.Value.MaxActiveBookingsPerUser;

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId)
    {
        logger.LogInformation("Creating booking for event {EventId} by user {UserId}", eventId, userId);

        await BookingLock.WaitAsync();
        try
        {
            var activeBookings = await bookingRepository.GetActiveByUserIdAsync(userId);
            if (activeBookings.Count() >= _maxActiveBookingsPerUser)
            {
                logger.LogWarning("Cannot create booking: user {UserId} has reached the limit of {Limit} active bookings", userId, _maxActiveBookingsPerUser);
                throw new BookingLimitExceededException($"User has reached the limit of {_maxActiveBookingsPerUser} active bookings.");
            }

            var booking = new Booking(eventId, userId);
            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();

            logger.LogInformation("Created booking {BookingId} for event {EventId} by user {UserId}",
                booking.Id, eventId, userId);

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

    public async Task ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Confirming booking {BookingId}", bookingId);

        await BookingLock.WaitAsync(cancellationToken);
        try
        {
            var booking = await bookingRepository.GetByIdAsync(bookingId);
            if (booking is null)
            {
                logger.LogWarning("Cannot confirm booking: booking {BookingId} not found", bookingId);
                throw new KeyNotFoundException($"Booking with id '{bookingId}' not found.");
            }

            if (booking.Status != BookingStatus.Pending)
            {
                logger.LogInformation("Booking {BookingId} is not pending (status: {Status}), skipping confirmation", bookingId, booking.Status);
                return;
            }

            booking.Confirm();
            bookingRepository.Update(booking);
            await bookingRepository.SaveChangesAsync();

            logger.LogInformation("Booking {BookingId} confirmed", bookingId);

            try
            {
                await publisher.PublishAsync(
                    new BookingConfirmedEvent(
                        booking.Id,
                        booking.EventId,
                        booking.UserId,
                        Seats: 1,
                        ConfirmedAt: DateTime.UtcNow),
                    cancellationToken);

                logger.LogInformation("Published BookingConfirmed event for booking {BookingId}", bookingId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish BookingConfirmed event for booking {BookingId}", bookingId);
            }
        }
        finally
        {
            BookingLock.Release();
        }
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin = false)
    {
        logger.LogInformation("Cancelling booking {BookingId} by user {UserId}, isAdmin={IsAdmin}", bookingId, userId, isAdmin);

        await BookingLock.WaitAsync();
        try
        {
            var booking = await bookingRepository.GetByIdAsync(bookingId);
            if (booking is null)
            {
                logger.LogWarning("Cannot cancel booking: booking {BookingId} not found", bookingId);
                throw new KeyNotFoundException($"Booking with id '{bookingId}' not found.");
            }

            if (!isAdmin && booking.UserId != userId)
            {
                logger.LogWarning("Cannot cancel booking {BookingId}: user {UserId} does not have permission", bookingId, userId);
                throw new ForbiddenOperationException("You can only cancel your own bookings.");
            }

            booking.Cancel();
            bookingRepository.Update(booking);
            await bookingRepository.SaveChangesAsync();

            logger.LogInformation("Booking {BookingId} cancelled by user {UserId}", bookingId, userId);
        }
        finally
        {
            BookingLock.Release();
        }
    }
}
