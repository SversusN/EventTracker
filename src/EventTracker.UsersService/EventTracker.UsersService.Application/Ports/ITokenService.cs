using EventTracker.UsersService.Domain.Models;

namespace EventTracker.UsersService.Application.Ports;

/// <summary>
/// Абстракция для генерации JWT-токена
/// </summary>
public interface ITokenService
{
    string GenerateToken(User user);
}
