using _CarRental.Domain.Entities;

namespace _CarRental.Domain.Interfaces.Repositories;

public interface IEmployeeRepository
{
    Task<int> CreateAsync(Employee employee);
    Task<Employee?> GetByIdAsync(int employeeId);
    Task<Employee?> GetByUserIdAsync(int userId);
    Task<IReadOnlyList<Employee>> ListByBranchAsync(int branchId, int page, int pageSize);
    Task<int> GetCountByBranchAsync(int branchId);
    Task UpdateAsync(Employee employee);
}
