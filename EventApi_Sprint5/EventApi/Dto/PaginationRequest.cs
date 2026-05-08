namespace EventApi.Dto;

public sealed record PaginationRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}