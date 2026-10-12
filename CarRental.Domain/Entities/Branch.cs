namespace _CarRental.Domain.Entities;

public sealed class Branch
{
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int? ManagerEmployeeId { get; set; }
}
