using EventTracker.UsersService.Domain.Models;

namespace EventTracker.UsersService.Application.DTOs;

public record RegisterRequestDto(
    string Login,
    string Password,
    UserRole Role = UserRole.User
);
