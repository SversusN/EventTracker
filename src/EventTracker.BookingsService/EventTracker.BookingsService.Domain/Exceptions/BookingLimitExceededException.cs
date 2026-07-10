namespace EventTracker.BookingsService.Domain.Exceptions;

public class BookingLimitExceededException : Exception
{
    public BookingLimitExceededException(string message) : base(message) { }
}
