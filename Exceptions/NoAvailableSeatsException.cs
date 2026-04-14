namespace EventTrackerApi.Exceptions;

/// <summary>
/// Исключение, возникающее при отсутствии свободных мест на событии
/// </summary>
public class NoAvailableSeatsException(string message) : Exception(message)
{
}
