using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Reports;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class ReportService(IUnitOfWork unitOfWork, ICurrentUser currentUser) : IReportService
{
    public async Task<IReadOnlyList<FleetStatusDto>> GetFleetStatusAsync(int? branchId)
    {
        var scope = await ResolveBranchAsync(branchId);
        return (await unitOfWork.Reports.GetFleetStatusAsync(scope))
            .Select(x => new FleetStatusDto(x.BranchId, x.BranchName, x.VehicleStatus, x.VehicleCount)).ToArray();
    }

    public async Task<IReadOnlyList<RevenueDto>> GetRevenueAsync(DateOnly from, DateOnly to, int? branchId)
    {
        ValidateRange(from, to);
        var scope = await ResolveBranchAsync(branchId);
        return (await unitOfWork.Reports.GetRevenueAsync(from, to, scope))
            .Select(x => new RevenueDto(x.Period, x.BranchId, x.BranchName, x.PaidAmount, x.RefundedAmount, x.NetRevenue)).ToArray();
    }

    public async Task<IReadOnlyList<TopVehicleDto>> GetTopVehiclesAsync(DateOnly from, DateOnly to, int? branchId, int take)
    {
        ValidateRange(from, to);
        var scope = await ResolveBranchAsync(branchId);
        take = Math.Clamp(take, 1, 100);
        return (await unitOfWork.Reports.GetTopVehiclesAsync(from, to, scope, take))
            .Select(x => new TopVehicleDto(x.VehicleId, x.PlateNumber, x.Make, x.Model, x.RentalCount, x.ContractValue)).ToArray();
    }

    public async Task<IReadOnlyList<MaintenanceCostDto>> GetMaintenanceCostAsync(DateOnly from, DateOnly to, int? branchId)
    {
        ValidateRange(from, to);
        var scope = await ResolveBranchAsync(branchId);
        return (await unitOfWork.Reports.GetMaintenanceCostAsync(from, to, scope))
            .Select(x => new MaintenanceCostDto(x.BranchId, x.BranchName, x.TotalCost, x.CompletedRecords)).ToArray();
    }

    public async Task<IReadOnlyList<PaymentSummaryDto>> GetPaymentSummaryAsync(DateOnly from, DateOnly to, int? branchId)
    {
        ValidateRange(from, to);
        var scope = await ResolveBranchAsync(branchId);
        return (await unitOfWork.Reports.GetPaymentSummaryAsync(from, to, scope))
            .Select(x => new PaymentSummaryDto(x.BranchId, x.BranchName, x.PaymentMethod, x.PaymentStatus, x.PaymentCount, x.TotalAmount)).ToArray();
    }

    private async Task<int?> ResolveBranchAsync(int? requestedBranchId)
    {
        if (currentUser.IsInRole(RoleNames.Admin))
        {
            if (requestedBranchId is <= 0) throw new BusinessRuleException("Branch id must be positive.");
            return requestedBranchId;
        }
        if (!currentUser.IsInRole(RoleNames.BranchManager))
            throw new ForbiddenException("Only administrators and branch managers can view reports.");
        var ownBranch = await currentUser.GetBranchIdAsync();
        if (requestedBranchId.HasValue && requestedBranchId.Value != ownBranch)
            throw new ForbiddenException("Branch managers can view reports for their own branch only.");
        return ownBranch;
    }

    internal static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (from > to) throw new BusinessRuleException("The start date must be on or before the end date.");
        if (to.DayNumber - from.DayNumber > 365) throw new BusinessRuleException("The date range cannot exceed 366 inclusive days.");
    }
}
