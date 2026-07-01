namespace EventTrackerApi.Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при отсутствии прав на выполнение операции
/// </summary>
public class ForbiddenOperationException(string message) : Exception(message)
{
}
