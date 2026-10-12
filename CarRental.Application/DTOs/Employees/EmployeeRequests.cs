namespace _CarRental.Application.DTOs.Employees;

public sealed class CreateEmployeeRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int BranchId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public bool IsBranchManager { get; set; }
}
public sealed class UpdateEmployeeRequest { public int BranchId { get; set; } public string Position { get; set; } = string.Empty; public DateTime HireDate { get; set; } }
public sealed record EmployeeDto(int EmployeeId, int UserId, string Username, string Email, string? PhoneNumber, bool IsActive, int BranchId, string Position, DateTime HireDate, IReadOnlyList<string> Roles);
