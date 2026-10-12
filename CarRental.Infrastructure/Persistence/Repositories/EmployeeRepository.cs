using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository(SqlExecutor executor) : IEmployeeRepository
{
    public async Task<int> CreateAsync(Employee employee) => await Run(() => executor.ExecuteScalarInt32Async("usp_Employees_Create", p =>
    { AddInt(p, "@UserId", employee.UserId); AddInt(p, "@BranchId", employee.BranchId); AddText(p, "@Position", 100, employee.Position); p.Add("@HireDate", SqlDbType.Date).Value = employee.HireDate.Date; }));
    public Task<Employee?> GetByIdAsync(int id) => executor.QuerySingleOrDefaultAsync("usp_Employees_GetById", EmployeeMapper.Map, p => AddInt(p, "@EmployeeId", id));
    public Task<Employee?> GetByUserIdAsync(int id) => executor.QuerySingleOrDefaultAsync("usp_Employees_GetByUserId", EmployeeMapper.Map, p => AddInt(p, "@UserId", id));
    public Task<IReadOnlyList<Employee>> ListByBranchAsync(int branchId, int page, int pageSize) => executor.QueryAsync("usp_Employees_ListByBranch", EmployeeMapper.Map, p => { AddInt(p, "@BranchId", branchId); AddInt(p, "@Page", page); AddInt(p, "@PageSize", pageSize); });
    public Task<int> GetCountByBranchAsync(int branchId) => executor.ExecuteScalarInt32Async("usp_Employees_CountByBranch", p => AddInt(p, "@BranchId", branchId));
    public async Task UpdateAsync(Employee employee) => await Run(() => executor.ExecuteNonQueryAsync("usp_Employees_Update", p =>
    { AddInt(p, "@EmployeeId", employee.EmployeeId); AddInt(p, "@BranchId", employee.BranchId); AddText(p, "@Position", 100, employee.Position); p.Add("@HireDate", SqlDbType.Date).Value = employee.HireDate.Date; }));
    private static async Task<T> Run<T>(Func<Task<T>> work) { try { return await work(); } catch (SqlException ex) { SqlErrorTranslator.ThrowIfKnown(ex); throw; } }
    private static async Task Run(Func<Task> work) { try { await work(); } catch (SqlException ex) { SqlErrorTranslator.ThrowIfKnown(ex); throw; } }
    private static void AddInt(SqlParameterCollection p, string n, int v) => p.Add(n, SqlDbType.Int).Value = v;
    private static void AddText(SqlParameterCollection p, string n, int size, string v) => p.Add(n, SqlDbType.NVarChar, size).Value = v;
}
