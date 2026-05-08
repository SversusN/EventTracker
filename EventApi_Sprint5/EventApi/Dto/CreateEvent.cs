namespace EventApi.Dto;

public sealed record CreateEvent
{
    public string? Title { get; init; }
    public DateTime? StartAt { get; init; }
    public DateTime? EndAt { get; init; }
    public int? TotalSeats { get; init; }
    public string? Description { get; init; }
}
