using _CarRental.Application.Common.Models;
using _CarRental.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace _CarRental.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void ProjectsAreAvailableToTheTestRunner()
    {
        Assert.Equal(1, new PagedRequest().Page);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 10)]
    [InlineData(500, 100)]
    public void PagedRequestClampsPageSize(int requested, int expected)
    {
        Assert.Equal(expected, new PagedRequest { PageSize = requested }.PageSize);
    }

    [Fact]
    public async Task MiddlewareReturnsProblemDetailsWithoutExceptionDetailsInProduction()
    {
        var (context, body) = await InvokeMiddleware(new InvalidOperationException("private failure detail"));
        using (body)
        {
            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("application/problem+json", context.Response.ContentType);
            Assert.Equal("An unexpected error occurred", body.RootElement.GetProperty("title").GetString());
            Assert.False(body.RootElement.TryGetProperty("detail", out _));
            Assert.True(body.RootElement.TryGetProperty("traceId", out _));
        }
    }

    [Theory]
    [MemberData(nameof(DomainExceptionMappings))]
    public async Task MiddlewareMapsDomainExceptionsToProblemDetails(Exception exception, int expectedStatus)
    {
        var (context, body) = await InvokeMiddleware(exception);
        using (body)
        {
            Assert.Equal(expectedStatus, context.Response.StatusCode);
            Assert.Equal(expectedStatus, body.RootElement.GetProperty("status").GetInt32());
            Assert.True(body.RootElement.TryGetProperty("code", out _));
        }
    }

    public static TheoryData<Exception, int> DomainExceptionMappings => new()
    {
        { new BusinessRuleException("Invalid request"), 400 },
        { new UnauthorizedException("Unauthenticated"), 401 },
        { new ForbiddenException("Not permitted"), 403 },
        { new NotFoundException("Missing resource"), 404 },
        { new ConflictException("Conflicting state"), 409 }
    };

    private static async Task<(DefaultHttpContext Context, System.Text.Json.JsonDocument Body)> InvokeMiddleware(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new _CarRental.API.Middleware.GlobalExceptionMiddleware(
            _ => throw exception,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<_CarRental.API.Middleware.GlobalExceptionMiddleware>.Instance,
            new TestHostEnvironment());

        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        return (context, await System.Text.Json.JsonDocument.ParseAsync(context.Response.Body));
    }

    private sealed class TestHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "CarRental.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
