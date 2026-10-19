using _CarRental.Application.DTOs.Reports;

namespace _CarRental.Application.Common.Interfaces;

public interface IReportService
{
    Task<IReadOnlyList<FleetStatusDto>> GetFleetStatusAsync(int? branchId);
    Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateOnly from, DateOnly to, int? branchId);
    Task<IReadOnlyList<TopVehicleDto>> GetTopVehiclesAsync(DateOnly from, DateOnly to, int? branchId, int take);
    Task<IReadOnlyList<MaintenanceCostDto>> GetMaintenanceCostAsync(DateOnly from, DateOnly to, int? branchId);
    Task<IReadOnlyList<PaymentSummaryDto>> GetPaymentSummaryAsync(DateOnly from, DateOnly to, int? branchId);
}
