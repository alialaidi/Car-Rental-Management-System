using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize(Roles = "Customer")]
public sealed class ReservationsController(IReservationService reservationService) : ControllerBase
{


    [HttpPost]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> Create(CreateReservationRequest request)
    {
        var reservation = await reservationService.CreateAsync(request);
        return CreatedAtAction(nameof(GetMine), new { reservationId = reservation.ReservationId }, reservation);
    }



    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReservationDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<ReservationDto>> ListMine([FromQuery] PagedRequest request) =>
        reservationService.ListMineAsync(request);



    [HttpGet("{reservationId:int}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ReservationDto> GetMine(int reservationId) => reservationService.GetMineAsync(reservationId);



    [HttpPost("{reservationId:int}/cancel")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ReservationDto> CancelMine(int reservationId) => reservationService.CancelMineAsync(reservationId);


}
