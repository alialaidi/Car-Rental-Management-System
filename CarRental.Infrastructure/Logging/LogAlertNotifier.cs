using System.Security.Cryptography;
using System.Text;
using _CarRental.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace _CarRental.Infrastructure.Logging;

public sealed class LogAlertNotifier(
    ILogger<LogAlertNotifier> logger,
    int serverErrorThreshold = 5,
    TimeSpan? serverErrorWindow = null,
    int failedLoginThreshold = 5,
    TimeSpan? failedLoginWindow = null) : IAlertNotifier
{
    private const int MaximumTrackedSources = 10_000;
    private readonly object _sync = new();
    private readonly Queue<DateTimeOffset> _serverErrors = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _failedLoginsBySource = new(StringComparer.Ordinal);
    private readonly TimeSpan _serverWindow = serverErrorWindow ?? TimeSpan.FromMinutes(1);
    private readonly TimeSpan _loginWindow = failedLoginWindow ?? TimeSpan.FromMinutes(5);
    private readonly int _serverThreshold = Math.Max(1, serverErrorThreshold);
    private readonly int _loginThreshold = Math.Max(1, failedLoginThreshold);

    public void RecordServerError()
    {
        var now = DateTimeOffset.UtcNow;
        var shouldAlert = false;
        lock (_sync)
        {
            Prune(_serverErrors, now - _serverWindow);
            _serverErrors.Enqueue(now);
            shouldAlert = _serverErrors.Count == _serverThreshold;
        }
        if (shouldAlert)
            logger.LogError("Alert: at least {Threshold} server errors occurred within {WindowSeconds} seconds.",
                _serverThreshold, _serverWindow.TotalSeconds);
    }

    public void RecordFailedLogin(string? sourceAddress)
    {
        var key = HashSource(sourceAddress);
        var now = DateTimeOffset.UtcNow;
        var shouldAlert = false;
        lock (_sync)
        {
            if (!_failedLoginsBySource.TryGetValue(key, out var events))
            {
                if (_failedLoginsBySource.Count >= MaximumTrackedSources)
                {
                    var oldest = _failedLoginsBySource.MinBy(x => x.Value.TryPeek(out var timestamp) ? timestamp : DateTimeOffset.MinValue).Key;
                    _failedLoginsBySource.Remove(oldest);
                }
                _failedLoginsBySource[key] = events = new Queue<DateTimeOffset>();
            }
            Prune(events, now - _loginWindow);
            events.Enqueue(now);
            shouldAlert = events.Count == _loginThreshold;
            foreach (var stale in _failedLoginsBySource.Where(x => x.Value.Count == 0).Select(x => x.Key).ToArray())
                _failedLoginsBySource.Remove(stale);
        }
        if (shouldAlert)
            logger.LogWarning("Alert: repeated failed login attempts reached the configured threshold for a source within {WindowSeconds} seconds.",
                _loginWindow.TotalSeconds);
    }

    private static void Prune(Queue<DateTimeOffset> events, DateTimeOffset cutoff)
    {
        while (events.TryPeek(out var timestamp) && timestamp < cutoff)
            events.Dequeue();
    }

    private static string HashSource(string? sourceAddress)
    {
        var value = string.IsNullOrWhiteSpace(sourceAddress) ? "unknown" : sourceAddress.Trim();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
