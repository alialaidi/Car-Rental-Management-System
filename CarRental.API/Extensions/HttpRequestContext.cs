using _CarRental.Application.Common.Interfaces;

namespace _CarRental.API.Extensions;

public sealed class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    public string? IpAddress => Truncate(httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(), 50);

    public string? DeviceInfo => Truncate(httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(), 255);

    public string? CorrelationId => Truncate(httpContextAccessor.HttpContext?.TraceIdentifier, 128);

    private static string? Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maximumLength ? value : value[..maximumLength];
    }
}
