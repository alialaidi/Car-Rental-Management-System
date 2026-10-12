namespace _CarRental.Application.DTOs.Users;

public sealed class AssignRoleRequest { public string RoleName { get; set; } = string.Empty; }
public sealed class SetUserActiveRequest { public bool IsActive { get; set; } }
public sealed record UserDto(int UserId, string Username, string Email, string? PhoneNumber, bool IsActive, DateTime CreatedAt, IReadOnlyList<string> Roles);
