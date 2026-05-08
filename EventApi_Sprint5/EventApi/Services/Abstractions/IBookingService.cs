using EventApi.Dto;

namespace EventApi.Services.Abstractions;

internal interface IBookingService
{
    Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
