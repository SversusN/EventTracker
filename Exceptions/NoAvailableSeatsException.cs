namespace EventTrackerApi.Exceptions;

/// <summary>
/// Исключение, возникающее при отсутствии свободных мест на событии
/// </summary>
public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message)
    {
    }
}
