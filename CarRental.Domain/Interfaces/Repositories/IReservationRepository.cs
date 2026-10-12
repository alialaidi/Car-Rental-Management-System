using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;

namespace _CarRental.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
    Task<int> CreateAsync(Reservation reservation);
    Task<Reservation?> GetByIdAsync(int reservationId);
    Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListByCustomerAsync(int customerId, int page, int pageSize);
    Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListForBranchAsync(
        int? branchId, ReservationStatus? status, DateTime? pickupFromUtc, DateTime? pickupToUtc, int page, int pageSize);
    Task UpdateStatusAsync(int reservationId, ReservationStatus expectedStatus, ReservationStatus newStatus, int? confirmedByEmployeeId = null);
}
