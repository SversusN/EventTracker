using EventTracker.BookingsService.Application.Ports;
using EventTracker.BookingsService.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventTracker.BookingsService.Application.Services;

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
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            await bookingService.ConfirmBookingAsync(bookingId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Booking {BookingId} processing failed", bookingId);
        }
    }
}
