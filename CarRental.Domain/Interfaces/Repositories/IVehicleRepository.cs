using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.ValueObjects;
namespace _CarRental.Domain.Interfaces.Repositories;
public interface IVehicleRepository
{
    Task<int> CreateAsync(Vehicle vehicle);
    Task<Vehicle?> GetByIdAsync(int vehicleId);
    Task<IReadOnlyList<Vehicle>> ListByBranchAsync(int branchId, int page, int pageSize);
    Task<int> GetCountByBranchAsync(int branchId);
    Task<VehicleCatalogPage> SearchAvailableAsync(VehicleSearchCriteria criteria);
    Task<VehicleCatalogDetails?> GetCatalogDetailsAsync(int vehicleId);
    Task<bool> IsAvailableAsync(int vehicleId, DateTime pickupDateTime, DateTime expectedReturnDateTime);
    Task SetStatusAsync(int vehicleId, VehicleStatus status);
    Task LockForReservationReviewAsync(int vehicleId);
    Task<bool> HasOtherConfirmedReservationAsync(int vehicleId, int excludingReservationId);
    Task UpdateAsync(Vehicle vehicle);
    Task DeleteAsync(int vehicleId);
}
