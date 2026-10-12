using _CarRental.Application.Common.Interfaces;

namespace _CarRental.Infrastructure.Identity;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public const int WorkFactor = 12;

    public string Hash(string plainTextPassword)
    {
        ArgumentNullException.ThrowIfNull(plainTextPassword);
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: WorkFactor);
    }

    public bool Verify(string plainTextPassword, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(plainTextPassword);
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
