using EventTrackerApi.Domain.Models;

namespace EventTrackerApi.Application.DTOs;

public record RegisterRequestDto(
    string Login,
    string Password,
    UserRole Role = UserRole.User
);
