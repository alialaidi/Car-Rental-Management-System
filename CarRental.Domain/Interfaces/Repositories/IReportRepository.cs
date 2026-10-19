namespace _CarRental.Domain.Interfaces.Repositories;

public interface IReportRepository
{
    Task<IReadOnlyList<FleetStatusReportRow>> GetFleetStatusAsync(int? branchId);
    Task<IReadOnlyList<RevenueReportRow>> GetRevenueAsync(DateOnly from, DateOnly to, int? branchId);
    Task<IReadOnlyList<TopVehicleReportRow>> GetTopVehiclesAsync(DateOnly from, DateOnly to, int? branchId, int take);
    Task<IReadOnlyList<MaintenanceCostReportRow>> GetMaintenanceCostAsync(DateOnly from, DateOnly to, int? branchId);
    Task<IReadOnlyList<PaymentSummaryReportRow>> GetPaymentSummaryAsync(DateOnly from, DateOnly to, int? branchId);
}

public sealed record FleetStatusReportRow(int BranchId, string BranchName, string VehicleStatus, int VehicleCount);
public sealed record RevenueReportRow(DateOnly Period, int BranchId, string BranchName, decimal PaidAmount, decimal RefundedAmount, decimal NetRevenue);
public sealed record TopVehicleReportRow(int VehicleId, string PlateNumber, string Make, string Model, int RentalCount, decimal ContractValue);
public sealed record MaintenanceCostReportRow(int BranchId, string BranchName, decimal TotalCost, int CompletedRecords);
public sealed record PaymentSummaryReportRow(int BranchId, string BranchName, string PaymentMethod, string PaymentStatus, int PaymentCount, decimal TotalAmount);
