using _CarRental.Domain.Entities;

namespace _CarRental.Domain.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<int> CreateAsync(Payment payment);
    Task<Payment?> GetByIdAsync(int paymentId);
    Task UpdateStatusAsync(int paymentId, string status);
    Task<IReadOnlyList<Payment>> ListByContractAsync(int contractId);
    Task<IReadOnlyList<Payment>> ListByCustomerAsync(int customerId);
    Task<PaymentBalance> GetBalanceAsync(int contractId);
}

public sealed record PaymentBalance(int ContractId, decimal TotalAmount, decimal PaidAmount, decimal RefundedAmount)
{
    public decimal Balance => TotalAmount - PaidAmount + RefundedAmount;
}
