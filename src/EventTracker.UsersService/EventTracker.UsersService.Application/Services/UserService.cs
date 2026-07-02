using EventTracker.UsersService.Application.DTOs;
using EventTracker.UsersService.Application.Ports;
using EventTracker.UsersService.Domain.Exceptions;
using EventTracker.UsersService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace EventTracker.UsersService.Application.Services;

/// <summary>
/// Сервис для работы с пользователями
/// </summary>
public class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ILogger<UserService> logger) : IUserService
{
    public async Task RegisterAsync(RegisterRequestDto dto)
    {
        logger.LogInformation("Registering user with login: {Login}", dto.Login);

        var existingUser = await userRepository.GetByLoginAsync(dto.Login);
        if (existingUser is not null)
        {
            logger.LogWarning("User with login {Login} already exists", dto.Login);
            throw new ArgumentException($"User with login '{dto.Login}' already exists.");
        }

        var passwordHash = passwordHasher.Hash(dto.Password);
        var user = new User(dto.Login, passwordHash, dto.Role);

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        logger.LogInformation("User {Login} registered with id {UserId}", dto.Login, user.Id);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        logger.LogInformation("User {Login} attempting to log in", dto.Login);

        var user = await userRepository.GetByLoginAsync(dto.Login);
        if (user is null || !passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            logger.LogWarning("Invalid login attempt for user {Login}", dto.Login);
            throw new InvalidCredentialsException("Invalid login or password.");
        }

        var token = tokenService.GenerateToken(user);

        logger.LogInformation("User {Login} logged in successfully", dto.Login);
        return new LoginResponseDto(token);
    }
}
