using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Domain.Entities;

public sealed class Reservation
{
    public Reservation() { }

    public Reservation(ReservationStatus status) => Status = status;

    public int ReservationId { get; set; }
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public int PickupBranchId { get; set; }
    public int ReturnBranchId { get; set; }
    public DateTime PickupDateTime { get; set; }
    public DateTime ExpectedReturnDateTime { get; set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Pending;
    public int? ConfirmedByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Cancel()
    {
        if (Status != ReservationStatus.Pending)
            throw new BusinessRuleException("INVALID_RESERVATION_TRANSITION", "Only a pending reservation can be cancelled.");

        Status = ReservationStatus.Cancelled;
    }

    public void Confirm(int? employeeId)
    {
        EnsurePending();
        if (employeeId is <= 0)
            throw new BusinessRuleException("INVALID_EMPLOYEE", "Confirming employee id must be positive.");

        Status = ReservationStatus.Confirmed;
        ConfirmedByEmployeeId = employeeId;
    }

    public void Reject()
    {
        EnsurePending();
        Status = ReservationStatus.Rejected;
    }

    public void CancelByStaff()
    {
        if (Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new BusinessRuleException("INVALID_RESERVATION_TRANSITION", "Only a pending or confirmed reservation can be cancelled by staff.");

        Status = ReservationStatus.Cancelled;
    }

    private void EnsurePending()
    {
        if (Status != ReservationStatus.Pending)
            throw new BusinessRuleException("INVALID_RESERVATION_TRANSITION", "Only a pending reservation can be reviewed.");
    }
}
