using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using Microsoft.IdentityModel.Tokens;

namespace _CarRental.Infrastructure.Identity;

public sealed class JwtService(JwtOptions options, IDateTimeProvider dateTimeProvider) : IJwtService
{
    private readonly SigningCredentials _signingCredentials = new(
        new SymmetricSecurityKey(ValidateAndGetKey(options)),
        SecurityAlgorithms.HmacSha256);

    public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(int userId, string username, IEnumerable<string> roles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(roles);

        var now = TruncateToSeconds(dateTimeProvider.UtcNow.ToUniversalTime());
        var expiresAtUtc = now.AddMinutes(options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.UniqueName, username)
        };
        claims.AddRange(roles.Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => new Claim("role", role)));

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: _signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return (token, dateTimeProvider.UtcNow.ToUniversalTime().AddDays(options.RefreshTokenDays));
    }

    private static byte[] ValidateAndGetKey(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return Encoding.UTF8.GetBytes(options.SigningKey);
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);


}
