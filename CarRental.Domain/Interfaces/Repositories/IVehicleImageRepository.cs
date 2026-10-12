using _CarRental.Domain.Entities;
namespace _CarRental.Domain.Interfaces.Repositories;
public interface IVehicleImageRepository
{
    Task<int> AddAsync(VehicleImage image);
    Task<IReadOnlyList<VehicleImage>> ListByVehicleAsync(int vehicleId);
    Task RemoveAsync(int imageId);
    Task SetPrimaryAsync(int vehicleId, int imageId);
}
