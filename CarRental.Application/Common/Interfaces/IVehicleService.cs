using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Vehicles;
namespace _CarRental.Application.Common.Interfaces;
public interface IVehicleService
{
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request);
    Task<VehicleDto> GetAsync(int vehicleId);
    Task<PagedResult<VehicleDto>> ListByBranchAsync(int branchId,PagedRequest request);
    Task<VehicleDto> UpdateAsync(int vehicleId,UpdateVehicleRequest request);
    Task DeleteAsync(int vehicleId);
    Task<IReadOnlyList<VehicleImageDto>> ListImagesAsync(int vehicleId);
    Task<VehicleImageDto> AddImageAsync(int vehicleId,AddImageRequest request);
    Task RemoveImageAsync(int vehicleId,int imageId);
    Task SetPrimaryImageAsync(int vehicleId,int imageId);
}
