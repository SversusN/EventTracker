namespace EventTrackerApi.Application.DTOs;

public record LoginRequestDto(
    string Login,
    string Password
);
