namespace _CarRental.Domain.Entities;

public sealed class Employee
{
    public int EmployeeId { get; set; }
    public int UserId { get; set; }
    public int BranchId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
}
