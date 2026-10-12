using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/staff/reservations")]
[Authorize(Roles = "Employee,BranchManager,Admin")]
public sealed class StaffReservationsController(IReservationService reservationService) : ControllerBase
{


    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<ReservationDto>> List([FromQuery] ReservationFilterRequest request) =>
        reservationService.ListForStaffAsync(request);



    [HttpPost("{reservationId:int}/confirm")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ReservationDto> Confirm(int reservationId) => reservationService.ConfirmAsync(reservationId);



    [HttpPost("{reservationId:int}/reject")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ReservationDto> Reject(int reservationId, RejectReservationRequest request) =>
        reservationService.RejectAsync(reservationId, request);



    [HttpPost("{reservationId:int}/cancel")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ReservationDto> Cancel(int reservationId) => reservationService.CancelByStaffAsync(reservationId);


}
