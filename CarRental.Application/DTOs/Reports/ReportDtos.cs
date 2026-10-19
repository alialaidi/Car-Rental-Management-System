namespace _CarRental.Application.DTOs.Reports;

public sealed record FleetStatusDto(int BranchId, string BranchName, string VehicleStatus, int VehicleCount);
public sealed record RevenueDto(DateOnly Period, int BranchId, string BranchName, decimal PaidAmount, decimal RefundedAmount, decimal NetRevenue);
public sealed record TopVehicleDto(int VehicleId, string PlateNumber, string Make, string Model, int RentalCount, decimal ContractValue);
public sealed record MaintenanceCostDto(int BranchId, string BranchName, decimal TotalCost, int CompletedRecords);
public sealed record PaymentSummaryDto(int BranchId, string BranchName, string PaymentMethod, string PaymentStatus, int PaymentCount, decimal TotalAmount);
public sealed record AuditLogDto(int AuditId, int? ActorUserId, string Action, string EntityName, string? EntityId,
    string? OldValue, string? NewValue, string? IpAddress, string? DeviceInfo, DateTime Timestamp);
public sealed record AuditLogPageDto(IReadOnlyList<AuditLogDto> Items, int Page, int PageSize, int TotalCount);
