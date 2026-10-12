using _CarRental.Application.DTOs.Lookups;
using _CarRental.Domain.Enums;
namespace _CarRental.Application.Common.Interfaces;
public interface ILookupService
{
    Task<IReadOnlyList<LookupDto>> ListAsync(VehicleLookupType type);
    Task<LookupDto> CreateAsync(VehicleLookupType type,LookupRequest request);
    Task<LookupDto> UpdateAsync(VehicleLookupType type,int id,LookupRequest request);
    Task DeleteAsync(VehicleLookupType type,int id);
}
