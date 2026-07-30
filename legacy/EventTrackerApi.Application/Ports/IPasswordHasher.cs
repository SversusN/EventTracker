namespace EventTrackerApi.Application.Ports;

/// <summary>
/// Абстракция для хеширования паролей
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
