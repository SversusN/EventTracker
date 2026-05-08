namespace EventApi.Dto;

public sealed record EventInfo
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required DateTime StartAt { get; init; }
    public required DateTime EndAt { get; init; }
    public required int TotalSeats { get; init; }
    public required int AvailableSeats { get; init; }
    public string? Description { get; init; }
}
