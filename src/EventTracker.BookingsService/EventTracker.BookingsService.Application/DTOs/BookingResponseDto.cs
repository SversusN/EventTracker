using EventTracker.BookingsService.Domain.Models;

namespace EventTracker.BookingsService.Application.DTOs;

public record BookingResponseDto(
    Guid Id,
    Guid EventId,
    Guid UserId,
    BookingStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);
