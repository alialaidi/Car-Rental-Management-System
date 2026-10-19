using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api")]
public sealed class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpPost("contracts/{contractId:int}/review")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Create(int contractId, CreateReviewRequest request)
    {
        var review = await reviewService.CreateAsync(contractId, request);
        return CreatedAtAction(nameof(ListByVehicle), new { vehicleId = review.VehicleId }, review);
    }

    [HttpPut("reviews/{reviewId:int}")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ReviewDto> Update(int reviewId, UpdateReviewRequest request) => reviewService.UpdateAsync(reviewId, request);

    [HttpDelete("reviews/{reviewId:int}")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int reviewId)
    {
        await reviewService.DeleteAsync(reviewId);
        return NoContent();
    }

    [HttpGet("vehicles/{vehicleId:int}/reviews")]
    [Authorize(Roles = "Customer,Employee,BranchManager,Admin")]
    [ProducesResponseType(typeof(VehicleReviewsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<VehicleReviewsDto> ListByVehicle(int vehicleId, [FromQuery] PagedRequest request) =>
        reviewService.ListByVehicleAsync(vehicleId, request);
}
