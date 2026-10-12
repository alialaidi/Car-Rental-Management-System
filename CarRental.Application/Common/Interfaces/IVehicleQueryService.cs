using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Vehicles;

namespace _CarRental.Application.Common.Interfaces;

public interface IVehicleQueryService
{
    Task<PagedResult<VehicleListItemDto>> SearchAsync(VehicleSearchRequest request);
    Task<VehicleDetailsDto> GetDetailsAsync(int vehicleId);
}
