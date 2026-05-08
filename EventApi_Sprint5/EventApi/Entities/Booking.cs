using EventApi.Exceptions;

namespace EventApi.Entities;

internal sealed class Booking
{
    internal Guid Id { get; private set; }
    internal Guid EventId { get; private set; }
    internal BookingStatus Status { get; private set; }
    internal DateTime CreatedAt { get; private set; }
    internal DateTime? ProcessedAt { get; private set; }
    internal Event? Event { get; private set; }

    private Booking() { }

    private Booking(Guid id, Guid eventId, BookingStatus status, DateTime createdAt)
    {
        Id = id;
        EventId = eventId;
        Status = status;
        CreatedAt = createdAt;
    }

    internal static Booking CreatePending(Guid eventId)
    {
        if (eventId == Guid.Empty)
            throw new ValidationException(nameof(EventId), "EventId cannot be empty");

        return new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);
    }

    internal void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    internal void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}
