using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class BranchRepository(SqlExecutor executor) : IBranchRepository
{
    public async Task<int> CreateAsync(Branch branch) => await ExecuteAsync(() => executor.ExecuteScalarInt32Async("usp_Branches_Create", p =>
    {
        AddText(p, "@Name", 100, branch.Name); AddText(p, "@Address", 255, branch.Address);
        AddText(p, "@City", 100, branch.City); AddText(p, "@Phone", 20, branch.Phone);
    }));

    public Task<Branch?> GetByIdAsync(int id) => executor.QuerySingleOrDefaultAsync("usp_Branches_GetById", BranchMapper.Map, p => AddInt(p, "@BranchId", id));

    public Task<IReadOnlyList<Branch>> ListAsync(int page, int pageSize) => executor.QueryAsync("usp_Branches_List", BranchMapper.Map, p => { AddInt(p, "@Page", page); AddInt(p, "@PageSize", pageSize); });
    
    public Task<int> GetCountAsync() => executor.ExecuteScalarInt32Async("usp_Branches_Count");

    public Task<bool> HasDependentsAsync(int id) => executor.ExecuteScalarBooleanAsync("usp_Branches_HasDependents", p => AddInt(p, "@BranchId", id));

    
    public async Task UpdateAsync(Branch branch) => await ExecuteAsync(() => executor.ExecuteNonQueryAsync("usp_Branches_Update", p =>
    {
        AddInt(p, "@BranchId", branch.BranchId); AddText(p, "@Name", 100, branch.Name);
        AddText(p, "@Address", 255, branch.Address); AddText(p, "@City", 100, branch.City); AddText(p, "@Phone", 20, branch.Phone);
    }));

    public async Task DeleteAsync(int id) => await ExecuteAsync(() => executor.ExecuteNonQueryAsync("usp_Branches_Delete", p => AddInt(p, "@BranchId", id)));
    
    public async Task SetManagerAsync(int id, int? employeeId) => await ExecuteAsync(() => executor.ExecuteNonQueryAsync("usp_Branches_SetManager", p =>
    { AddInt(p, "@BranchId", id); p.Add("@EmployeeId", SqlDbType.Int).Value = (object?)employeeId ?? DBNull.Value; }));

    private static async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return await action(); } catch (SqlException ex) { SqlErrorTranslator.ThrowIfKnown(ex); throw; }
    }
    
    private static async Task ExecuteAsync(Func<Task> action)
    {
        try { await action(); } catch (SqlException ex) { SqlErrorTranslator.ThrowIfKnown(ex); throw; }
    }
    
    private static void AddInt(SqlParameterCollection p, string n, int v) => p.Add(n, SqlDbType.Int).Value = v;
    
    private static void AddText(SqlParameterCollection p, string n, int size, string? v) => p.Add(n, SqlDbType.NVarChar, size).Value = (object?)v ?? DBNull.Value;

}
