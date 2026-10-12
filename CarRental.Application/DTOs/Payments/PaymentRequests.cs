using _CarRental.Domain.Enums;

namespace _CarRental.Application.DTOs.Payments;

public sealed record CreatePaymentRequest(decimal Amount, PaymentMethod Method, string? TransactionReference = null);
public sealed record UpdatePaymentStatusRequest(PaymentStatus Status);
public sealed record PaymentDto(int PaymentId, int ContractId, int CustomerId, string Method, decimal Amount,
    string? TransactionReference, string Status, DateTime PaymentDateUtc);
public sealed record BalanceDto(int ContractId, decimal TotalAmount, decimal PaidAmount, decimal RefundedAmount, decimal Balance);
