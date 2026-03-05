namespace EventTracker.Models.Dtos;

public record UpdateEventDto(
    string Title,
    string Description,
    DateTime StartAt,
    DateTime EndAt
 );
