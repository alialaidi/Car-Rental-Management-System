using System.ComponentModel.DataAnnotations;
using System.Text;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class RegisterRequestValidator(IDateTimeProvider dateTimeProvider)
{
    public RegisterRequest ValidateAndNormalize(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var username = request.Username?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var licenseNumber = request.DrivingLicenseNumber?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        var address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();

        if (username.Length is < 3 or > 50)
            throw Invalid("Username must contain 3 to 50 characters.");
        if (email.Length is 0 or > 255 || !new EmailAddressAttribute().IsValid(email))
            throw Invalid("A valid email address is required.");
        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw Invalid("Password must be at least 8 characters and contain a letter and a digit.");
        if (Encoding.UTF8.GetByteCount(password) > 72)
            throw Invalid("Password is too long.");
        if (phoneNumber?.Length > 20)
            throw Invalid("Phone number must contain at most 20 characters.");
        if (fullName.Length is 0 or > 150)
            throw Invalid("Full name is required and must contain at most 150 characters.");
        if (licenseNumber.Length is 0 or > 50)
            throw Invalid("Driving license number is required and must contain at most 50 characters.");
        if (request.DrivingLicenseExpiry.Date <= dateTimeProvider.UtcNow.Date)
            throw Invalid("Driving license expiry must be a future date.");
        if (address?.Length > 255)
            throw Invalid("Address must contain at most 255 characters.");

        return new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            PhoneNumber = phoneNumber,
            FullName = fullName,
            DrivingLicenseNumber = licenseNumber,
            DrivingLicenseExpiry = request.DrivingLicenseExpiry.Date,
            Address = address
        };
    }

    private static BusinessRuleException Invalid(string message) => new("VALIDATION_FAILED", message);

}
