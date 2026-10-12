using System.Text;

namespace _CarRental.Application.Common.Models;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt:Audience must be configured.");
        if (Encoding.UTF8.GetByteCount(SigningKey) < MinimumSigningKeyBytes)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes and must be configured through user-secrets or an environment variable.");
        if (AccessTokenMinutes <= 0)
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be greater than zero.");
        if (RefreshTokenDays <= 0)
            throw new InvalidOperationException("Jwt:RefreshTokenDays must be greater than zero.");
    }
}
