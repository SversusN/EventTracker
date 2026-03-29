using EventTrackerApi.Models;

namespace EventTrackerApi.Services;

/// <summary>
/// Интерфейс сервиса для работы с бронированиями
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создаёт бронь для указанного события
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <returns>Созданная бронь или null, если событие не найдено</returns>
    Task<Booking?> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// Получает бронь по идентификатору
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <returns>Бронь или null, если не найдена</returns>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);

    /// <summary>
    /// Получает все брони в указанном статусе
    /// </summary>
    /// <param name="status">Статус броней</param>
    /// <returns>Список броней</returns>
    Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status);

    /// <summary>
    /// Обновляет бронь
    /// </summary>
    /// <param name="booking">Бронь для обновления</param>
    /// <returns>true, если обновление успешно</returns>
    Task<bool> UpdateBookingAsync(Booking booking);
}
