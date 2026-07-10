namespace EventTrackerApi.Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при превышении лимита активных броней пользователя
/// </summary>
public class BookingLimitExceededException(string message) : Exception(message)
{
}
