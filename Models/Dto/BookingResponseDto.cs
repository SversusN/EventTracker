namespace EventTrackerApi.Models.Dto;

/// <summary>
/// DTO для ответа с информацией о бронировании
/// </summary>
public record BookingResponseDto(
    Guid Id,
    Guid EventId,
    BookingStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);
