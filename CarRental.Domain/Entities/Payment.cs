using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Domain.Entities;

public sealed class Payment
{
    public int PaymentId { get; set; }
    public int ContractId { get; set; }
    public int CustomerId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionReference { get; set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTime PaymentDate { get; set; }

    public static Payment Create(int contractId, decimal amount, PaymentMethod method, string? transactionReference)
    {
        if (contractId <= 0 || amount <= 0 || !Enum.IsDefined(method) || transactionReference?.Length > 100)
            throw new BusinessRuleException("VALIDATION_FAILED", "Payment details are invalid.");
        return new Payment { ContractId = contractId, Amount = amount, Method = method, TransactionReference = transactionReference, Status = PaymentStatus.Pending };
    }

    public static Payment FromPersistence(int paymentId, int contractId, int customerId, PaymentMethod method,
        decimal amount, string? transactionReference, PaymentStatus status, DateTime paymentDate) => new()
    {
        PaymentId = paymentId, ContractId = contractId, CustomerId = customerId, Method = method,
        Amount = amount, TransactionReference = transactionReference, Status = status, PaymentDate = paymentDate
    };

    public void SetStatus(PaymentStatus status)
    {
        var valid = Status == PaymentStatus.Pending && status is PaymentStatus.Paid or PaymentStatus.Failed
            || Status == PaymentStatus.Paid && status == PaymentStatus.Refunded;
        if (!valid) throw new BusinessRuleException("INVALID_STATE_TRANSITION", "The payment status transition is not allowed.");
        Status = status;
    }
}
