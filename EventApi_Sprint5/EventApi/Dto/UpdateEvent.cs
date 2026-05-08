namespace EventApi.Dto;

public sealed record UpdateEvent
{
    public string? Title { get; init; }
    public DateTime? StartAt { get; init; }
    public DateTime? EndAt { get; init; }
    public string? Description { get; init; }
}
