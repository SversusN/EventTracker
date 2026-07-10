namespace EventTracker.BookingsService.Application.Ports;

public interface IMessagePublisher<T>
{
    Task PublishAsync(T message, CancellationToken cancellationToken = default);
}
