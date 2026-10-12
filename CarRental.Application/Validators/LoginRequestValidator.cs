using System.Text;
using _CarRental.Application.DTOs;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class LoginRequestValidator
{
    public LoginRequest ValidateAndNormalize(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var usernameOrEmail = request.UsernameOrEmail?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (usernameOrEmail.Length is 0 or > 255 || string.IsNullOrWhiteSpace(password))
            throw new BusinessRuleException("VALIDATION_FAILED", "Username or email and password are required.");
        if (Encoding.UTF8.GetByteCount(password) > 72)
            throw new BusinessRuleException("VALIDATION_FAILED", "Password is too long.");

        return new LoginRequest { UsernameOrEmail = usernameOrEmail, Password = password };
    }
}
