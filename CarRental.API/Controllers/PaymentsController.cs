using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Constants.PolicyNames;
using _CarRental.Application.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{


    [HttpPost("api/contracts/{contractId:int}/payments")]
    [Authorize(Policy = Policies.ProcessPayments)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentDto>> Create(int contractId, CreatePaymentRequest request)
    {
        var payment = await paymentService.CreateAsync(contractId, request);
        return CreatedAtAction(nameof(ListByContract), new { contractId }, payment);
    }



    [HttpGet("api/contracts/{contractId:int}/payments")]
    [Authorize(Roles = "Customer,Employee,BranchManager,Admin")]
    public Task<IReadOnlyList<PaymentDto>> ListByContract(int contractId) => paymentService.ListByContractAsync(contractId);



    [HttpGet("api/payments/my")]
    [Authorize(Roles = "Customer")]
    public Task<IReadOnlyList<PaymentDto>> ListMine() => paymentService.ListMineAsync();



    [HttpGet("api/contracts/{contractId:int}/balance")]
    [Authorize(Roles = "Customer,Employee,BranchManager,Admin")]
    public Task<BalanceDto> GetBalance(int contractId) => paymentService.GetBalanceAsync(contractId);



    [HttpPost("api/payments/{paymentId:int}/status")]
    [Authorize(Policy = Policies.ProcessPayments)]
    public Task<PaymentDto> UpdateStatus(int paymentId, UpdatePaymentStatusRequest request) => paymentService.UpdateStatusAsync(paymentId, request);



    [HttpPost("api/payments/{paymentId:int}/refund")]
    [Authorize(Policy = Policies.ProcessPayments)]
    public Task<PaymentDto> Refund(int paymentId) => paymentService.RefundAsync(paymentId);


}
