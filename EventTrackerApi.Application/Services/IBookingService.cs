using EventTrackerApi.Domain.Models;

namespace EventTrackerApi.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с бронированиями
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создаёт бронь для указанного события
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <returns>Созданная бронь</returns>
    Task<Booking> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// Получает бронь по идентификатору
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <returns>Бронь или null, если не найдена</returns>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);
}
