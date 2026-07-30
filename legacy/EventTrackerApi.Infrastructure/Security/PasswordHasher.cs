using EventTrackerApi.Application.Ports;
using System.Security.Cryptography;
using System.Text;

namespace EventTrackerApi.Infrastructure.Security;

/// <summary>
/// Хеширование паролей с помощью SHA-256
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string password, string hash)
    {
        return Hash(password) == hash;
    }
}
