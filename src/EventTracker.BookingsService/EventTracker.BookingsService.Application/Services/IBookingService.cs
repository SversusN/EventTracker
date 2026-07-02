using EventTracker.BookingsService.Domain.Models;

namespace EventTracker.BookingsService.Application.Services;

public interface IBookingService
{
    /// <summary>
    /// Создаёт бронь для указанного события от имени пользователя
    /// </summary>
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId);

    /// <summary>
    /// Получает бронь по идентификатору
    /// </summary>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);

    /// <summary>
    /// Подтверждает бронь и публикует событие в Kafka
    /// </summary>
    Task ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменяет бронь с проверкой прав доступа
    /// </summary>
    Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin = false);
}
