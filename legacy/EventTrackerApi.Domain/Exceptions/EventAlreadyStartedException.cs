namespace EventTrackerApi.Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при попытке забронировать событие, которое уже началось
/// </summary>
public class EventAlreadyStartedException(string message) : Exception(message)
{
}
