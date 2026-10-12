using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IDateTimeProvider dateTimeProvider,
    IAuditLogger auditLogger,
    RegisterRequestValidator registerValidator,
    LoginRequestValidator loginValidator) : IAuthService
{
    private const string GenericLoginFailure = "Invalid username or password.";
    private const string InvalidRefreshToken = "Invalid or expired refresh token.";

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var normalized = registerValidator.ValidateAndNormalize(request);
        var passwordHash = passwordHasher.Hash(normalized.Password);
        await unitOfWork.BeginTransactionAsync();
        int userId;
        int customerId;
        AuthResult result;

        try
        {
            var user = new User
            {
                Username = normalized.Username,
                Email = normalized.Email,
                PasswordHash = passwordHash,
                PhoneNumber = normalized.PhoneNumber,
                IsActive = true
            };
            userId = await unitOfWork.Users.CreateAsync(user);

            var customerRoleId = await unitOfWork.Users.GetRoleIdByNameAsync(RoleNames.Customer)
                ?? throw new InvalidOperationException("The required Customer role has not been seeded.");
            await unitOfWork.Users.AssignRoleAsync(userId, customerRoleId);

            customerId = await unitOfWork.Customers.CreateAsync(new Customer
            {
                UserId = userId,
                FullName = normalized.FullName,
                DrivingLicenseNumber = normalized.DrivingLicenseNumber,
                DrivingLicenseExpiry = normalized.DrivingLicenseExpiry,
                Address = normalized.Address
            });

            result = await CreateAuthResultAsync(userId, normalized.Username, [RoleNames.Customer]);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await TryRollbackAsync();
            throw;
        }

        await auditLogger.LogAsync(
            AuditActions.UserRegistered,
            "Users",
            userId.ToString(CultureInfo.InvariantCulture),
            oldValue: null,
            newValue: new { CustomerId = customerId, Result = "registered" },
            actorUserId: userId);
        return result;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var normalized = loginValidator.ValidateAndNormalize(request);
        var lookupValue = normalized.UsernameOrEmail.Contains('@')
            ? normalized.UsernameOrEmail.ToLowerInvariant()
            : normalized.UsernameOrEmail;
        var user = await unitOfWork.Users.GetByUsernameOrEmailAsync(lookupValue);
        var passwordMatches = user is not null
            ? passwordHasher.Verify(normalized.Password, user.PasswordHash)
            : BurnPasswordVerificationCost(normalized.Password);

        if (user is null || !passwordMatches || !user.IsActive)
        {
            await auditLogger.LogAsync(
                AuditActions.LoginFailed,
                "Users",
                user?.UserId.ToString(CultureInfo.InvariantCulture),
                oldValue: null,
                newValue: new { Result = "failed" },
                actorUserId: user?.UserId);
            throw new UnauthorizedException(GenericLoginFailure);
        }

        var roles = await unitOfWork.Users.GetRoleNamesAsync(user.UserId);
        var result = await CreateAuthResultAsync(user.UserId, user.Username, roles);
        await auditLogger.LogAsync(
            AuditActions.LoginSucceeded,
            "Users",
            user.UserId.ToString(CultureInfo.InvariantCulture),
            oldValue: null,
            newValue: new { Result = "succeeded" },
            actorUserId: user.UserId);
        return result;
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 500)
            throw new UnauthorizedException(InvalidRefreshToken);

        var storedToken = await unitOfWork.RefreshTokens.GetByTokenAsync(HashRefreshToken(refreshToken));
        if (storedToken is null)
            throw new UnauthorizedException(InvalidRefreshToken);

        if (storedToken.RevokedAt is not null)
        {
            await RevokeAllForUserInTransactionAsync(storedToken.UserId);
            await auditLogger.LogAsync(
                AuditActions.RefreshTokenReuseDetected,
                "Users",
                storedToken.UserId.ToString(CultureInfo.InvariantCulture),
                oldValue: null,
                newValue: new { Result = "refresh_token_reuse_detected" },
                actorUserId: storedToken.UserId);
            throw new UnauthorizedException(InvalidRefreshToken);
        }

        if (!storedToken.IsActive(dateTimeProvider.UtcNow))
            throw new UnauthorizedException(InvalidRefreshToken);

        var user = await unitOfWork.Users.GetByIdAsync(storedToken.UserId);
        if (user is null || !user.IsActive)
            throw new UnauthorizedException(InvalidRefreshToken);

        var roles = await unitOfWork.Users.GetRoleNamesAsync(user.UserId);
        var accessToken = jwtService.GenerateAccessToken(user.UserId, user.Username, roles);
        var newRefreshToken = jwtService.GenerateRefreshToken();

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var revoked = await unitOfWork.RefreshTokens.TryRevokeAsync(storedToken.TokenId, dateTimeProvider.UtcNow);
            if (!revoked)
            {
                await unitOfWork.RollbackAsync();
                await RevokeAllForUserInTransactionAsync(storedToken.UserId);
                await auditLogger.LogAsync(
                    AuditActions.RefreshTokenReuseDetected,
                    "Users",
                    storedToken.UserId.ToString(CultureInfo.InvariantCulture),
                    oldValue: null,
                    newValue: new { Result = "refresh_token_reuse_detected" },
                    actorUserId: storedToken.UserId);
                throw new UnauthorizedException(InvalidRefreshToken);
            }

            await unitOfWork.RefreshTokens.CreateAsync(new RefreshToken
            {
                UserId = user.UserId,
                Token = HashRefreshToken(newRefreshToken.Token),
                ExpiresAt = newRefreshToken.ExpiresAtUtc
            });
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await TryRollbackAsync();
            throw;
        }

        return new AuthResult
        {
            AccessToken = accessToken.Token,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 500)
            return;

        var storedToken = await unitOfWork.RefreshTokens.GetByTokenAsync(HashRefreshToken(refreshToken));
        if (storedToken is null)
            return;

        await unitOfWork.RefreshTokens.RevokeAsync(storedToken.TokenId, dateTimeProvider.UtcNow);
    }

    private async Task<AuthResult> CreateAuthResultAsync(int userId, string username, IEnumerable<string> roles)
    {
        var accessToken = jwtService.GenerateAccessToken(userId, username, roles);
        var refreshToken = jwtService.GenerateRefreshToken();
        await unitOfWork.RefreshTokens.CreateAsync(new RefreshToken
        {
            UserId = userId,
            Token = HashRefreshToken(refreshToken.Token),
            ExpiresAt = refreshToken.ExpiresAtUtc
        });

        return new AuthResult
        {
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc
        };
    }

    private async Task RevokeAllForUserInTransactionAsync(int userId)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, dateTimeProvider.UtcNow);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await TryRollbackAsync();
            throw;
        }
    }

    private async Task TryRollbackAsync()
    {
        try
        {
            await unitOfWork.RollbackAsync();
        }
        catch (InvalidOperationException)
        {
            // Preserve the original exception when the transaction already ended.
        }
    }

    private bool BurnPasswordVerificationCost(string password)
    {
        _ = passwordHasher.Hash(password);
        return false;
    }

    internal static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));



}
