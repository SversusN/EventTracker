using EventApi.DataAccess;
using EventApi.Dto;
using EventApi.Entities;
using EventApi.Exceptions;
using EventApi.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Services;

internal sealed class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly AppDbContext _context;

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await BookingLock.WaitAsync(cancellationToken);
        try
        {
            var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
                ?? throw new NotFoundException("Event not found");

            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            var booking = Booking.CreatePending(eventId);
            await _context.Bookings.AddAsync(booking, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return ToInfo(booking);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking not found");

        return ToInfo(booking);
    }

    internal static BookingInfo ToInfo(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}
