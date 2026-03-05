namespace EventTracker.Models.Dto;
    public record CreateEventDto(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
    );
  

