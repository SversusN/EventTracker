namespace EventTracker.UsersService.Application.DTOs;

public record LoginRequestDto(
    string Login,
    string Password
);
