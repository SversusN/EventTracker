using System.ComponentModel.DataAnnotations;

namespace EventTrackerApi.Domain.Models;

/// <summary>
/// Пользователь системы
/// </summary>
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Логин пользователя
    /// </summary>
    public string Login { get; private set; } = string.Empty;

    /// <summary>
    /// Хеш пароля
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Роль пользователя
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Бронирования пользователя
    /// </summary>
    public ICollection<Booking> Bookings { get; private set; } = [];

    private User() { }

    /// <summary>
    /// Создаёт нового пользователя
    /// </summary>
    public User(string login, string passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ValidationException("Login is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ValidationException("PasswordHash is required.");
        }

        Id = Guid.NewGuid();
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }
}
