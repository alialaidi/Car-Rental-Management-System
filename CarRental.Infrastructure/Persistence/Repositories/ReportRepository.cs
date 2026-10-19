using System.Data;
using _CarRental.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class ReportRepository(SqlExecutor executor) : IReportRepository
{
    public Task<IReadOnlyList<FleetStatusReportRow>> GetFleetStatusAsync(int? branchId) => executor.QueryAsync(
        "usp_Reports_FleetStatusByBranch", r => new FleetStatusReportRow(r.GetInt32(r.GetOrdinal("BranchId")),
            r.GetString(r.GetOrdinal("BranchName")), r.GetString(r.GetOrdinal("VehicleStatus")), r.GetInt32(r.GetOrdinal("VehicleCount"))),
        p => AddNullableInt(p, "@BranchId", branchId));

    public Task<IReadOnlyList<RevenueReportRow>> GetRevenueAsync(DateOnly from, DateOnly to, int? branchId) => executor.QueryAsync(
        "usp_Reports_RevenueByPeriod", r => new RevenueReportRow(DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal("Period"))),
            r.GetInt32(r.GetOrdinal("BranchId")), r.GetString(r.GetOrdinal("BranchName")), r.GetDecimal(r.GetOrdinal("PaidAmount")),
            r.GetDecimal(r.GetOrdinal("RefundedAmount")), r.GetDecimal(r.GetOrdinal("NetRevenue"))),
        p => { AddDate(p, "@FromDate", from); AddDate(p, "@ToDate", to); AddNullableInt(p, "@BranchId", branchId); });

    public Task<IReadOnlyList<TopVehicleReportRow>> GetTopVehiclesAsync(DateOnly from, DateOnly to, int? branchId, int take) => executor.QueryAsync(
        "usp_Reports_TopVehicles", r => new TopVehicleReportRow(r.GetInt32(r.GetOrdinal("VehicleId")), r.GetString(r.GetOrdinal("PlateNumber")),
            r.GetString(r.GetOrdinal("Make")), r.GetString(r.GetOrdinal("Model")), r.GetInt32(r.GetOrdinal("RentalCount")),
            r.GetDecimal(r.GetOrdinal("ContractValue"))),
        p => { AddDate(p, "@FromDate", from); AddDate(p, "@ToDate", to); AddNullableInt(p, "@BranchId", branchId); p.Add("@Take", SqlDbType.Int).Value = take; });

    public Task<IReadOnlyList<MaintenanceCostReportRow>> GetMaintenanceCostAsync(DateOnly from, DateOnly to, int? branchId) => executor.QueryAsync(
        "usp_Reports_MaintenanceCost", r => new MaintenanceCostReportRow(r.GetInt32(r.GetOrdinal("BranchId")),
            r.GetString(r.GetOrdinal("BranchName")), r.GetDecimal(r.GetOrdinal("TotalCost")), r.GetInt32(r.GetOrdinal("CompletedRecords"))),
        p => { AddDate(p, "@FromDate", from); AddDate(p, "@ToDate", to); AddNullableInt(p, "@BranchId", branchId); });

    public Task<IReadOnlyList<PaymentSummaryReportRow>> GetPaymentSummaryAsync(DateOnly from, DateOnly to, int? branchId) => executor.QueryAsync(
        "usp_Reports_PaymentSummary", r => new PaymentSummaryReportRow(r.GetInt32(r.GetOrdinal("BranchId")),
            r.GetString(r.GetOrdinal("BranchName")), r.GetString(r.GetOrdinal("PaymentMethod")), r.GetString(r.GetOrdinal("PaymentStatus")),
            r.GetInt32(r.GetOrdinal("PaymentCount")), r.GetDecimal(r.GetOrdinal("TotalAmount"))),
        p => { AddDate(p, "@FromDate", from); AddDate(p, "@ToDate", to); AddNullableInt(p, "@BranchId", branchId); });

    private static void AddNullableInt(SqlParameterCollection p, string name, int? value) => p.Add(name, SqlDbType.Int).Value = value.HasValue ? value.Value : DBNull.Value;
    private static void AddDate(SqlParameterCollection p, string name, DateOnly value) => p.Add(name, SqlDbType.Date).Value = value.ToDateTime(TimeOnly.MinValue);
}
