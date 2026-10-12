using _CarRental.Domain.Entities;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace _CarRental.Tests;

public sealed class PersistenceIntegrationTests
{
    [SqlIntegrationFact]
    public async Task UserRepositoryCreatesReadsAndAssignsRoles()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = NewUser();

        var userId = await unitOfWork.Users.CreateAsync(user);
        var loaded = await unitOfWork.Users.GetByIdAsync(userId);
        var byUsername = await unitOfWork.Users.GetByUsernameOrEmailAsync(user.Username);
        var byEmail = await unitOfWork.Users.GetByUsernameOrEmailAsync(user.Email);
        var roleId = await unitOfWork.Users.GetRoleIdByNameAsync("Customer");

        Assert.NotNull(loaded);
        Assert.Equal(userId, loaded.UserId);
        Assert.Equal(user.Username, byUsername?.Username);
        Assert.Equal(user.Email, byEmail?.Email);
        Assert.True(await unitOfWork.Users.ExistsByUsernameAsync(user.Username));
        Assert.True(await unitOfWork.Users.ExistsByEmailAsync(user.Email));
        Assert.NotNull(roleId);

        await unitOfWork.Users.AssignRoleAsync(userId, roleId.Value);
        Assert.Contains("Customer", await unitOfWork.Users.GetRoleNamesAsync(userId));
    }

    [SqlIntegrationFact]
    public async Task DuplicateUsernameAndEmailBecomeConflictExceptions()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Users;
        var user = NewUser();
        await repository.CreateAsync(user);

        var usernameConflict = await Assert.ThrowsAsync<ConflictException>(() => repository.CreateAsync(NewUser(username: user.Username)));
        var emailConflict = await Assert.ThrowsAsync<ConflictException>(() => repository.CreateAsync(NewUser(email: user.Email)));

        Assert.Equal("USERNAME_ALREADY_EXISTS", usernameConflict.Code);
        Assert.Equal("EMAIL_ALREADY_EXISTS", emailConflict.Code);
    }

    [SqlIntegrationFact]
    public async Task CustomerRepositoryCreatesReadsAndRejectsDuplicateLicense()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = NewUser();
        var userId = await unitOfWork.Users.CreateAsync(user);
        var licenseNumber = "DL" + Guid.NewGuid().ToString("N");
        var customer = NewCustomer(userId, licenseNumber);

        var customerId = await unitOfWork.Customers.CreateAsync(customer);
        var loaded = await unitOfWork.Customers.GetByUserIdAsync(userId);
        Assert.Equal(customerId, loaded?.CustomerId);
        Assert.True(await unitOfWork.Customers.ExistsByDrivingLicenseNumberAsync(licenseNumber));

        var anotherUserId = await unitOfWork.Users.CreateAsync(NewUser());
        var conflict = await Assert.ThrowsAsync<ConflictException>(
            () => unitOfWork.Customers.CreateAsync(NewCustomer(anotherUserId, licenseNumber)));
        Assert.Equal("DRIVING_LICENSE_ALREADY_EXISTS", conflict.Code);
    }

    [SqlIntegrationFact]
    public async Task RefreshTokensCanBeCreatedAndRevokedForOneOrAllSessions()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var userId = await unitOfWork.Users.CreateAsync(NewUser());
        var tokenOne = "refresh-" + Guid.NewGuid().ToString("N");
        var tokenTwo = "refresh-" + Guid.NewGuid().ToString("N");
        var expiry = DateTime.UtcNow.AddDays(7);
        var tokenOneId = await unitOfWork.RefreshTokens.CreateAsync(new RefreshToken { UserId = userId, Token = tokenOne, ExpiresAt = expiry });
        var tokenTwoId = await unitOfWork.RefreshTokens.CreateAsync(new RefreshToken { UserId = userId, Token = tokenTwo, ExpiresAt = expiry });

        Assert.Equal(tokenOneId, (await unitOfWork.RefreshTokens.GetByTokenAsync(tokenOne))?.TokenId);
        var revokedAt = TruncateToMilliseconds(DateTime.UtcNow);
        await unitOfWork.RefreshTokens.RevokeAsync(tokenOneId, revokedAt);
        Assert.Equal(revokedAt, (await unitOfWork.RefreshTokens.GetByTokenAsync(tokenOne))?.RevokedAt);
        Assert.False(await unitOfWork.RefreshTokens.TryRevokeAsync(tokenOneId, revokedAt.AddSeconds(1)));

        var allRevokedAt = revokedAt.AddSeconds(1);
        await unitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, allRevokedAt);
        Assert.Equal(allRevokedAt, (await unitOfWork.RefreshTokens.GetByTokenAsync(tokenTwo))?.RevokedAt);
        Assert.False(await unitOfWork.RefreshTokens.TryRevokeAsync(tokenTwoId, allRevokedAt.AddSeconds(1)));
    }

    [SqlIntegrationFact]
    public async Task TransactionRollbackDiscardsChangesAcrossRepositories()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = NewUser();

        await unitOfWork.BeginTransactionAsync();
        var userId = await unitOfWork.Users.CreateAsync(user);
        await unitOfWork.Customers.CreateAsync(NewCustomer(userId, "DL" + Guid.NewGuid().ToString("N")));
        await unitOfWork.RollbackAsync();

        Assert.Null(await unitOfWork.Users.GetByIdAsync(userId));
        Assert.Null(await unitOfWork.Customers.GetByUserIdAsync(userId));
    }

    [SqlIntegrationFact]
    public async Task TransactionCommitPersistsChangesAcrossRepositories()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = NewUser();

        await unitOfWork.BeginTransactionAsync();
        var userId = await unitOfWork.Users.CreateAsync(user);
        var customer = NewCustomer(userId, "DL" + Guid.NewGuid().ToString("N"));
        await unitOfWork.Customers.CreateAsync(customer);
        await unitOfWork.CommitAsync();

        Assert.Equal(userId, (await unitOfWork.Users.GetByIdAsync(userId))?.UserId);
        Assert.Equal(customer.DrivingLicenseNumber,
            (await unitOfWork.Customers.GetByUserIdAsync(userId))?.DrivingLicenseNumber);
    }

    [SqlIntegrationFact]
    public async Task DisposedUnitOfWorkCannotReuseItsConnection()
    {
        var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.Users.ExistsByUsernameAsync("not-a-real-user");

        await unitOfWork.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => unitOfWork.Users.ExistsByUsernameAsync("not-a-real-user"));
        await provider.DisposeAsync();
    }

    [SqlIntegrationFact]
    public async Task AuditEntrySurvivesBusinessTransactionRollbackAndRedactsSecrets()
    {
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        const string entityName = "IntegrationAudit";

        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.Users.CreateAsync(NewUser());
        await auditLogger.LogAsync(
            AuditActions.UserRegistered,
            entityName,
            Guid.NewGuid().ToString("N"),
            oldValue: null,
            newValue: new { password = "must-not-persist", refreshToken = "also-secret", status = "Created" });
        await unitOfWork.RollbackAsync();

        var auditLog = Assert.Single(await auditRepository.QueryAsync(1, 10, entityName: entityName));
        Assert.Null(auditLog.ActorUserId);
        Assert.Contains("[REDACTED]", auditLog.NewValue);
        Assert.DoesNotContain("must-not-persist", auditLog.NewValue);
        Assert.DoesNotContain("also-secret", auditLog.NewValue);
        Assert.Contains("Created", auditLog.NewValue);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var connectionString = Environment.GetEnvironmentVariable("CAR_RENTAL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SQL integration tests require CAR_RENTAL_TEST_CONNECTION_STRING.");
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CarRentalDb"] = connectionString,
                ["Jwt:Issuer"] = "CarRentalSystem.Tests",
                ["Jwt:Audience"] = "CarRentalSystem.Tests.Client",
                ["Jwt:SigningKey"] = "test-only-signing-key-with-32-bytes-or-more"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddScoped<IRequestContext, TestRequestContext>();
        return services.BuildServiceProvider();
    }

    private static User NewUser(string? username = null, string? email = null) => new()
    {
        Username = username ?? "test_" + Guid.NewGuid().ToString("N"),
        Email = email ?? Guid.NewGuid().ToString("N") + "@integration.test",
        PasswordHash = "integration-test-hash"
    };

    private static Customer NewCustomer(int userId, string licenseNumber) => new()
    {
        UserId = userId,
        FullName = "Integration Test Customer",
        DrivingLicenseNumber = licenseNumber,
        DrivingLicenseExpiry = DateTime.UtcNow.Date.AddYears(1),
        Address = "Test address"
    };

    private static DateTime TruncateToMilliseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);

    private sealed class TestRequestContext : IRequestContext
    {
        public string? IpAddress => "127.0.0.1";
        public string? DeviceInfo => "integration-test";
        public string? CorrelationId => "integration-test-correlation";
    }
}
