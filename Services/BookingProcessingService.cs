using EventTrackerApi.Models;

namespace EventTrackerApi.Services;

/// <summary>
/// Фоновый сервис для обработки бронирований
/// </summary>
public class BookingProcessingService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingProcessingService> logger) : BackgroundService
{
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _artificialDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Booking processing service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBookingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing pending bookings");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        logger.LogInformation("Booking processing service stopped");
    }

    private async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        //Scope должен существовать только на время одной итерации обработки.
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Получаем все брони в статусе Pending
        var pendingBookings = await bookingService.GetBookingsByStatusAsync(BookingStatus.Pending);

        foreach (var booking in pendingBookings)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                logger.LogInformation("Processing booking {BookingId} for event {EventId}",
                    booking.Id, booking.EventId);

                // Искусственная задержка, имитирующая обращение к внешней системе
                await Task.Delay(_artificialDelay, cancellationToken);

                // Пока просто подтверждаем все брони (в следующих спринтах будет логика выбора)
                booking.Confirm();
                await bookingService.UpdateBookingAsync(booking);

                logger.LogInformation("Booking {BookingId} confirmed at {ProcessedAt}",
                    booking.Id, booking.ProcessedAt);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Processing of booking {BookingId} was cancelled", booking.Id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing booking {BookingId}", booking.Id);
            }
        }
    }
}
