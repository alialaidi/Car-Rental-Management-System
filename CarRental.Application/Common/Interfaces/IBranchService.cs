using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Branches;

namespace _CarRental.Application.Common.Interfaces;

public interface IBranchService
{
    Task<BranchDto> CreateAsync(CreateBranchRequest request);
    Task<BranchDto> GetAsync(int branchId);
    Task<PagedResult<BranchDto>> ListAsync(PagedRequest request);
    Task<BranchDto> UpdateAsync(int branchId, UpdateBranchRequest request);
    Task DeleteAsync(int branchId);
    Task SetManagerAsync(int branchId, int? employeeId);
}
