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
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Booking processing service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBookingsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing pending bookings");
            }

            try
            {
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Booking processing service delay was cancelled");
                throw;
            }
        }

        logger.LogInformation("Booking processing service stopped");
    }

    private async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        // Получаем все брони в статусе Pending
        var pendingBookings = await bookingService.GetBookingsByStatusAsync(BookingStatus.Pending);
        var pendingList = pendingBookings.ToList();

        if (pendingList.Count > 0)
        {
            logger.LogInformation("Found {Count} pending bookings to process", pendingList.Count);
        }

        var tasks = pendingList.Select(booking => ProcessBookingAsync(booking, eventService, bookingService, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessBookingAsync(
        Booking booking,
        IEventService eventService,
        IBookingService bookingService,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing booking {BookingId} for event {EventId}",
            booking.Id, booking.EventId);

        try
        {
            // Искусственная задержка выполняется до захвата семафора, так задержки выполняются параллельно
            await Task.Delay(_artificialDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Processing delay of booking {BookingId} was cancelled", booking.Id);
            throw;
        }

        await _processingSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Проверяем, существует ли событие в хранилище
            var eventItem = eventService.GetEventById(booking.EventId);
            if (eventItem is null)
            {
                logger.LogWarning("Event {EventId} not found for booking {BookingId}. Rejecting booking.",
                    booking.EventId, booking.Id);
                booking.Reject();
                await bookingService.UpdateBookingAsync(booking);
                return;
            }

            // Подтверждаем бронь
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

            // При неожиданной ошибке отклоняем бронь и возвращаем место
            try
            {
                var eventItem = eventService.GetEventById(booking.EventId);
                eventItem?.ReleaseSeats();
                booking.Reject();
                await bookingService.UpdateBookingAsync(booking);
                logger.LogInformation("Booking {BookingId} rejected and seats released due to error", booking.Id);
            }
            catch (Exception innerEx)
            {
                logger.LogError(innerEx, "Failed to reject booking {BookingId} after error", booking.Id);
            }
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}
