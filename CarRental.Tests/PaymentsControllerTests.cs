using _CarRental.API.Controllers;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Payments;
using _CarRental.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace _CarRental.Tests;

public sealed class PaymentsControllerTests
{
    [Fact]
    public async Task CreateReturnsPaymentAndPointsToContractPaymentList()
    {
        var service = new FakePaymentService();
        var controller = new PaymentsController(service);
        var request = new CreatePaymentRequest(25m, PaymentMethod.Cash);

        var result = await controller.Create(17, request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(PaymentsController.ListByContract), created.ActionName);
        Assert.Equal(17, created.RouteValues!["contractId"]);
        Assert.Same(service.Payment, created.Value);
        Assert.Equal(17, service.ContractId);
        Assert.Same(request, service.Request);
    }

    [Fact]
    public void MutationsRequireProcessPaymentsPolicyAndCustomerReadsAreRestricted()
    {
        Assert.Equal("ProcessPayments", Policy(nameof(PaymentsController.Create)));
        Assert.Equal("ProcessPayments", Policy(nameof(PaymentsController.UpdateStatus)));
        Assert.Equal("ProcessPayments", Policy(nameof(PaymentsController.Refund)));
        Assert.Equal("Customer", Roles(nameof(PaymentsController.ListMine)));
        Assert.Equal("Customer,Employee,BranchManager,Admin", Roles(nameof(PaymentsController.ListByContract)));
    }

    private static string? Policy(string method) => typeof(PaymentsController).GetMethod(method)!
        .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy;
    private static string? Roles(string method) => typeof(PaymentsController).GetMethod(method)!
        .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles;

    private sealed class FakePaymentService : IPaymentService
    {
        public PaymentDto Payment { get; } = new(1, 17, 42, "Cash", 25m, null, "Pending", DateTime.UtcNow);
        public int ContractId { get; private set; }
        public CreatePaymentRequest? Request { get; private set; }
        public Task<PaymentDto> CreateAsync(int contractId, CreatePaymentRequest request)
        { ContractId = contractId; Request = request; return Task.FromResult(Payment); }
        public Task<PaymentDto> UpdateStatusAsync(int paymentId, UpdatePaymentStatusRequest request) => Task.FromResult(Payment);
        public Task<PaymentDto> RefundAsync(int paymentId) => Task.FromResult(Payment);
        public Task<IReadOnlyList<PaymentDto>> ListByContractAsync(int contractId) => Task.FromResult<IReadOnlyList<PaymentDto>>([Payment]);
        public Task<IReadOnlyList<PaymentDto>> ListMineAsync() => Task.FromResult<IReadOnlyList<PaymentDto>>([Payment]);
        public Task<BalanceDto> GetBalanceAsync(int contractId) => Task.FromResult(new BalanceDto(contractId, 100m, 25m, 0m, 75m));
    }
}
