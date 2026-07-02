namespace EventTracker.Contracts.Kafka;

public record BookingConfirmedEvent(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int Seats,
    DateTime ConfirmedAt);
