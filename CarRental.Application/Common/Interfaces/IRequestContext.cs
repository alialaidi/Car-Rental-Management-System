namespace _CarRental.Application.Common.Interfaces;

public interface IRequestContext
{
    string? IpAddress { get; }
    string? DeviceInfo { get; }
    string? CorrelationId { get; }
}
