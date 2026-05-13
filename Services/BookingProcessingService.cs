using EventTrackerApi.DataAccess.Repositories;
using EventTrackerApi.Models;

namespace EventTrackerApi.Services;

/// <summary>
/// Фоновый сервис для обработки бронирований
/// </summary>
public class BookingProcessingService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingProcessingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Booking processing service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> pendingBookingIds;

                using (var scope = scopeFactory.CreateScope())
                {
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    var pendingBookings = await bookingRepository.GetPendingAsync();
                    pendingBookingIds = pendingBookings.Select(b => b.Id).ToList();
                }

                var tasks = pendingBookingIds.Select(id =>
                    ProcessBookingAsync(id, stoppingToken));

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing pending bookings");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Booking processing service delay was cancelled");
                throw;
            }
        }

        logger.LogInformation("Booking processing service stopped");
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        logger.LogInformation("Processing booking {BookingId}", bookingId);

        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

            var booking = await bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return;

            var eventItem = await eventRepository.GetByIdAsync(booking.EventId);
            if (eventItem == null)
            {
                booking.Reject();
                bookingRepository.Update(booking);
                await bookingRepository.SaveChangesAsync();

                logger.LogWarning(
                    "Booking {BookingId} rejected: event {EventId} not found",
                    booking.Id, booking.EventId);

                return;
            }

            booking.Confirm();
            bookingRepository.Update(booking);
            await bookingRepository.SaveChangesAsync();

            logger.LogInformation(
                "Booking {BookingId} for event {EventId} processed → {Status}",
                booking.Id, booking.EventId, booking.Status);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var booking = await bookingRepository.GetByIdAsync(bookingId);
                if (booking != null)
                {
                    booking.Reject();
                    bookingRepository.Update(booking);

                    var eventItem = await eventRepository.GetByIdAsync(booking.EventId);
                    if (eventItem != null)
                        eventItem.ReleaseSeats();

                    await bookingRepository.SaveChangesAsync();
                }

                logger.LogError(ex,
                    "Booking {BookingId} rejected due to processing error",
                    bookingId);
            }
            catch (Exception releaseEx)
            {
                logger.LogError(releaseEx,
                    "Failed to reject booking {BookingId} after error",
                    bookingId);
            }
        }
    }
}
