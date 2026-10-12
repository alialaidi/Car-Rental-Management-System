using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
namespace _CarRental.Domain.Interfaces.Repositories;
public interface ILookupRepository
{
    Task<IReadOnlyList<LookupItem>> ListAsync(VehicleLookupType type);
    Task<int> CreateAsync(VehicleLookupType type, string name, string? description);
    Task UpdateAsync(VehicleLookupType type, int id, string name, string? description);
    Task DeleteAsync(VehicleLookupType type, int id);
}
