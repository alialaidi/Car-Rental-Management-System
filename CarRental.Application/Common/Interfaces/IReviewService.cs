using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reviews;

namespace _CarRental.Application.Common.Interfaces;

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(int contractId, CreateReviewRequest request);
    Task<ReviewDto> UpdateAsync(int reviewId, UpdateReviewRequest request);
    Task DeleteAsync(int reviewId);
    Task<VehicleReviewsDto> ListByVehicleAsync(int vehicleId, PagedRequest request);
}
