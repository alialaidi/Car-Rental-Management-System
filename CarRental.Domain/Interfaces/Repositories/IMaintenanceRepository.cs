using _CarRental.Domain.Entities;

namespace _CarRental.Domain.Interfaces.Repositories;

public interface IMaintenanceRepository
{
    Task<int> CreateAsync(MaintenanceRecord record);
    Task UpdateAsync(MaintenanceRecord record);
    Task CompleteAsync(int maintenanceId, DateOnly actualEndDate, decimal? cost);
    Task CancelAsync(int maintenanceId);
    Task<MaintenanceRecord?> GetByIdAsync(int maintenanceId);
    Task<IReadOnlyList<MaintenanceRecord>> ListByVehicleAsync(int vehicleId);
    Task<(IReadOnlyList<MaintenanceRecord> Items, int TotalCount)> ListByBranchAsync(int branchId, int page, int pageSize);
}
