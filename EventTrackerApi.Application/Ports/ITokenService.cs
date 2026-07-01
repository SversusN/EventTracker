using EventTrackerApi.Domain.Models;

namespace EventTrackerApi.Application.Ports;

/// <summary>
/// Абстракция для генерации JWT-токена
/// </summary>
public interface ITokenService
{
    string GenerateToken(User user);
}
