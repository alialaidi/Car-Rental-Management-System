using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Maintenance;

namespace _CarRental.Application.Common.Interfaces;

public interface IMaintenanceService
{
    Task<MaintenanceDto> CreateAsync(CreateMaintenanceRequest request);
    Task<MaintenanceDto> UpdateAsync(int maintenanceId, UpdateMaintenanceRequest request);
    Task<MaintenanceDto> CompleteAsync(int maintenanceId, CompleteMaintenanceRequest request);
    Task<MaintenanceDto> CancelAsync(int maintenanceId);
    Task<MaintenanceDto> GetAsync(int maintenanceId);
    Task<IReadOnlyList<MaintenanceDto>> ListByVehicleAsync(int vehicleId);
    Task<PagedResult<MaintenanceDto>> ListByBranchAsync(int branchId, PagedRequest request);
}
