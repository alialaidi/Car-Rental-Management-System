using _CarRental.Application.Common.Models;
using _CarRental.Domain.Enums;

namespace _CarRental.Application.DTOs.Reservations;

public sealed class ReservationFilterRequest : PagedRequest
{
    public ReservationStatus? Status { get; init; }
    public DateTimeOffset? PickupFrom { get; init; }
    public DateTimeOffset? PickupTo { get; init; }
}
