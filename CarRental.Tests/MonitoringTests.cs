using _CarRental.API.Middleware;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace _CarRental.Tests;

public sealed class MonitoringTests
{
    [Fact]
    public void AlertNotifierLogsThresholdWithoutLoggingSourceAddress()
    {
        var logger = new CaptureLogger<LogAlertNotifier>();
        var notifier = new LogAlertNotifier(logger, failedLoginThreshold: 2, failedLoginWindow: TimeSpan.FromMinutes(1));
        const string sourceAddress = "192.0.2.44";

        notifier.RecordFailedLogin(sourceAddress);
        notifier.RecordFailedLogin(sourceAddress);

        var alert = Assert.Single(logger.Messages);
        Assert.Contains("repeated failed login", alert, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(sourceAddress, alert, StringComparison.Ordinal);
    }

    [Fact]
    public void AlertNotifierRaisesServerErrorAlertAtConfiguredThreshold()
    {
        var logger = new CaptureLogger<LogAlertNotifier>();
        var notifier = new LogAlertNotifier(logger, serverErrorThreshold: 2, serverErrorWindow: TimeSpan.FromMinutes(1));

        notifier.RecordServerError();
        notifier.RecordServerError();

        var alert = Assert.Single(logger.Messages);
        Assert.Contains("server errors", alert, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestLoggerLogsStatusAndCorrelationWithoutQueryStringAndAlertsOnServerError()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/health/ready";
        context.Request.QueryString = new QueryString("?access_token=do-not-log");
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Body = new MemoryStream();
        var logger = new CaptureLogger<RequestLoggingMiddleware>();
        var notifier = new FakeAlertNotifier();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context, notifier);

        var message = Assert.Single(logger.Messages);
        Assert.Contains("GET", message);
        Assert.Contains("/health/ready", message);
        Assert.Contains("503", message);
        Assert.Contains(context.TraceIdentifier, message);
        Assert.DoesNotContain("access_token", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, notifier.ServerErrors);
    }

    private sealed class FakeAlertNotifier : IAlertNotifier
    {
        public int ServerErrors { get; private set; }
        public void RecordServerError() => ServerErrors++;
        public void RecordFailedLogin(string? sourceAddress) { }
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
