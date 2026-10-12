using System.Net.Mail;
using System.Text;
using _CarRental.Application.DTOs.Branches;
using _CarRental.Application.DTOs.Employees;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public static class StaffRequestValidator
{
    public static CreateBranchRequest Validate(CreateBranchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Name = Required(request.Name, 100, "Name");
        request.Address = Required(request.Address, 255, "Address");
        request.City = Required(request.City, 100, "City");
        request.Phone = Optional(request.Phone, 20, "Phone");
        return request;
    }

    public static CreateEmployeeRequest Validate(CreateEmployeeRequest request, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Username = Required(request.Username, 50, "Username");
        request.Email = Required(request.Email, 255, "Email").ToLowerInvariant();
        try { _ = new MailAddress(request.Email); }
        catch (FormatException) { throw new BusinessRuleException("Email must be valid."); }
        request.Password ??= string.Empty;
        var bytes = Encoding.UTF8.GetByteCount(request.Password);
        if (request.Password.Length < 8 || bytes > 72 || !request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
            throw new BusinessRuleException("Password must be at least 8 characters and contain a letter and a digit.");
        request.PhoneNumber = Optional(request.PhoneNumber, 20, "PhoneNumber");
        request.Position = Required(request.Position, 100, "Position");
        if (request.BranchId <= 0) throw new BusinessRuleException("BranchId must be positive.");
        if (request.HireDate.Date > utcNow.Date) throw new BusinessRuleException("HireDate cannot be in the future.");
        return request;
    }

    public static UpdateEmployeeRequest Validate(UpdateEmployeeRequest request, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Position = Required(request.Position, 100, "Position");
        if (request.BranchId <= 0) throw new BusinessRuleException("BranchId must be positive.");
        if (request.HireDate.Date > utcNow.Date) throw new BusinessRuleException("HireDate cannot be in the future.");
        return request;
    }

    private static string Required(string? value, int maxLength, string name)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maxLength)
            throw new BusinessRuleException($"{name} is required and must not exceed {maxLength} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int maxLength, string name)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > maxLength) throw new BusinessRuleException($"{name} must not exceed {maxLength} characters.");
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }


}
