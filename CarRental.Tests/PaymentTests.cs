using _CarRental.Application.DTOs.Payments;
using _CarRental.Application.Validators;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using Xunit;

namespace _CarRental.Tests;

public sealed class PaymentTests
{
    [Fact]
    public void Create_RequiresPositiveAmountAndValidReference()
    {
        Assert.Throws<BusinessRuleException>(() => Payment.Create(1, 0, PaymentMethod.Cash, null));
        Assert.Throws<BusinessRuleException>(() => Payment.Create(1, 10, PaymentMethod.Cash, new string('x', 101)));
    }

    [Fact]
    public void Status_EnforcesPendingPaidRefundFlow()
    {
        var payment = Payment.Create(1, 100, PaymentMethod.Cash, null);
        payment.SetStatus(PaymentStatus.Paid);
        payment.SetStatus(PaymentStatus.Refunded);
        Assert.Throws<BusinessRuleException>(() => payment.SetStatus(PaymentStatus.Failed));
    }

    [Fact]
    public void Status_AllowsPendingFailureButRejectsRepeatTransition()
    {
        var payment = Payment.Create(1, 100, PaymentMethod.Cash, null);
        payment.SetStatus(PaymentStatus.Failed);
        Assert.Throws<BusinessRuleException>(() => payment.SetStatus(PaymentStatus.Paid));
    }

    [Fact]
    public void Validator_RejectsUndefinedMethod()
    {
        var validator = new CreatePaymentRequestValidator();
        Assert.Throws<BusinessRuleException>(() => validator.Validate(new CreatePaymentRequest(10, (PaymentMethod)99)));
    }
}
