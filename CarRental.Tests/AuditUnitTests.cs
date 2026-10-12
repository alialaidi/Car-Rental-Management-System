using _CarRental.API.Extensions;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace _CarRental.Tests;

public sealed class AuditUnitTests
{
    [Fact]
    public void AuditValueSerializerRedactsSensitiveFieldsRecursively()
    {
        var json = AuditValueSerializer.Serialize(new
        {
            status = "Active",
            PasswordHash = "password-secret",
            metadata = new { refreshToken = "token-secret", changedBy = "employee" }
        });

        Assert.NotNull(json);
        Assert.Contains("Active", json);
        Assert.Contains("employee", json);
        Assert.DoesNotContain("password-secret", json);
        Assert.DoesNotContain("token-secret", json);
        Assert.Equal(2, json.Split("[REDACTED]", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public async Task AuditLoggerSwallowsRepositoryFailure()
    {
        var logger = new AuditLogger(
            new FailingAuditRepository(),
            new StubRequestContext(),
            new FixedDateTimeProvider(),
            NullLogger<AuditLogger>.Instance);

        await logger.LogAsync("Test.Action", "Users", "1", null, new { Active = true });
    }

    [Fact]
    public void HttpRequestContextTruncatesHeaderAndCorrelationValues()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = new string('u', 300);
        context.TraceIdentifier = new string('c', 150);
        var accessor = new HttpContextAccessor { HttpContext = context };
        var requestContext = new HttpRequestContext(accessor);

        Assert.Equal(255, requestContext.DeviceInfo?.Length);
        Assert.Equal(128, requestContext.CorrelationId?.Length);
    }

    private sealed class FailingAuditRepository : IAuditLogRepository
    {
        public Task InsertAsync(AuditLog auditLog) => throw new InvalidOperationException("Audit storage unavailable.");

        public Task<IReadOnlyList<AuditLog>> QueryAsync(int page, int pageSize, int? actorUserId = null, string? entityName = null) =>
            throw new NotSupportedException();
    }

    private sealed class StubRequestContext : IRequestContext
    {
        public string? IpAddress => null;
        public string? DeviceInfo => null;
        public string? CorrelationId => null;
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);
    }
}
