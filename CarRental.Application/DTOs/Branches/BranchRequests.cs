namespace _CarRental.Application.DTOs.Branches;

public class CreateBranchRequest { public string Name { get; set; } = string.Empty; public string Address { get; set; } = string.Empty; public string City { get; set; } = string.Empty; public string? Phone { get; set; } }
public sealed class UpdateBranchRequest : CreateBranchRequest { }
public sealed class SetManagerRequest { public int? EmployeeId { get; set; } }
public sealed record BranchDto(int BranchId, string Name, string Address, string City, string? Phone, int? ManagerEmployeeId);

