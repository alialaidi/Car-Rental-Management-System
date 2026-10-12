using _CarRental.Application.DTOs.Payments;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class CreatePaymentRequestValidator
{
    public void Validate(CreatePaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount <= 0 || !Enum.IsDefined(request.Method) || request.TransactionReference?.Length > 100)
            throw new BusinessRuleException("VALIDATION_FAILED", "Payment amount, method, or reference is invalid.");
    }
}
