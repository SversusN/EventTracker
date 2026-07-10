namespace EventTrackerApi.Application.DTOs;

public record EventResponseDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats
);
