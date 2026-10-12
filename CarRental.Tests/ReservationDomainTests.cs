using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using Xunit;

namespace _CarRental.Tests;

public sealed class ReservationDomainTests
{
    [Fact]
    public void CancelChangesPendingReservationToCancelled()
    {
        var reservation = new Reservation();

        reservation.Cancel();

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void ConfirmSetsEmployeeAndReservationStatus()
    {
        var reservation = new Reservation();

        reservation.Confirm(12);

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(12, reservation.ConfirmedByEmployeeId);
    }

    [Fact]
    public void RejectChangesPendingReservationToRejected()
    {
        var reservation = new Reservation();

        reservation.Reject();

        Assert.Equal(ReservationStatus.Rejected, reservation.Status);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Confirmed)]
    public void StaffCanCancelPendingOrConfirmedReservation(ReservationStatus status)
    {
        var reservation = new Reservation(status);

        reservation.CancelByStaff();

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public void ConfirmAndRejectRejectNonPendingReservation()
    {
        var reservation = new Reservation(ReservationStatus.Confirmed);

        Assert.Throws<BusinessRuleException>(() => reservation.Confirm(12));
        Assert.Throws<BusinessRuleException>(reservation.Reject);
    }

    [Theory]
    [InlineData(ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Rejected)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Completed)]
    public void CancelRejectsNonPendingReservations(ReservationStatus status)
    {
        var reservation = new Reservation(status);

        Assert.Throws<BusinessRuleException>(reservation.Cancel);
    }
}
