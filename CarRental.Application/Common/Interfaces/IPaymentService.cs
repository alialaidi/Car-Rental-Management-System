using _CarRental.Application.DTOs.Payments;

namespace _CarRental.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(int contractId, CreatePaymentRequest request);
    Task<PaymentDto> UpdateStatusAsync(int paymentId, UpdatePaymentStatusRequest request);
    Task<PaymentDto> RefundAsync(int paymentId);
    Task<IReadOnlyList<PaymentDto>> ListByContractAsync(int contractId);
    Task<IReadOnlyList<PaymentDto>> ListMineAsync();
    Task<BalanceDto> GetBalanceAsync(int contractId);
}
