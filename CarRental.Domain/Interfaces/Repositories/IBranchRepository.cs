using _CarRental.Domain.Entities;

namespace _CarRental.Domain.Interfaces.Repositories;

public interface IBranchRepository
{
    Task<int> CreateAsync(Branch branch);
    Task<Branch?> GetByIdAsync(int branchId);
    Task<IReadOnlyList<Branch>> ListAsync(int page, int pageSize);
    Task<int> GetCountAsync();
    Task UpdateAsync(Branch branch);
    Task<bool> HasDependentsAsync(int branchId);
    Task DeleteAsync(int branchId);
    Task SetManagerAsync(int branchId, int? employeeId);
}
