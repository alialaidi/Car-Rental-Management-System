using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Contracts;

namespace _CarRental.Application.Common.Interfaces;

public interface IRentalContractService
{
    Task<ContractDto> CreateAsync(CreateContractRequest request);
    Task<ContractDto> ReturnAsync(int contractId, ReturnVehicleRequest request);
    Task<ContractDto> GetByIdAsync(int contractId);
    Task<PagedResult<ContractDto>> ListAsync(PagedRequest request);
    Task<PagedResult<ContractDto>> ListMineAsync(PagedRequest request);
}
