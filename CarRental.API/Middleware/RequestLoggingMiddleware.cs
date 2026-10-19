using System.Diagnostics;
using _CarRental.Application.Common.Interfaces;

namespace _CarRental.API.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAlertNotifier alertNotifier)
    {
        var correlationId = context.TraceIdentifier;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            return Task.CompletedTask;
        });

        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:0.###} ms. CorrelationId: {CorrelationId}",
                context.Request.Method, context.Request.Path.Value, statusCode, elapsedMs, correlationId);
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                try { alertNotifier.RecordServerError(); }
                catch (Exception exception) { logger.LogWarning(exception, "Unable to record the server-error alert event."); }
            }
        }
    }
}
