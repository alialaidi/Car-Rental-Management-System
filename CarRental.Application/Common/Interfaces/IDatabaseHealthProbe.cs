namespace _CarRental.Application.Common.Interfaces;

public interface IDatabaseHealthProbe
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
