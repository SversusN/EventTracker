using EventTrackerApi.Application.DTOs;

namespace EventTrackerApi.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с пользователями
/// </summary>
public interface IUserService
{
    Task RegisterAsync(RegisterRequestDto dto);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
}
