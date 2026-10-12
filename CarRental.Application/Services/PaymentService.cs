using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Payments;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class PaymentService(IUnitOfWork unitOfWork, ICurrentUser currentUser,
    IAuditLogger auditLogger, CreatePaymentRequestValidator validator) : IPaymentService
{
    public async Task<PaymentDto> CreateAsync(int contractId, CreatePaymentRequest request)
    {
        if (contractId <= 0) throw new NotFoundException("Contract was not found.");
        validator.Validate(request);
        var contract = await unitOfWork.Contracts.GetByIdAsync(contractId)
            ?? throw new NotFoundException("Contract was not found.");
        if (contract.Status == ContractStatus.Cancelled)
            throw new ConflictException("CONTRACT_CANCELLED", "Payments cannot be recorded for a cancelled contract.");
        var paymentId = 0;
        await unitOfWork.BeginTransactionAsync();
        try
        {
            var balance = await unitOfWork.Payments.GetBalanceAsync(contractId);
            if (request.Amount > balance.Balance)
                throw new ConflictException("OVERPAYMENT_NOT_ALLOWED", "The payment exceeds the outstanding balance.");
            var payment = Payment.Create(contractId, request.Amount, request.Method, request.TransactionReference);
            paymentId = await unitOfWork.Payments.CreateAsync(payment);
            await unitOfWork.CommitAsync();
        }
        catch { await unitOfWork.RollbackAsync(); throw; }
        var saved = await unitOfWork.Payments.GetByIdAsync(paymentId)
            ?? throw new InvalidOperationException("The payment could not be reloaded.");
        return ToDto(saved);
    }

    public async Task<PaymentDto> UpdateStatusAsync(int paymentId, UpdatePaymentStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Status is not (PaymentStatus.Paid or PaymentStatus.Failed))
            throw new BusinessRuleException("INVALID_STATE_TRANSITION", "Use the refund operation to refund a payment.");
        return await ChangeStatusAsync(paymentId, request.Status);
    }

    public Task<PaymentDto> RefundAsync(int paymentId) => ChangeStatusAsync(paymentId, PaymentStatus.Refunded);

    private async Task<PaymentDto> ChangeStatusAsync(int paymentId, PaymentStatus status)
    {
        var old = await unitOfWork.Payments.GetByIdAsync(paymentId)
            ?? throw new NotFoundException("Payment was not found.");
        await unitOfWork.BeginTransactionAsync();
        try { await unitOfWork.Payments.UpdateStatusAsync(paymentId, status.ToString()); await unitOfWork.CommitAsync(); }
        catch { await unitOfWork.RollbackAsync(); throw; }
        var updated = await unitOfWork.Payments.GetByIdAsync(paymentId)
            ?? throw new InvalidOperationException("The payment could not be reloaded.");
        if (status == PaymentStatus.Paid)
            await auditLogger.LogAsync(AuditActions.PaymentCompleted, "Payments", paymentId.ToString(CultureInfo.InvariantCulture), ToDto(old), ToDto(updated), currentUser.UserId);
        return ToDto(updated);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListByContractAsync(int contractId)
    {
        var contract = await unitOfWork.Contracts.GetByIdAsync(contractId)
            ?? throw new NotFoundException("Contract was not found.");
        await EnsureCanReadAsync(contract.CustomerId, contract.PickupBranchId);
        return (await unitOfWork.Payments.ListByContractAsync(contractId)).Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<PaymentDto>> ListMineAsync()
    {
        var customerId = await currentUser.GetCustomerIdAsync();
        return (await unitOfWork.Payments.ListByCustomerAsync(customerId)).Select(ToDto).ToArray();
    }

    public async Task<BalanceDto> GetBalanceAsync(int contractId)
    {
        var contract = await unitOfWork.Contracts.GetByIdAsync(contractId)
            ?? throw new NotFoundException("Contract was not found.");
        await EnsureCanReadAsync(contract.CustomerId, contract.PickupBranchId);
        var balance = await unitOfWork.Payments.GetBalanceAsync(contractId);
        return new BalanceDto(balance.ContractId, balance.TotalAmount, balance.PaidAmount, balance.RefundedAmount, balance.Balance);
    }

    private async Task EnsureCanReadAsync(int customerId, int branchId)
    {
        if (currentUser.IsInRole(RoleNames.Customer))
        {
            if (await currentUser.GetCustomerIdAsync() != customerId) throw new NotFoundException("Contract was not found.");
            return;
        }
        if (!currentUser.IsInRole(RoleNames.Admin) && !currentUser.IsInRole(RoleNames.Employee) && !currentUser.IsInRole(RoleNames.BranchManager))
            throw new ForbiddenException("Only customers and staff can access payments.");
        if (!currentUser.IsInRole(RoleNames.Admin) && await currentUser.GetBranchIdAsync() != branchId)
            throw new NotFoundException("Contract was not found.");
    }

    private static PaymentDto ToDto(Payment payment) => new(payment.PaymentId, payment.ContractId, payment.CustomerId,
        payment.Method.ToString(), payment.Amount, payment.TransactionReference, payment.Status.ToString(),
        DateTime.SpecifyKind(payment.PaymentDate, DateTimeKind.Utc));


}
