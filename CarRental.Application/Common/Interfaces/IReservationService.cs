using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reservations;

namespace _CarRental.Application.Common.Interfaces;

public interface IReservationService
{
    Task<ReservationDto> CreateAsync(CreateReservationRequest request);
    Task<PagedResult<ReservationDto>> ListMineAsync(PagedRequest request);
    Task<ReservationDto> GetMineAsync(int reservationId);
    Task<ReservationDto> CancelMineAsync(int reservationId);
    Task<PagedResult<ReservationDto>> ListForStaffAsync(ReservationFilterRequest request);
    Task<ReservationDto> ConfirmAsync(int reservationId);
    Task<ReservationDto> RejectAsync(int reservationId, RejectReservationRequest request);
    Task<ReservationDto> CancelByStaffAsync(int reservationId);
}
