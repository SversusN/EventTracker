using EventTrackerApi.Domain.Models;

namespace EventTrackerApi.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с бронированиями
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создаёт бронь для указанного события от имени пользователя
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <returns>Созданная бронь</returns>
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId);

    /// <summary>
    /// Получает бронь по идентификатору
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <returns>Бронь или null, если не найдена</returns>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);

    /// <summary>
    /// Отменяет бронь с проверкой прав доступа
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <param name="userId">Идентификатор текущего пользователя</param>
    Task CancelBookingAsync(Guid bookingId, Guid userId);
}
