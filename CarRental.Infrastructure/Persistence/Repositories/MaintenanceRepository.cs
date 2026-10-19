using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class MaintenanceRepository(SqlExecutor executor) : IMaintenanceRepository
{
    public Task<int> CreateAsync(MaintenanceRecord record) => Run(() => executor.ExecuteScalarInt32Async("usp_Maintenance_Create", p =>
    {
        AddInt(p, "@VehicleId", record.VehicleId); AddInt(p, "@CreatedByEmployeeId", record.CreatedByEmployeeId);
        AddText(p, "@MaintenanceType", 100, record.MaintenanceType); AddNullableText(p, "@ProblemDescription", 1000, record.ProblemDescription);
        AddDate(p, "@StartDate", record.StartDate); AddNullableDate(p, "@ExpectedEndDate", record.ExpectedEndDate);
    }));

    public Task UpdateAsync(MaintenanceRecord record) => Run(() => executor.ExecuteNonQueryAsync("usp_Maintenance_Update", p =>
    {
        AddInt(p, "@MaintenanceId", record.MaintenanceId); AddText(p, "@MaintenanceType", 100, record.MaintenanceType);
        AddNullableText(p, "@ProblemDescription", 1000, record.ProblemDescription); AddNullableDate(p, "@ExpectedEndDate", record.ExpectedEndDate);
    }));

    public Task CompleteAsync(int id, DateOnly actualEndDate, decimal? cost) => Run(() => executor.ExecuteNonQueryAsync("usp_Maintenance_Complete", p =>
    {
        AddInt(p, "@MaintenanceId", id); AddDate(p, "@ActualEndDate", actualEndDate); AddNullableDecimal(p, "@Cost", cost);
    }));

    public Task CancelAsync(int id) => Run(() => executor.ExecuteNonQueryAsync("usp_Maintenance_Cancel", p => AddInt(p, "@MaintenanceId", id)));
    public Task<MaintenanceRecord?> GetByIdAsync(int id) => Run(() => executor.QuerySingleOrDefaultAsync("usp_Maintenance_GetById", MaintenanceMapper.Map, p => AddInt(p, "@MaintenanceId", id)));
    public Task<IReadOnlyList<MaintenanceRecord>> ListByVehicleAsync(int id) => Run(() => executor.QueryAsync("usp_Maintenance_ListByVehicle", MaintenanceMapper.Map, p => AddInt(p, "@VehicleId", id)));

    public async Task<(IReadOnlyList<MaintenanceRecord> Items, int TotalCount)> ListByBranchAsync(int branchId, int page, int pageSize)
    {
        var (counts, items) = await Run(() => executor.QueryTwoResultSetsAsync("usp_Maintenance_ListByBranch",
            r => r.GetInt32(r.GetOrdinal("TotalCount")), MaintenanceMapper.Map,
            p => { AddInt(p, "@BranchId", branchId); AddInt(p, "@Page", page); AddInt(p, "@PageSize", pageSize); }));
        return (items, counts.FirstOrDefault());
    }

    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (SqlException exception) { SqlErrorTranslator.ThrowIfKnown(exception); throw; }
    }
    private static void AddInt(SqlParameterCollection p, string n, int v) => p.Add(n, SqlDbType.Int).Value = v;
    private static void AddText(SqlParameterCollection p, string n, int s, string v) => p.Add(n, SqlDbType.NVarChar, s).Value = v;
    private static void AddNullableText(SqlParameterCollection p, string n, int s, string? v) => p.Add(n, SqlDbType.NVarChar, s).Value = (object?)v ?? DBNull.Value;
    private static void AddDate(SqlParameterCollection p, string n, DateOnly v) => p.Add(n, SqlDbType.Date).Value = v.ToDateTime(TimeOnly.MinValue);
    private static void AddNullableDate(SqlParameterCollection p, string n, DateOnly? v) => p.Add(n, SqlDbType.Date).Value = v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
    private static void AddNullableDecimal(SqlParameterCollection p, string n, decimal? v)
    { var x = p.Add(n, SqlDbType.Decimal); x.Precision = 18; x.Scale = 2; x.Value = (object?)v ?? DBNull.Value; }
}
