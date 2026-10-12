using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Employees;

namespace _CarRental.Application.Common.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request);
    Task<EmployeeDto> GetAsync(int employeeId);
    Task<PagedResult<EmployeeDto>> ListByBranchAsync(int branchId, PagedRequest request);
    Task<EmployeeDto> UpdateAsync(int employeeId, UpdateEmployeeRequest request);
}
