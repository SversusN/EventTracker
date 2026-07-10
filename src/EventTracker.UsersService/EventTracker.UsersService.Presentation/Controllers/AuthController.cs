using EventTracker.UsersService.Application.DTOs;
using EventTracker.UsersService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTracker.UsersService.Presentation.Controllers;

/// <summary>
/// Контроллер для аутентификации и регистрации пользователей
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Зарегистрировать нового пользователя
    /// </summary>
    /// <param name="dto">Данные для регистрации</param>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        await userService.RegisterAsync(dto);
        return NoContent();
    }

    /// <summary>
    /// Войти в систему и получить JWT-токен
    /// </summary>
    /// <param name="dto">Учётные данные</param>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var response = await userService.LoginAsync(dto);
        return Ok(response);
    }
}
