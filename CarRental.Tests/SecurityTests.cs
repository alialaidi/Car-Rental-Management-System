using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Infrastructure.Identity;
using _CarRental.Infrastructure.Persistence;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace _CarRental.Tests;

public sealed class SecurityTests
{
    private const string SigningKey = "this-is-a-test-signing-key-at-least-32-bytes";

    [Fact]
    public void BcryptHashesAreSaltedAndVerifyOnlyTheCorrectPassword()
    {
        var hasher = new BcryptPasswordHasher();
        var firstHash = hasher.Hash("correct horse battery staple");
        var secondHash = hasher.Hash("correct horse battery staple");

        Assert.NotEqual(firstHash, secondHash);
        Assert.True(hasher.Verify("correct horse battery staple", firstHash));
        Assert.False(hasher.Verify("wrong password", firstHash));
        Assert.False(hasher.Verify("correct horse battery staple", "not-a-bcrypt-hash"));
    }

    [Fact]
    public void AccessTokenHasExpectedClaimsAndValidatesIssuerAudienceSignatureAndLifetime()
    {
        var service = new JwtService(TestOptions(), new SystemDateTimeProvider());
        var (token, expiresAtUtc) = service.GenerateAccessToken(42, "customer-42", ["Customer", "BranchManager"]);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "CarRentalSystem.Tests",
            ValidateAudience = true,
            ValidAudience = "CarRentalSystem.Tests.Client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role"
        }, out var validatedToken);

        Assert.Equal("42", principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal("customer-42", principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value);
        Assert.Contains(principal.Claims, claim => claim.Type == "role" && claim.Value == "Customer");
        Assert.Contains(principal.Claims, claim => claim.Type == "role" && claim.Value == "BranchManager");
        Assert.True(principal.IsInRole("Customer"));
        Assert.Equal(expiresAtUtc, validatedToken.ValidTo);
    }

    [Fact]
    public void RefreshTokenContainsAtLeast64RandomUrlSafeBytes()
    {
        var service = new JwtService(TestOptions(), new SystemDateTimeProvider());
        var (token, expiresAtUtc) = service.GenerateRefreshToken();
        var base64 = token.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        Assert.DoesNotContain("=", token);
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.Equal(64, Convert.FromBase64String(base64).Length);
        Assert.True(expiresAtUtc > DateTime.UtcNow.AddDays(6));
        Assert.True(expiresAtUtc < DateTime.UtcNow.AddDays(8));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void JwtOptionsRejectsMissingOrShortSigningKeys(string signingKey)
    {
        var options = new JwtOptions
        {
            Issuer = "CarRentalSystem.Tests",
            Audience = "CarRentalSystem.Tests.Client",
            SigningKey = signingKey
        };
        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    private static JwtOptions TestOptions() => new()
    {
        Issuer = "CarRentalSystem.Tests",
        Audience = "CarRentalSystem.Tests.Client",
        SigningKey = SigningKey,
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };
}
